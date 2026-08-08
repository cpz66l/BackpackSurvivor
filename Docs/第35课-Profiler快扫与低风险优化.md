# 第35课 · Profiler 快扫与低风险优化

> 课程周期：2026-08-08  
> 前置：第34课《完整 15 分钟通关验收》  
> 关键词：Unity Profiler、Editor 开销、Build 验证、资源上传尖刺、低风险修复、打包前性能闸门

---

## 一、这节课实现了什么

第35课不是继续加玩法，而是把项目从“可玩”推进到“可以放心打包给别人玩”。本课围绕一个真实问题展开：Editor 里试玩到后期会出现几秒一顿的感觉，但 Build 里完整体验一局后并没有明显卡顿。

本课的重点不是“看到尖刺就优化”，而是学会判断：

```text
这个尖刺到底来自游戏逻辑？
还是来自 Editor？
还是来自 Profiler 观察本身？
还是来自资源首次加载/上传？
```

最终结论：当前 V0.2 Demo 在 Build 中后期波次没有阻断级掉帧，性能可以进入打包阶段；真正暴露的是 Build 中子弹与装备掉落物颜色异常，这属于打包资源表现问题，已通过显式材质资产修复。

### 任务 1 · 保存 Profiler 快扫证据

本课把 Profiler 截图整理进项目：

```text
Docs/ProfilerEvidence/
  README.md
  profiler-editor-render-thread-spike.png
  profiler-loading-texture-upload-spike.png
  profiler-editorloop-overhead.png
  profiler-live-display-warning.png
```

这样做的目的不是把所有原始数据堆进仓库，而是保留一条清晰证据链：看到尖刺、定位来源、区分 Editor 和 Player、最后用 Build 实测下结论。

### 任务 2 · 判断 Editor Profiler 尖刺不等于游戏卡顿

第一类截图显示了极高的尖刺，例如 Render Thread 上出现约 `1100ms` 级别耗时。

![Editor Render Thread spike](ProfilerEvidence/profiler-editor-render-thread-spike.png)

这类尖刺看起来很吓人，但不能直接说明游戏逻辑有问题。因为它出现在 Editor + Profiler 录制环境中，必须继续看 Timeline / Hierarchy 的线程来源。

### 任务 3 · 定位一次资源预加载/纹理上传尖刺

第二类截图显示 CPU 约 `263.87ms`，Main Thread 中 `PlayerLoop` 约 `244.39ms`，关键路径是：

```text
EarlyUpdate.UpdatePreloading
Application.WaitForAsyncOperationToComplete
Gfx.CreateTexture / Gfx.UploadTexture
```

![Loading texture upload spike](ProfilerEvidence/profiler-loading-texture-upload-spike.png)

这个证据说明：尖刺更像资源预加载、贴图上传、字体/界面首次展开，而不是敌人 AI、子弹、掉落或背包 UI 的每帧脚本热点。

### 任务 4 · 识别 EditorLoop / Live Display 观察开销

后续截图里，Hierarchy 显示 `EditorLoop` 占 `99.2%`，而 `PlayerLoop` 只有约 `1.30ms`。

![EditorLoop overhead](ProfilerEvidence/profiler-editorloop-overhead.png)

Profiler 自己也给出提示：录制 Playmode/Editor 时，Live display 会增加 Profiler Window repaint 带来的 EditorLoop 开销。

![Profiler live display warning](ProfilerEvidence/profiler-live-display-warning.png)

这一步是本课最重要的判断：不能把 EditorLoop 的耗时当作游戏运行时代码热点去优化。

### 任务 5 · 用 Build 实测做最终裁决

用户在 Build 中完整体验后反馈：

```text
完全没有卡顿。
总共打到约 6000 分。
最后波次也没有明显掉帧。
```

这条反馈把性能问题从“疑似阻断”降级为“Editor/Profiler 环境中的观察问题”。当前 Demo 不需要为这条线做大重构。

### 任务 6 · 修复 Build 中颜色异常

Build 中新的实际问题是：子弹模型与装备掉落物模型颜色不对，像是颜色没有渲染上。

排查后发现两者共性很明显：

