# Backpack Survivor / 背包幸存者

> Unity 3D 俯视角生存射击 Demo。玩家在 15 分钟单局中移动、射击、搜刮、整理背包，并通过武器激活、合并升级、邻接芯片、局内成长和本地纪录形成构筑循环。

![Unity](https://img.shields.io/badge/Unity-6000.3.20f1-000000?logo=unity)
![CSharp](https://img.shields.io/badge/C%23-Gameplay-512BD4?logo=csharp)
![URP](https://img.shields.io/badge/URP-17.3.0-blue)
![Status](https://img.shields.io/badge/Status-v0.3%20Windows%20Demo-success)

## 项目概览

《背包幸存者》是一款个人独立开发的 Unity 3D 俯视角生存射击 Demo。项目融合幸存者战斗、掉落搜刮、网格背包构筑、合并升级、邻接联动、升级三选一、波次压力、金币与物品价值结算，目标是在一个可独立运行的 15 分钟单局中呈现“边战斗、边搜刮、边整理构筑”的核心体验。

当前 v0.3 已经完成 Windows Build 验收：在 v0.2 完整闭环基础上，进一步补充了升级候选池、更多背包构筑效果、内容池扩展、基础音频与 BGM、设置菜单、敌人群体移动优化、远程敌人波次混编和本地存档纪录。

## 试玩下载

Windows 可试玩包发布在 GitHub Releases：

- [下载 Backpack Survivor v0.3 Windows Demo](https://github.com/cpz66l/BackpackSurvivor/releases/tag/v0.3.0)
- [观看项目展示视频](https://t.bilibili.com/1234504825008291859?share_source=pc_native)

下载后请先解压整个压缩包，再双击 `BackpackSurvivor.exe` 启动。不要只单独运行 exe，游戏还需要同目录下的 `BackpackSurvivor_Data`、`UnityPlayer.dll`、`MonoBleedingEdge` 等文件。

## 当前版本

| 维度 | 状态 |
| --- | --- |
| 当前版本 | v0.3 Windows Demo |
| 最近状态 | v0.3 正式包已完成独立运行验收 |
| 开发周期 | 2026-07-19 开始，2026-08-20 完成 v0.3 Build 验收 |
| 工程规模 | 63 次 Git 提交、86 个 C# 脚本、约 7.0k 行 C# 代码 |
| 主要入口 | `Assets/BackpackSurvivor/Scenes/MainMenu/MainMenu.unity` |
| 单局目标 | 15 分钟生存、成长、搜刮与结算 |
| Build 验收 | Windows 独立 exe 已通过完整试玩验证 |

> 说明：项目设计、功能取舍、实现落地与 Unity 内接线均由本人独立完成，AI 主要作为任务拆解、代码 review、调试分析、重复样板提效和复盘整理的辅助工具。公开仓库重点展示可试玩版本、核心系统、技术取舍和验证结论。

## 核心玩法

```text
主菜单
  -> 阅读玩法说明 / 调整设置 / 查看本地纪录 / 开始游戏
  -> 移动、主动射击、自动武器战斗
  -> 击杀敌人，拾取经验、金币和装备
  -> 寻找并开启不同稀有度宝箱
  -> 打开背包，摆放、旋转、合并、丢弃和重新拾取物品
  -> 背包武器激活自动武器，邻接芯片和被动物品强化战斗表现
  -> 升级三选一扩展攻击、生存、机动、搜刮和构筑方向
  -> 波次强度持续提升，精英敌人、远程敌人和高品质宝箱逐步出现
  -> 胜利或失败后进入结算，胜利会写入本地战绩记录
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

- `IDamageable / Health / DamageInfo / WeaponBase` 形成统一伤害管线，主动武器、自动武器和敌方投射物复用命中逻辑。
- `AutoWeapon / ActiveWeapon / Projectile` 支持自动索敌、主动射击、投射物扫掠检测、阵营过滤、暴击标记和真实伤害结算。
- `EnemyAI / RangedEnemyAI / EnemyMovement` 支持近战追击、远程停距射击、贴脸后退、局部分离、障碍避让和方向错峰采样。
- `EnemySpawner / WaveDirector` 支持普通、精英、远程敌人的分池生成、刷怪间隔、场上上限、血量成长、精英概率和远程敌人概率。
- `GameSession / RunTimer / GameState / RunResult` 负责 15 分钟倒计时、暂停、死亡失败、时间到胜利、结算快照、重开和返回主菜单。
- `RunHudView / ResultView` 显示血量、时间、等级、经验圆环、金币、波次名称、宝箱距离、背包价值和结算数据。

### 掉落、宝箱与经济

- `LootTableData / LootRoller / LootManager` 支持权重掉落、保底机制、束表递归和经验/金币/装备分频道生成。
- `XpOrb / GoldOrb / DropItem / LootChest` 覆盖经验、金币、装备和宝箱，支持对象池、散落飞出、磁吸、交互拾取和超时回收。
- 宝箱品质会随波次阶段调整，高压后期更容易出现高稀有度宝箱。
- 背包内物品带有 `scoreValue`，局内显示单件价值和背包总价值，结算时冻结背包价值快照。
- 金币已接入局内 HUD 和本地纪录，胜利后累计为局外金币数，为后续商店或局外成长预留入口。

### 背包与构筑

- `InventoryGrid` 是纯 C# 数据层，使用二维数组管理多格占用、放置、移除、查找空位、合并升级、邻接扫描和总价值统计。
- `BS.Inventory` 独立 asmdef 且不引用 `UnityEngine`，背包核心逻辑与表现层隔离，便于测试和维护。
- 背包 UI 支持拖拽、红绿预览、冲突判断、旋转、面板外丢弃、丢弃后再拾取、Tooltip 详情和 Tab 开关。
- 同名同级物品可 2 合 1 升级，升级会影响价值、芯片效果和武器伤害倍率。
- `AdjacencyRuleBook / AdjacencyEffectResolver / BackpackEffectCollector` 将邻接规则、有效效果解析和数值汇总集中管理，避免用大量 if-else 写死构筑规则。
- 已实现 DualWield 双持、FireRateBoost 攻速芯片、DamageBoost 攻击芯片、CritBoost 瞄准镜、MechanicalArm 激活上限、Armor 减伤和 MagnetCore 拾取范围等构筑效果。

### 升级、数值与反馈

- `LevelUpOptionGenerator` 已从固定 3 个选项扩展为候选池，支持分类、权重、等级门槛、同轮不重复和最大选择次数。
- `PlayerRunStats` 统一承接伤害、射速、暴击、弹速、射程、生命、减伤、移速、拾取范围、经验倍率、金币倍率和武器上限等运行期属性。
- `BackpackWeaponActivator` 根据背包内武器实例激活玩家身边的自动武器，并支持按武器类型配置激活槽位。
- `WeaponItemStatResolver` 按武器稀有度与等级提供伤害倍率，不同等级、稀有度和武器类型会产生不同战斗收益。
- `WeaponBase` 的最终伤害由“武器基础伤害 × 玩家升级倍率 × 背包武器倍率 × 芯片/邻接倍率 × 暴击倍率”组成。
- 命中闪白、玩家受击闪红、池化伤害数字、拾取/升级/开箱/开火/UI/胜负音效、BGM 和轻量相机震动已经接入。

### UI、场景与包装

- `MainMenu` 场景支持开始游戏、退出游戏、设置、历史纪录、玩法说明和制作者声明面板。
- 设置面板支持 Master / SFX / Music 音量、分辨率和窗口模式，使用 `PlayerPrefs` 持久化并跨场景生效。
- `SaveData / SaveService / MainMenuRecordView` 支持总局数、胜场、最高背包价值、局外金币、传说带出数量和传说累计价值的本地 JSON 记录。
- `01-Run` 场景完成基础氛围包装：地面贴图、边界提醒、宝箱模型、掉落物可读性、背包美术、物品图标和邻接接边表现。
- 背包物品使用透明 PNG 图标、稀有度底色、等级星星和灰/金接边展示可连接方向与已生效邻接。
- CanvasScaler 已按 `1920x1080 Scale With Screen Size` 调整，主菜单与 HUD 在不同窗口尺寸下保持稳定。

## 技术栈

- Unity `6000.3.20f1`
- C#
- Universal Render Pipeline `17.3.0`
- Unity Input System `1.19.0`
- Cinemachine `3.1.7`
- AI Navigation `2.0.13`
- UGUI / TextMeshPro
- ScriptableObject 配置
- JSON 本地存档
- Git / GitHub

## 工程结构

```text
Backpack Survivor/
├─ BackpackSurvivor/                         # Unity 工程
│  ├─ Assets/BackpackSurvivor/
│  │  ├─ Art/                                # 模型、材质、图标、字体与视觉资源
│  │  ├─ Prefabs/                            # 敌人、子弹、掉落物、UI 等预制体
│  │  ├─ Scenes/
│  │  │  ├─ MainMenu/                        # 主菜单、设置、纪录与玩法说明
│  │  │  ├─ Run/                             # 15 分钟单局场景
│  │  │  └─ Project/Input/                   # GameInput 输入资产
│  │  └─ Scripts/
│  │     ├─ Core/                            # 对象池、边界、通用接口
│  │     ├─ Data/                            # 掉落表与配置数据
│  │     ├─ GamePlay/                        # 战斗、敌人、掉落、单局、波次、玩家、存档、设置、升级
│  │     ├─ Inventory/                       # 纯 C# 背包数据、物品、邻接规则
│  │     ├─ Presentation/                    # HUD、背包 UI、音频、反馈、菜单、结算
│  │     └─ Tests/                           # 测试与验证入口
│  ├─ Packages/
│  └─ ProjectSettings/
├─ Docs/                                     # 版本复盘、Bug 记录和性能证据
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

- 当前正式演示包版本：`v0.3.0`
- 平台：Windows
- Release 下载：[Backpack Survivor v0.3 Windows Demo](https://github.com/cpz66l/BackpackSurvivor/releases/tag/v0.3.0)
- 项目展示视频：[Bilibili](https://t.bilibili.com/1234504825008291859?share_source=pc_native)
- 推荐窗口：`1600 x 900`，Windowed，可调整窗口大小
- Build 场景顺序：`MainMenu` -> `01-Run`
- Build 输出目录：`Builds/BackpackSurvivor_v0.3_Windows/`
- Build 输出目录已被 `.gitignore` 忽略，仓库只保存源工程和配置，不提交 exe 与 Data 目录。

正式包已完成独立 exe 验收，覆盖主菜单、玩法说明、设置、历史纪录、进入单局、战斗、拾取、宝箱、背包整理、升级、邻接/芯片、远程敌人、结算、重开和返回主菜单。

## 验证与复盘

- C# 编译检查：`dotnet build BackpackSurvivor/BackpackSurvivor.sln --no-restore` 通过，0 error；保留少量 Unity Inspector 序列化字段警告。
- Build 前检查：场景顺序、危险 using、`.meta` 完整性、Input System 引用、Development Build 和 Build Profile 已完成扫描。
- Profiler 快扫：已整理轻量截图证据包，记录在 `Docs/ProfilerEvidence/`。
- 性能结论：Editor 中可见尖刺主要来自资源预加载、贴图上传、GPU 等待或 Editor/Profiler 观察成本，正式 Build 试玩未出现阻断性卡顿。
- Build 体验：用户完成 v0.3 打包后实测，窗口/分辨率设置、生存节奏、音频、远程敌人、本地存档和单局流程均能正常工作。

## 项目复盘

公开文档保留对作品判断有帮助的版本复盘、Bug 记录和性能证据，避免把内部过程资料包装成教程式阅读路径。

推荐阅读：

- [V0.1 阶段复盘：战斗核心原型](./Docs/V0.1阶段复盘.md)
- [V0.2 版本复盘：15 分钟可试玩 Demo](./Docs/V0.2版本复盘.md)
- [V0.3 版本复盘：内容深度、反馈与留存](./Docs/V0.3版本复盘.md)
- [Bug 记录簿](./Docs/Bug记录簿.md)
- [性能优化记录](./Docs/性能优化记录.md)
- [Profiler 快扫证据包](./Docs/ProfilerEvidence/README.md)

## 后续优化方向

当前 v0.3 已经从“完整可试玩”推进到“具备内容扩展、反馈包装和局外记录”的作品阶段。后续优化会优先服务项目展示质量和系统深度，而不是无边界扩功能。

- 增加更多升级选项、背包被动物品、芯片流派和构筑收益展示。
- 扩展局外金币用途，例如商店、开局加成或收藏目标。
- 补充更多敌人类型、攻击方式、弹幕预警和阶段性强敌。
- 优化音频混音、命中音色、低血量提示和更完整的 AudioMixer 路由。
- 为存档增加版本迁移、重置入口和更清晰的历史纪录展示。
- 继续用 Profiler 验证真实瓶颈，再决定是否引入更复杂的导航、数据化或性能架构。

## 求职展示重点

该项目主要用于展示 Unity 游戏客户端实习岗位所需的以下能力：

- 能从玩法设计出发拆分系统，并持续推进到可试玩、可打包、可复盘的 Demo。
- 能用 C# 编写模块化 Gameplay 代码，处理事件、对象池、UI、输入、配置、音频、存档和运行时状态。
- 能将核心玩法规则先做成纯数据逻辑，再接入 Unity 表现层，保持系统边界清晰。
- 能围绕玩家体验迭代：战斗反馈、背包可读性、伤害一致性、数值平衡、目标提示、设置和 Build 稳定性。
- 能使用 Git、Profiler、Bug 记录和版本复盘沉淀工程过程，并把项目经验转化为可面试表达的项目重点。

## 项目说明

本仓库为个人学习、玩法验证与求职作品展示项目。当前 v0.3 已经形成可独立运行的 Windows Demo，后续主要围绕系统深度、内容扩展、展示材料和少量 Demo 后优化继续推进。

## 使用与授权说明

本项目的源码、文档和 Demo 公开仅用于学习交流、技术评估、作品展示和招聘面试参考。未经作者许可，不得将本项目或其修改版本用于商业用途、二次发布、打包转载，或声称为自己的原创作品。

项目中的部分美术、字体、音效、模型、图标或占位资源仅用于 Demo 展示和学习验证。若需商业使用或二次开发，请自行替换相关资源并确认授权。

详细授权边界见：[LICENSE.md](./LICENSE.md)。
