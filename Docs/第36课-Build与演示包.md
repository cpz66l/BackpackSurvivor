# 第36课 · Build 与演示包

> 课程周期：2026-08-08  
> 前置：第35课《Profiler 快扫与低风险优化》  
> 关键词：Windows Build、Build Profile、Player Settings、独立 exe 验收、交付目录、Demo 冻结

---

## 一、这节课实现了什么

第36课的目标不是新增玩法，而是把《背包幸存者》从“Unity Editor 里能跑”推进到“别人拿到正式 Windows 包也能独立运行”。本课完成了当前项目第一个可交付演示包，输出目录为：

```text
Builds/BackpackSurvivor_v0.2_Windows/
  BackpackSurvivor.exe
  BackpackSurvivor_Data/
  MonoBleedingEdge/
  D3D12/
```

这一步的意义很大。项目已经不只是课堂练习或工程原型，而是进入了作品集材料阶段：可以给同学、面试官或实习投递方独立运行体验。

### 任务 1 · Build 前全包扫描

开课前先对整个项目做了一次交付前扫描，重点看 C# 编译、Build Settings 场景顺序、`.meta` 完整性、危险 using、仓库大文件、输入资产和 Player Settings。

扫描结果：

```text
dotnet build：0 warning / 0 error
缺失 .meta：0
孤儿 .meta：0
Build 场景：MainMenu → 01-Run
危险 using：未发现
ProfilerCaptures：已被 .gitignore 忽略
```

这一步确认项目具备进入正式 Build 的基础条件。

### 任务 2 · 修正 UIInputModule 动作资产引用

扫描时发现两个场景的 `InputSystemUIInputModule` 早期引用过一个断掉的 actions GUID。用户在 Unity 中尝试修复后，两个场景都恢复为有效的 `InputSystem_Actions.inputactions`。

最终状态：

```text
MainMenu / EventSystem / InputSystemUIInputModule → InputSystem_Actions
01-Run / EventSystem / InputSystemUIInputModule → InputSystem_Actions
PlayerInput → GameInput.inputactions / PlayerNormal
```

这里要区分两个输入系统用途：`GameInput` 是玩家战斗输入事实源；`InputSystem_Actions` 当前只作为 UI Input Module 的默认 UI actions 使用。玩家移动、射击、交互、旋转、暂停、开背包仍然走 `GameInput`，没有被模板输入资产接管。

### 任务 3 · 降级非阻断配置风险

扫描中还发现 `DefaultVolumeProfile.asset` 里有几个 Missing/Test Volume 组件，来自 `Unity.RenderPipelines.Core.Editor.Tests`。用户在 Unity 中确认尝试后，当前 Build 没有受到影响，因此本课不再卡住交付。

这个点被降级为：

```text
已知非阻断风险：Demo v0.2 不处理，后续视觉/后处理整理时再清。
```

这样处理的原因是：Demo 冻结期只修阻断交付的问题，不把低收益配置清理变成新的风险源。

### 任务 4 · 关闭开发包选项

正式包前必须关闭开发构建开关。最终 Windows Build Profile 状态为：

```text
m_Development: 0
m_ConnectProfiler: 0
m_BuildWithDeepProfilingSupport: 0
m_AllowDebugging: 0
m_WaitForManagedDebugger: 0
```

这意味着正式包不会自动连接 Profiler，也不会带 Development Build 调试开销。

### 任务 5 · 设置演示分辨率和窗口模式

Player Settings 调整为适合录屏和试玩的窗口配置：

```text
defaultScreenWidth: 1600
defaultScreenHeight: 900
resizableWindow: 1
fullscreenMode: 3
bundleVersion: 0.2.0
activeInputHandler: 1
```

`1600×900 + Windowed + Resizable` 对 Demo 很友好：方便 OBS 录屏，方便面试官切窗口，也方便调试时观察表现。

### 任务 6 · 删除两个运行时调试日志

本课顺手清掉两个不再需要的运行时日志：

```text
InventorySystem：经验 +x
DropItem：背包已满
```

这不是性能优化，而是交付清洁度。正式包里应尽量减少无意义 Console 输出，尤其是高频事件日志。

### 任务 7 · 正式包独立运行验收

Build 后不从 Unity 启动，而是直接双击 exe 独立运行。验收链路覆盖：

