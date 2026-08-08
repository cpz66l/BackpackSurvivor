# Backpack Survivor / 背包幸存者

> Unity 3D 俯视角生存射击 Demo。玩家在 15 分钟单局中移动、射击、搜刮、整理背包，并通过武器激活、合并升级、邻接芯片和物品价值管理形成构筑成长。

![Unity](https://img.shields.io/badge/Unity-6000.3.20f1-000000?logo=unity)
![CSharp](https://img.shields.io/badge/C%23-Gameplay-512BD4?logo=csharp)
![URP](https://img.shields.io/badge/URP-17.3.0-blue)
![Status](https://img.shields.io/badge/Status-v0.2%20Playable%20Demo-success)

## 项目概览

《背包幸存者》是一款个人独立开发的 Unity 3D 俯视角生存射击 Demo。项目融合幸存者战斗、掉落搜刮、网格背包构筑、合并升级、邻接联动、经验成长、波次压力、金币与物品价值结算，目标是在一个可独立运行的 15 分钟单局中呈现“边战斗、边搜刮、边整理构筑”的核心体验。

当前 v0.2 已经完成正式 Windows Build 验收：游戏可以从主菜单进入，完成战斗、拾取、宝箱、背包整理、升级、结算、重开和返回主菜单等完整链路。

## 试玩下载

Windows 可试玩包已发布在 GitHub Releases：

- [下载 Backpack Survivor v0.2 Windows Demo](https://github.com/cpz66l/BackpackSurvivor/releases/tag/v0.2.0)

下载后请先解压整个压缩包，再双击 `BackpackSurvivor.exe` 启动。不要只单独运行 exe，游戏还需要同目录下的 `BackpackSurvivor_Data`、`UnityPlayer.dll`、`MonoBleedingEdge` 等文件。

## 当前版本

| 维度 | 状态 |
| --- | --- |
| 当前版本 | v0.2 Windows Playable Demo |
| 最近状态 | 第 36 个迭代节点：Build 与演示包 |
| 开发周期 | 2026-07-19 开始，2026-08-08 完成 v0.2 正式包验收 |
| 工程规模 | 46 次 Git 提交、68 个 C# 脚本、约 5.1k 行 C# 代码 |
| 主要入口 | `Assets/BackpackSurvivor/Scenes/MainMenu/MainMenu.unity` |
| 单局目标 | 15 分钟生存、成长、搜刮与结算 |
| Build 验收 | Windows 独立 exe 已通过完整试玩验证 |

> 说明：`Docs/` 中“第 xx 课”的命名是个人开发节奏与复盘记录，不代表网课或教程项目。项目设计、功能取舍、实现落地与 Unity 内接线均由本人独立完成，AI 主要作为任务拆解、代码 review、调试分析和复盘整理的辅助工具。

## 核心玩法

```text
主菜单
  -> 阅读玩法说明 / 开始游戏
  -> 移动、主动射击、自动武器战斗
  -> 击杀敌人，拾取经验、金币和装备
  -> 寻找并开启不同稀有度宝箱
  -> 打开背包，摆放、旋转、合并、丢弃和重新拾取物品
  -> 背包武器激活自动武器，邻接芯片强化战斗表现
  -> 波次强度持续提升，精英敌人与高品质宝箱逐步出现
  -> 胜利或失败后进入结算，可重开或返回主菜单
```

## 操作说明

| 操作 | 输入 |
| --- | --- |
| 移动 | `WASD` |
| 瞄准 | 鼠标指向地面 |
| 主动射击 | 鼠标左键 |
| 交互 / 拾取装备 / 开宝箱 | `E` |
| 打开或关闭背包 | `Tab` |
| 拖拽物品 | 鼠标左键拖拽 |
| 旋转背包物品 | 拖拽中按 `R` |
| 暂停 | `Esc` |

## 已实现系统

### 战斗与单局

- `IDamageable / Health / DamageInfo / WeaponBase` 形成统一伤害管线，主动武器和自动武器复用投射物命中逻辑。
- `AutoWeapon / ActiveWeapon / Projectile` 支持自动索敌、主动射击、投射物扫掠检测、阵营过滤和真实伤害结算。
- `EnemyAI / EnemySpawner / WaveDirector` 支持敌人追击、普通/精英分池、刷怪间隔、场上上限、精英概率和波次血量成长。
- `GameSession / RunTimer / GameState / RunResult` 负责 15 分钟倒计时、暂停、死亡失败、时间到胜利、结算快照、重开和返回主菜单。
- `RunHudView / ResultView` 显示血量、时间、等级、经验圆环、金币、波次名称、宝箱距离和结算数据。

### 掉落、宝箱与经济

- `LootTableData / LootRoller / LootManager` 支持权重掉落、保底机制、束表递归和经验/金币/装备分频道生成。
- `XpOrb / GoldOrb / DropItem / LootChest` 覆盖经验、金币、装备和宝箱，支持对象池、散落飞出、磁吸、交互拾取和超时回收。
- 宝箱品质会随波次阶段调整，高压后期更容易出现高稀有度宝箱。
- 背包内物品带有 `scoreValue`，局内显示单件价值和背包总价值，结算时冻结背包价值快照。
- 金币已接入局内 HUD，为后续商店、出售或撤离经济预留入口。

### 背包与构筑

- `InventoryGrid` 是纯 C# 数据层，使用二维数组管理多格占用、放置、移除、查找空位、合并升级、邻接扫描和总价值统计。
- `BS.Inventory` 独立 asmdef 且不引用 `UnityEngine`，背包核心逻辑与表现层隔离，便于测试和维护。
- 背包 UI 支持拖拽、红绿预览、冲突判断、旋转、面板外丢弃、丢弃后再拾取、Tooltip 详情和 Tab 开关。
- 同名同级物品可 2 合 1 升级，升级会影响价值、芯片效果和武器伤害倍率。
- `AdjacencyRuleBook / AdjacencyEffectResolver` 将邻接规则和有效效果解析集中管理，避免用大量 if-else 写死构筑规则。
- 已实现 DualWield 双持、FireRateBoost 攻速芯片、DamageBoost 攻击芯片等构筑效果。

### 武器、数值与反馈

- `BackpackWeaponActivator` 根据背包内武器实例激活玩家身边的自动武器，默认上限为 1，DualWield 可突破为 2。
- `WeaponItemStatResolver` 按武器稀有度与等级提供伤害倍率，不同等级、稀有度和武器类型会产生不同战斗收益。
- `WeaponBase` 的最终伤害由“武器基础伤害 × 玩家升级倍率 × 背包武器倍率 × 攻击芯片倍率”组成。
- 命中闪白、玩家受击闪红、池化伤害数字、拾取/升级/开箱/开火/命中音效和轻量相机震动已经接入。
- 伤害数字与真实扣血在伤害源头统一取整，避免 UI 显示和实际战斗结果不一致。

### UI、场景与包装

- `MainMenu` 场景支持开始游戏、退出游戏、玩法说明和制作者声明面板。
- `01-Run` 场景完成基础氛围包装：地面贴图、边界提醒、宝箱模型、掉落物可读性、背包美术、物品图标和邻接接边表现。
- 背包物品使用透明 PNG 图标、稀有度底色、等级星星和灰/金接边展示可连接方向与已生效邻接。
- CanvasScaler 已按 `1920x1080 Scale With Screen Size` 调整，主菜单与 HUD 在不同窗口尺寸下保持稳定。

## 技术亮点

- **纯数据背包内核**：背包格子、占用、合并、旋转、邻接和价值统计都先在数据层完成，UI 只负责投影，降低表现层耦合。
- **可扩展邻接系统**：通过 `ItemTag + ConnectableSides + RuleBook + Resolver` 描述构筑规则，后续可以继续扩展芯片和互斥策略。
- **对象池体系**：敌人、子弹、经验、金币、装备掉落、宝箱和伤害数字都使用对象池，减少运行期频繁 Instantiate/Destroy。
- **单局导演系统**：`WaveDirector` 按时间推进刷怪压力、敌人血量、精英概率和宝箱品质，使 15 分钟体验具有节奏变化。
- **构筑影响战斗**：背包中的具体武器实例会影响自动武器激活、伤害倍率和芯片收益，而不是只停留在 UI 表现。
- **交付前验证**：使用 Profiler 快扫区分 Editor 开销和真实 PlayerLoop 热点，并完成正式 Windows Build 独立运行验收。

## 技术栈

- Unity `6000.3.20f1`
- C#
- Universal Render Pipeline `17.3.0`
- Unity Input System `1.19.0`
- Cinemachine `3.1.7`
- AI Navigation `2.0.13`
- UGUI / TextMeshPro
- ScriptableObject 配置
- Git / GitHub

## 工程结构

```text
Backpack Survivor/
├─ BackpackSurvivor/                         # Unity 工程
│  ├─ Assets/BackpackSurvivor/
│  │  ├─ Art/                                # 模型、材质、图标与视觉资源
│  │  ├─ Prefabs/                            # 敌人、子弹、掉落物、UI 等预制体
│  │  ├─ Scenes/
│  │  │  ├─ MainMenu/                        # 主菜单与玩法说明
│  │  │  ├─ Run/                             # 15 分钟单局场景
│  │  │  └─ Project/Input/                   # GameInput 输入资产
│  │  └─ Scripts/
│  │     ├─ Core/                            # 对象池、边界、通用接口
│  │     ├─ Data/                            # 掉落表与配置数据
│  │     ├─ GamePlay/                        # 战斗、敌人、掉落、单局、波次、玩家
│  │     ├─ Inventory/                       # 纯 C# 背包数据、物品、邻接规则
│  │     ├─ Presentation/                    # HUD、背包 UI、反馈表现
│  │     └─ Tests/                           # 测试与验证入口
│  ├─ Packages/
│  └─ ProjectSettings/
├─ Docs/                                     # 迭代日志、Bug 记录、性能记录和作品材料
├─ plan.md                                  # Demo 冲刺计划与阶段划分
└─ 《背包幸存者》游戏设计与实施方案.md        # GDD 与实施方案
```

## 本地运行

1. 克隆仓库：

   ```bash
   git clone https://github.com/cpz66l/BackpackSurvivor.git
   ```

2. 使用 Unity Hub 打开仓库中的 `BackpackSurvivor` 文件夹。
3. 推荐使用 Unity `6000.3.20f1`，或兼容的 Unity 6 编辑器版本。
4. 打开主菜单场景：

   ```text
   Assets/BackpackSurvivor/Scenes/MainMenu/MainMenu.unity
   ```

5. 进入 Play Mode 后点击开始游戏，进入 `01-Run` 单局场景。

## Build 说明

- 当前正式演示包版本：`v0.2.0`
- 平台：Windows
- Release 下载：[Backpack Survivor v0.2 Windows Demo](https://github.com/cpz66l/BackpackSurvivor/releases/tag/v0.2.0)
- 推荐窗口：`1600 x 900`，Windowed，可调整窗口大小
- Build 场景顺序：`MainMenu` → `01-Run`
- Build 输出目录：`Builds/BackpackSurvivor_v0.2_Windows/`
- Build 输出目录已被 `.gitignore` 忽略，仓库只保存源工程和配置，不提交 exe 与 Data 目录。

正式包已完成独立 exe 验收，覆盖主菜单、玩法说明、进入单局、战斗、拾取、宝箱、背包整理、邻接/芯片、结算、重开和返回主菜单。

## 验证与复盘

- C# 编译检查：`dotnet build BackpackSurvivor/BackpackSurvivor.sln --no-restore` 通过，0 warning / 0 error。
- Build 前检查：场景顺序、危险 using、`.meta` 完整性、Input System 引用和 Build Profile 已完成扫描。
- Profiler 快扫：已整理轻量截图证据包，记录在 `Docs/ProfilerEvidence/`。
- 性能结论：Editor 中可见的部分尖刺主要来自 EditorLoop / Live Display / 资源上传观察成本，正式 Build 后期波次试玩未出现明显卡顿。
- 已知非阻断项：`DefaultVolumeProfile.asset` 存在少量 Missing/Test Volume 组件，当前不影响 v0.2 正式包运行，后续视觉整理时清理。

## 开发日志

项目采用“每个迭代节点一个可验证目标”的方式推进，文档集中在 `Docs/` 目录中。

近期关键节点：

- [第 28 课：旋转邻接方向修正](./Docs/第28课-旋转邻接方向修正.md)
- [第 29 课：武器稀有度与等级差异](./Docs/第29课-武器稀有度与等级差异.md)
- [第 30 课：攻击芯片效果实装](./Docs/第30课-攻击芯片效果实装.md)
- [第 31 课：物品图标与背包可读性](./Docs/第31课-物品图标与背包可读性.md)
- [第 32 课：主菜单与场景流](./Docs/第32课-主菜单与场景流.md)
- [第 33 课：场景氛围与演示包装](./Docs/第33课-场景氛围与演示包装.md)
- [第 34 课：完整 15 分钟通关验收](./Docs/第34课-完整15分钟通关验收.md)
- [第 35 课：Profiler 快扫与低风险优化](./Docs/第35课-Profiler快扫与低风险优化.md)
- [第 36 课：Build 与演示包](./Docs/第36课-Build与演示包.md)

完整规划见：[plan.md](./plan.md)

## 后续优化方向

当前 v0.2 的重点已经从新增系统转向作品材料整理和求职展示。后续优化会优先围绕 Demo 表达和稳定性，而不是继续无止境扩功能。

- README、录屏脚本、截图清单、简历项目描述和面试技术亮点整理。
- 新手目标提示、局内规则提示和已生效构筑收益展示。
- 危险区表现强化、金币结算页、更多芯片效果、音效混音和 Boss 机制。
- 物品、规则和数值配置进一步 ScriptableObject 数据化。
- Demo 后可扩展 Gold 商店、出售/撤离经济、附近战利品面板和更完整的搜打撤循环。

## 求职展示重点

该项目主要用于展示 Unity 游戏客户端实习岗位所需的以下能力：

- 能从玩法设计出发拆分系统，并持续推进到可试玩、可打包、可复盘的 Demo。
- 能用 C# 编写模块化 Gameplay 代码，处理事件、对象池、UI、输入、配置和运行时状态。
- 能将核心玩法规则先做成纯数据逻辑，再接入 Unity 表现层，保持系统边界清晰。
- 能围绕玩家体验迭代：战斗反馈、背包可读性、伤害一致性、数值平衡、目标提示和 Build 稳定性。
- 能使用 Git、Profiler、Bug 记录和开发日志沉淀工程过程，并把项目经验转化为可面试表达的技术亮点。

## 项目说明

本仓库为个人学习、玩法验证与求职作品展示项目。当前 v0.2 已经形成可独立运行的 Windows Demo，后续主要围绕作品材料、展示录屏、简历表达和少量 Demo 后优化继续推进。