```text
Projectile：Awake 中 GameObject.CreatePrimitive 后 renderer.material.color = Color.yellow
DropItem：Awake 中 GameObject.CreatePrimitive 后按 rarity 写 modelRb.material.color
```

Editor 中默认材质完整可用，Build 中则会受到 URP Shader stripping、默认材质引用和属性映射影响。最终修复为：显式 URP Unlit 材质资产 + prefab 序列化引用 + `MaterialPropertyBlock` 写颜色。

### 任务 7 · 忽略大体积 Profiler 原始捕获

本课发现 `BackpackSurvivor/ProfilerCaptures/` 中存在接近 180MB 到 220MB 的 `.data` 文件。

这些文件适合本地分析，不适合提交到简历项目仓库。因此 `.gitignore` 增加：

```text
BackpackSurvivor/ProfilerCaptures/
```

最终策略是：提交轻量截图和文字结论，不提交大体积原始捕获。

### 任务 8 · 最终验收通过

本课收官前完成：

- Build 实测后期波次无明显掉帧。
- Profiler 截图已整理进 `Docs/ProfilerEvidence/`。
- 原始 Profiler `.data` 捕获已加入忽略规则。
- BUG-025 已记录并修复。
- 子弹与装备掉落物颜色在 Build 中恢复正常。
- `dotnet build --no-restore` 通过，0 错误。
- 危险 using 扫描干净。

---

## 二、最终代码（关键片段）

### 1. Projectile 显式材质入口

```csharp
[SerializeField] private Material visualMaterial;

private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
private static readonly int ColorId = Shader.PropertyToID("_Color");
private MaterialPropertyBlock propertyBlock;
private Renderer visualRenderer;
```

子弹仍然可以运行时生成简易球体，但颜色不再只依赖 Unity 默认材质。

### 2. Projectile 应用颜色

```csharp
private void ApplyVisualColor(Color color)
{
    if (visualRenderer == null) return;

    if (visualMaterial == null)
    {
        visualRenderer.material.color = color;
        return;
    }

    visualRenderer.sharedMaterial = visualMaterial;
    propertyBlock ??= new MaterialPropertyBlock();
    visualRenderer.GetPropertyBlock(propertyBlock);
    propertyBlock.SetColor(BaseColorId, color);
    propertyBlock.SetColor(ColorId, color);
    visualRenderer.SetPropertyBlock(propertyBlock);
}
```

这里保留了 `visualMaterial == null` 的兜底，避免 prefab 漏接时直接失效；正常路径使用显式材质和 `MaterialPropertyBlock`。

### 3. DropItem 按稀有度取颜色

```csharp
private Color GetRarityColor(Rarity rarity)
{
    switch (rarity)
    {
        case Rarity.Common:
            return Color.white;
        case Rarity.Uncommon:
            return Color.green;
        case Rarity.Rare:
            return Color.blue;
        case Rarity.Epic:
            return new Color(0.6f, 0.2f, 0.9f);
        case Rarity.Legendary:
            return new Color(1f, 0.84f, 0f);
        default:
            return Color.yellow;
    }
}
```

装备掉落物仍然保留白/绿/蓝/紫/金品质识别，但实现方式从“改默认材质实例”升级为“显式材质 + 实例属性覆盖”。

### 4. DropItem 应用颜色

```csharp
modelRb.sharedMaterial = visualMaterial;
propertyBlock ??= new MaterialPropertyBlock();
modelRb.GetPropertyBlock(propertyBlock);
propertyBlock.SetColor(BaseColorId, color);
propertyBlock.SetColor(ColorId, color);
modelRb.SetPropertyBlock(propertyBlock);
```

`MaterialPropertyBlock` 的好处是：同一张材质资产可以被多个掉落物共享，每个实例又能有自己的颜色，不需要反复生成材质副本。

### 5. Prefab 显式引用材质

```yaml
visualMaterial: {fileID: 2100000, guid: 9a0f8b5a7c3d4e1aa4b7c8d9e0f12345, type: 2}
```

`Projectlie.prefab` 和 `DropItem.prefab` 都引用 `M_RuntimeVisual_Unlit_Base.mat`。这条序列化引用确保 Build 会把该材质和 Shader 引用链带进包里。