```text
MainMenu
  ↓
玩法说明 / 制作者声明
  ↓
Start → 01-Run
  ↓
移动 / 射击 / 拾取 / 开宝箱
  ↓
Tab 背包 / 拖拽 / 旋转 / 合并 / 丢弃再拾取
  ↓
武器激活 / 邻接边 / 子弹颜色 / 掉落物颜色
  ↓
结算
  ↓
Restart / 返回 MainMenu
```

用户反馈：正式包独立验收通过。这说明当前项目已经跨过 V0.2 Demo 交付线。

### 任务 8 · 仓库提交边界确认

Build 输出目录在仓库根：

```text
Builds/BackpackSurvivor_v0.2_Windows/
```

该目录被 `.gitignore` 的 `[Bb]uilds/` 正确忽略，不会提交进 Git。本课应提交的是 Build 配置、Player Settings、场景保存和日志清理；不提交 exe 和 `_Data` 目录。

---

## 二、最终代码（关键配置）

### 1. Windows Build Profile 关闭开发选项

```yaml
m_Development: 0
m_ConnectProfiler: 0
m_BuildWithDeepProfilingSupport: 0
m_AllowDebugging: 0
m_WaitForManagedDebugger: 0
```

这些配置决定了生成包是正式演示包，而不是 Profiler 用开发包。

### 2. Player Settings 演示窗口配置

```yaml
defaultScreenWidth: 1600
defaultScreenHeight: 900
resizableWindow: 1
fullscreenMode: 3
bundleVersion: 0.2.0
activeInputHandler: 1
```

其中 `activeInputHandler: 1` 继续确认使用 New Input System。

### 3. UI InputModule 有效引用

```yaml
m_ActionsAsset: {fileID: -944628639613478452, guid: 052faaac586de48259a63d0c4782560b, type: 3}
m_PointAction: {fileID: -1654692200621890270, guid: 052faaac586de48259a63d0c4782560b, type: 3}
m_LeftClickAction: {fileID: 3001919216989983466, guid: 052faaac586de48259a63d0c4782560b, type: 3}
```

两个场景的 UI InputModule 都回到有效 UI actions，菜单按钮和 Scroll View 输入链路更稳。

### 4. PlayerInput 仍使用 GameInput

```yaml
m_Actions: {fileID: -944628639613478452, guid: b62f509b021bd3a4881855eb8caa2ef1, type: 3}
m_DefaultActionMap: PlayerNormal
```

这条很关键：UI 模块使用默认 UI actions，不影响玩家战斗输入继续使用 `GameInput`。

### 5. 运行时日志清理

```csharp
private void HandleCurrency(LootEntry entry)
{
    if (entry == null) return;
}
```

经验收集不再刷 Console。

```csharp
public bool Interact()
{
    if (inventorySystem.CanAccept(lootEntry))
    {
        Collect();
        return true;
    }
    return false;
}
```

背包满时通过交互失败链路和 UI 反馈处理，不再额外输出调试日志。

---

## 三、周期链路图

### 1. Build 前扫描链路

```text
git status
  ↓
dotnet build
  ↓
危险 using 扫描
  ↓
.meta 完整性扫描
  ↓
Build Settings 场景顺序检查
  ↓
输入资产 / UIInputModule / PlayerInput 检查
  ↓
Build Profile / Player Settings 检查
```

### 2. 正式包配置链路

```text
Windows Build Profile
  ↓
关闭 Development Build
关闭 Autoconnect Profiler
关闭 Deep Profiling / Debugging
  ↓
Player Settings
  ↓
1600×900 Windowed
Resizable Window
Version 0.2.0
  ↓
Build
```

### 3. 独立 exe 验收链路

```text
关闭 Unity 依赖
  ↓
双击 BackpackSurvivor.exe
  ↓
MainMenu 操作
  ↓
Run 场景完整试玩
  ↓
结算 / Restart / MainMenu
  ↓
确认正式包可交付
```

### 4. 仓库提交边界

```text
提交：项目配置 / 场景 / 脚本清理 / 文档
不提交：Builds/ 输出包 / Library / Temp / Logs / ProfilerCaptures
```

---

## 四、设计思考

### 1. 为什么正式包要独立运行验收？

Unity Editor 里能跑，只能说明开发环境里链路成立。正式包独立运行才说明玩家拿到游戏后真的能进入、游玩、结算和重开。