### 6. 忽略原始 Profiler 捕获

```gitignore
BackpackSurvivor/ProfilerCaptures/
```

`.data` 捕获适合本机复盘，不适合进入仓库。简历项目更适合提交截图证据和结论文档。

---

## 三、周期链路图

### 1. Profiler 快扫判断链路

```text
Editor 中感觉卡顿
  ↓
打开 Unity Profiler 录制
  ↓
观察 CPU / GPU / GC / Timeline / Hierarchy
  ↓
发现 Render Thread / Loading / EditorLoop 尖刺
  ↓
判断是否属于 PlayerLoop 游戏逻辑
  ↓
用 Development Build / Player Build 实测验证
  ↓
Build 无明显卡顿 → 不做大重构
```

### 2. 资源上传尖刺识别链路

```text
CPU 尖刺
  ↓
Timeline 展开 Main Thread
  ↓
EarlyUpdate.UpdatePreloading
  ↓
Application.WaitForAsyncOperationToComplete
  ↓
Render Thread 出现 Gfx.CreateTexture / Gfx.UploadTexture
  ↓
归因为资源首次加载/纹理上传候选问题
```

### 3. Build 颜色异常修复链路

```text
Build 中子弹/装备掉落物颜色异常
  ↓
查 Projectile / DropItem
  ↓
发现运行时 CreatePrimitive + 默认材质改色
  ↓
新增显式 URP Unlit 材质资产
  ↓
Prefab 序列化引用材质
  ↓
MaterialPropertyBlock 按实例写颜色
  ↓
Build 颜色恢复正常
```

### 4. Profiler 证据入仓链路

```text
原始 Profiler .data 捕获
  ↓
体积 180MB~220MB，不提交
  ↓
挑选关键截图
  ↓
复制到 Docs/ProfilerEvidence/
  ↓
README 解释每张图的判断意义
  ↓
课程总结引用截图
```

---

## 四、设计思考

### 1. 为什么不能看到尖刺就立刻优化？

Profiler 的数字不是答案，它只是线索。尤其在 Unity Editor 中，EditorLoop、Scene/Game 视图、Profiler Window repaint、Live display 都可能制造额外开销。

如果不区分来源，就容易把时间花在优化敌人 AI、子弹、背包 UI 上，但真正耗时可能根本不在游戏逻辑里。

### 2. 为什么 Build 实测比 Editor 感觉更重要？

玩家最终运行的是 Player Build，不是 Unity Editor。Editor 多了序列化、Inspector、Profiler、窗口刷新、Domain/资源管理等开销。

Demo 打包前，Editor Profiler 用来定位线索，Build 实测用来做交付判断。两者角色不同。

### 3. 为什么不提交 Profiler `.data` 原始捕获？

原始捕获文件很大，适合本地继续打开分析，但对简历仓库不友好。别人 clone 项目时，更需要看到你的分析结论和关键证据，而不是下载几百 MB 的数据文件。

提交轻量截图 + README 的方式更专业：它展示了你做过测量，也展示了你怎么判断。

### 4. 为什么子弹和装备掉落物要改成显式材质？

运行时 `CreatePrimitive` 本质上是原型期做法。它方便，但默认材质和 Shader 引用不够可控。

Build 阶段需要“所有可见资源都能被资产引用链追踪”。显式材质能让 Unity 打包系统知道这个材质和 Shader 是项目需要的。

### 5. 为什么用 Unlit，而不是 Lit？

子弹和装备掉落物是信息提示物，不是需要真实受光的主体模型。它们最重要的是稳定可读，尤其在混乱战斗中要一眼看见。

Unlit 不依赖场景光照，颜色稳定，打包风险低，适合这种小型提示视觉。

### 6. 为什么用 MaterialPropertyBlock？

如果直接改 `renderer.material.color`，Unity 会为 Renderer 实例化材质副本。对象多了以后，材质实例数量会变多，也不利于资源管理。

`MaterialPropertyBlock` 可以共享同一张材质资产，同时给每个实例设置不同颜色。对掉落物这种“同材质、多颜色”的场景很合适。

---

## 五、踩坑记录

### 1. Editor 卡顿不一定是 Build 卡顿

- **现象**：Editor 中后期体验有几秒一顿的感觉，Profiler 也能看到大尖刺。
- **排查**：Timeline / Hierarchy 显示部分尖刺来自 EditorLoop、Render Thread 或资源上传。
- **结论**：不能把 Editor Profiler 尖刺直接当成游戏逻辑瓶颈。
- **沉淀**：性能判断必须先问“这个耗时在哪个线程、哪个模块、哪个运行环境”。

### 2. Live Display 本身会制造观察开销

- **现象**：Profiler 提示 Live display 会增加 EditorLoop repaint 开销。
- **影响**：开着 Live display 看帧，可能让 Editor 比实际运行更卡。
- **处理**：最终用 Build 实测做裁决。
- **沉淀**：测量工具也会影响被测对象，性能分析要考虑观测成本。

### 3. 资源上传尖刺不要误判为脚本热点

- **现象**：出现 `UpdatePreloading`、`WaitForAsyncOperationToComplete`、`Gfx.UploadTexture`。
- **分析**：这类调用更像资源加载/贴图上传/TMP 字体或 UI 首次使用，而不是每帧逻辑慢。
- **处理**：Build 无复现就不优先优化；若复现，再做 UI/TMP/贴图预热。
- **沉淀**：Loading 尖刺和 Update 热点是两类问题，解决手段完全不同。

### 4. 运行时默认材质在 Build 中不可靠

- **现象**：Build 中子弹与装备掉落物颜色异常。
- **根因**：`CreatePrimitive` + 默认材质 + `material.color` 在 Editor 中可用，但 Build 中 Shader/材质引用不够稳定。
- **修复**：新增显式 URP Unlit 材质，Prefab 引用，颜色走 `MaterialPropertyBlock`。
- **沉淀**：Demo 打包前要把原型期临时视觉逐步资产化。

### 5. Profiler 证据要轻量化入仓

- **现象**：Profiler `.data` 捕获单个接近 200MB。
- **风险**：直接提交会污染仓库，也不利于别人快速 clone。
- **修复**：提交 `Docs/ProfilerEvidence/` 下的 PNG 截图与 README，忽略原始捕获目录。
- **沉淀**：项目证据要服务沟通，不是把全部原始数据无脑提交。

---

## 六、面试问答

**Q1：你怎么判断 Unity 项目后期卡顿是不是游戏逻辑导致的？**  
> 我会先用 Profiler 看 CPU/GPU/GC，再进 Timeline 或 Hierarchy 查具体线程和调用来源。如果耗时主要在 `PlayerLoop` 的脚本、物理、UI，那才是游戏逻辑热点；如果主要在 `EditorLoop`、Profiler repaint、Render Thread 或资源上传，就不能直接改玩法代码。

**Q2：这次 Profiler 快扫发现了什么？**  
> Editor 里看到过 Render Thread 尖刺、`UpdatePreloading` / `Gfx.UploadTexture` 资源上传尖刺，以及 `EditorLoop` 占用。但 Build 实测后期波次没有明显卡顿，所以当前 Demo 没有阻断打包的运行时性能问题。

**Q3：为什么要用 Build 实测验证？**  
> 因为玩家最终运行的是 Build，不是 Unity Editor。Editor 有额外窗口、Profiler、Inspector 和资源管理开销。Editor Profiler 用来定位线索，Build 用来做交付判断。

**Q4：你怎么处理 Profiler 证据？**  
> 我没有把几百 MB 的 `.data` 原始捕获提交进仓库，而是挑了关键截图，放到 `Docs/ProfilerEvidence/`，配 README 说明每张图的判断意义。这样既证明做过性能分析，又不会污染仓库。

**Q5：Build 里颜色异常为什么和性能课有关？**  
> 因为这是打包验证暴露出来的 Player-only 问题。性能课的目标是让 Demo 可以稳定交付，Build 颜色异常虽然不是性能瓶颈，但属于打包前必须修掉的视觉质量问题。

**Q6：为什么运行时 `CreatePrimitive` 改色会在 Build 里出问题？**  
> 它依赖 Unity 默认材质和默认 Shader。Editor 里默认资源都在，Build 会做 Shader stripping 和资源裁剪，如果没有显式资产引用链，就可能出现颜色或材质表现不稳定。