这一步会暴露 Editor 掩盖的问题，比如资源没进包、Shader 表现异常、场景列表错误、输入资产断引用、窗口设置不合适等。

### 2. 为什么 Build 输出不进 Git？

Build 产物体积大，而且是可重复生成的结果。Git 应该保存源工程和配置，而不是保存每次打出来的 exe。

更合理的方式是：仓库提交源工程；交付包单独压缩、命名、发给试玩者或放到 release/网盘。

### 3. 为什么 Demo 包推荐 Windowed？

面试和作品集展示需要录屏、切窗口、对照文档。窗口模式比独占全屏更适合演示环境，也更少出现分辨率和焦点切换问题。

`1600×900` 是一个舒服的折中：画面足够清晰，又比 1920×1080 更容易录制和在不同屏幕上展示。

### 4. 为什么 Development Build 要关闭？

Development Build 会带调试开销、开发标记和 Profiler 连接能力。它适合定位问题，不适合作为正式演示包。

第35课已经完成性能验证，第36课就应该切回正式包配置。

### 5. 为什么有些非阻断风险可以暂缓？

Demo 冻结期的原则是：只修影响交付的问题。`DefaultVolumeProfile` 的 Missing/Test 组件不优雅，但当前正式包独立验收已通过，没有阻断运行。

如果为了追求“配置绝对漂亮”而在最后阶段大动后处理资产，反而可能引入新问题。这个取舍比盲目清理更成熟。

### 6. 为什么要把 UI 输入和玩家输入分开看？

UIInputModule 负责按钮、滚轮、面板点击；PlayerInput 负责角色移动、攻击、交互等战斗输入。两者都是 Input System，但职责不同。

如果只看文件名，很容易误以为 `InputSystem_Actions` 又接管了玩家输入。实际检查场景序列化后，玩家输入仍然是 `GameInput / PlayerNormal`。

---

## 五、踩坑记录

### 1. UIInputModule 的 actions GUID 曾经断过

- **现象**：场景里的 UIInputModule 引用了一个 Assets 内找不到的 actions GUID。
- **影响**：可能导致 UI 点击、滚轮、导航在 Build 中不稳定。
- **处理**：在 Unity 中重新保存后，两个场景都恢复为有效的 `InputSystem_Actions` 引用。
- **沉淀**：输入系统要分层检查，UI 输入和玩家输入不是一回事。

### 2. Volume Profile 有 Missing/Test 组件

- **现象**：`DefaultVolumeProfile.asset` 中存在几个 `m_Script: {fileID: 0}`，名称像 URP Editor Tests 残留。
- **判断**：当前 Build 独立验收通过，不阻断 Demo v0.2。
- **处理**：本课暂缓，作为后续视觉配置清理挂账。
- **沉淀**：最后阶段不要为了低收益美化清理破坏已经通过的交付链路。

### 3. Build Profile 仍保持开发包配置

- **现象**：Windows Build Profile 中 `m_Development` 和 `m_ConnectProfiler` 仍为 1。
- **风险**：正式包可能带开发开销或自动连接 Profiler。
- **修复**：关闭 Development Build 和 Autoconnect Profiler。
- **沉淀**：Profiler 课后一定要记得把 Build Profile 切回正式包。

### 4. Build 输出目录不能提交

- **现象**：正式包输出到仓库根 `Builds/`。
- **风险**：如果未忽略，exe 和 Data 目录会污染仓库。
- **处理**：`.gitignore` 已有 `[Bb]uilds/`，确认 Build 输出被忽略。
- **沉淀**：交付包可以存在本地，但 Git 只管源工程。

### 5. 运行时日志要在演示包前清理

- **现象**：经验拾取和背包已满仍有调试日志。
- **处理**：删除这两个非必要日志。
- **沉淀**：正式包不是调试台，玩家看不到 Console，但无意义日志仍然反映工程洁癖。

---

## 六、面试问答

**Q1：你怎么确认 Unity 项目不是只能在 Editor 里跑？**  
> 我会打正式 Windows Build，然后关闭 Unity 依赖，直接双击 exe 独立运行，按主菜单、玩法说明、进入游戏、战斗、背包、结算、重开和返回主菜单的真实玩家链路验收。

**Q2：Development Build 和正式 Build 有什么区别？**  
> Development Build 适合调试和 Profiler 连接，会带额外调试能力和开销；正式演示包应该关闭 Development、Profiler、Deep Profiling 和 Script Debugging。

**Q3：为什么 Build 输出不提交 Git？**  
> Build 是可重复生成的产物，体积大，不适合进版本历史。仓库应该提交源码、资源和项目配置；交付包单独压缩分发。

**Q4：你怎么检查 Build 场景顺序？**  
> 我检查 `EditorBuildSettings.asset`，确认场景列表是 `MainMenu` 在 0，`01-Run` 在 1。这样开始游戏和结算返回都能走正确场景流。

**Q5：为什么选择 1600×900 Windowed？**  
> 它适合录屏和试玩，画面比例是标准 16:9，窗口不会强占全屏，面试或演示时更方便切换文档、录屏软件和游戏。

**Q6：UIInputModule 和 PlayerInput 为什么要分开查？**  
> UIInputModule 管按钮、滚动、点击；PlayerInput 管角色输入。它们都用 Input System，但使用不同 action asset 是可以接受的，关键是各自引用不能断。

**Q7：这次 Build 课最大的交付判断是什么？**  
> 最大判断是：正式 exe 已经独立验收通过。这个结果比 Editor 内 Play 更有交付意义，说明当前 Demo v0.2 可以进入作品材料整理阶段。

---

## 七、习惯清单

1. Build 前先全包扫描，不要直接点 Build。
2. 场景顺序必须确认，尤其是 MainMenu 和 Run。
3. Profiler 用完后，正式包要关闭 Development 和 Autoconnect Profiler。
4. 独立 exe 验收比 Editor Play 验收更接近玩家真实体验。
5. Build 输出目录必须被 `.gitignore` 忽略。
6. UI 输入和玩家输入要分开检查。
7. Player Settings 要明确版本号、分辨率和窗口模式。
8. 演示包优先 Windowed，方便录屏和面试展示。
9. 最后阶段只修阻断交付的问题，不做低收益大清理。
10. 删除无意义运行时日志，保留真正有用的错误/警告。
11. 每次 Build 后都要直接跑 exe，而不是只看 Unity Console。
12. 正式包过验收后，立刻记录版本和提交范围。

---

## 八、思考题（附复习参考）

**Q1：为什么正式包要关闭 Autoconnect Profiler？**  
> 因为它属于调试用途，会让包尝试连接 Profiler，也可能带来不必要开销。正式给别人玩的包应该尽量接近普通玩家运行环境。

**Q2：如果 exe 能打开但 Start 进不了 Run，优先查哪里？**  
> 优先查 Build Settings 场景列表和场景名。`SceneManager.LoadScene("01-Run")` 依赖该场景被加入 Build，并且名称正确。

**Q3：如果 Build 中 UI 按钮不能点，但 Editor 可以点，可能是什么问题？**  
> 可能是 EventSystem、InputSystemUIInputModule、Actions Asset、Canvas Raycaster 或透明面板吃射线的问题。先查 UI 输入模块，再查面板射线。

**Q4：为什么 Build 输出目录不应该进 Git？**  
> 它是生成物，体积大且可以重新构建。把它放进 Git 会拖慢仓库，也让版本历史变脏。

**Q5：最后阶段遇到 Missing Script 一定要立刻删吗？**  
> 不一定。要看它是否影响 Build 和运行。如果正式包已通过，且该 Missing 项不参与核心逻辑，可以先记录为非阻断风险，避免最后阶段引入新问题。

**Q6：作品集里怎么证明你真的做过 Build 验收？**  
> 可以在 README 或开发日志里写清 Build 平台、版本号、分辨率、独立 exe 验收链路和已知非阻断风险。再配一段运行录屏，就很有说服力。

---

## 九、下一步

**第37课：作品材料。**

第36课已经确认：当前 Demo v0.2 可以从正式 Windows 包独立运行，主菜单、玩法说明、战斗、背包、结算、重开和返回主菜单链路通过验收。

下一课不再加系统，而是把项目包装成可以投实习的作品材料：

- README：项目简介、玩法说明、核心系统、运行方式、技术亮点。
- 录屏脚本：1~2 分钟展示最有信息量的流程。
- 截图清单：主菜单、战斗、背包构筑、升级三选一、结算页。
- 简历描述：一句话项目定位、个人负责内容、技术难点、验证结果。
- 面试素材：Profiler 判断、纯数据背包、对象池、邻接系统、Build 验收。

**本课 commit 建议**：`第36课 Build与演示包`