**Q7：为什么用 `MaterialPropertyBlock`？**  
> 它允许多个 Renderer 共享同一张材质资产，同时每个实例有自己的颜色。相比直接改 `renderer.material.color`，更适合大量掉落物或子弹这种复用对象。

**Q8：这次你做了哪些低风险优化？**  
> 我没有大改系统，只做了三件低风险事：整理 Profiler 证据、忽略大体积原始捕获、把子弹和装备掉落物颜色从默认材质改为显式材质 + `MaterialPropertyBlock`。

---

## 七、习惯清单

1. Editor 中感觉卡，不等于 Build 一定卡。
2. Profiler 先看线程和模块，再决定优化方向。
3. 看到尖刺先问是不是 `PlayerLoop`，不要直接怪脚本。
4. `GC Alloc` 没有明显增长时，不要把问题误判为 GC。
5. `UpdatePreloading` / `Gfx.UploadTexture` 更像资源加载问题，不是普通 Update 热点。
6. Build 验证必须覆盖后期波次，而不是只跑开局 30 秒。
7. Profiler 原始捕获本地留，仓库提交轻量证据和结论。
8. Build 中稳定显示的视觉资源要显式资产化。
9. 小型提示物优先考虑 Unlit，保证可读性和打包稳定性。
10. 多实例同材质变色优先用 `MaterialPropertyBlock`。
11. 打包前继续扫危险 using：`UnityEditor`、`ShadowCascadeGUI`、`Unity.VisualScripting`。
12. 只修阻断交付的问题，不在 Build 前夕开大重构。

---

## 八、思考题（附复习参考）

**Q1：Profiler 里看到 200ms 尖刺，为什么不能马上优化 EnemyAI？**  
> 因为 200ms 只是结果，不是原因。必须先看调用路径。如果尖刺在 `EditorLoop` 或资源上传路径，优化 EnemyAI 没有意义。

**Q2：如果 Build 里也出现 `Gfx.UploadTexture` 尖刺，下一步该怎么办？**  
> 先确认是不是首次打开 UI、首次显示 TMP 中文字、首次加载大贴图或首次生成某类模型。确认后可以做预加载、材质/字体预热、减少贴图尺寸或避免战斗中首次加载。

**Q3：为什么 Profiler `.data` 不适合直接提交？**  
> 体积太大，会拖慢 clone 和仓库历史。简历项目更需要可读证据，截图和分析说明足够表达“我会测、会判断、会取舍”。

**Q4：`renderer.material.color` 和 `MaterialPropertyBlock` 的区别是什么？**  
> `renderer.material` 往往会实例化材质副本；`MaterialPropertyBlock` 不改共享材质资产，只给当前 Renderer 覆盖属性，更适合大量复用对象的按实例变色。

**Q5：为什么子弹和掉落物用 Unlit 是合理的？**  
> 它们承担信息传达，不承担真实受光表现。Unlit 在不同光照环境下颜色稳定，更适合战斗中快速识别。

**Q6：如果以后要继续做性能专题，优先看哪些点？**  
> 优先看敌人数量上升后的 `PlayerLoop`、对象池扩容、伤害数字数量、Canvas rebuild、GC Alloc、音效播放数量、TargetRegistry 查询和背包 Redraw。只有测到瓶颈再优化。

---

## 九、下一步

**第36课：Build 与演示包。**

第35课已经确认：当前 V0.2 Demo 在 Build 中后期波次无明显卡顿，Profiler 尖刺主要来自 Editor/Profiler 观察开销和资源加载候选项；Build 中发现的子弹/掉落物颜色问题也已修复。

第36课开始进入正式交付：

- 关闭 Development Build / Autoconnect Profiler。
- 打 Windows x64 正式包。
- 独立运行正式包，检查 MainMenu → Run → Result → Restart / MainMenu 全链路。
- 检查分辨率、窗口模式、输入、音效、字体、材质、场景加载。
- 准备演示包目录结构和交付说明。

**本课 commit 建议**：`第35课 Profiler快扫与低风险优化`
