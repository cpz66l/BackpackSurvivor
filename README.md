# Backpack Survivor / 背包幸存者

> Unity 3D 俯视角生存射击 Demo。项目融合幸存者战斗、掉落搜刮、网格背包构筑、合并升级、邻接联动与 15 分钟单局节奏，由个人独立设计、开发和持续复盘。

![Unity](https://img.shields.io/badge/Unity-6000.3.20f1-000000?logo=unity)
![CSharp](https://img.shields.io/badge/C%23-Gameplay-512BD4?logo=csharp)
![URP](https://img.shields.io/badge/URP-17.3.0-blue)
![Status](https://img.shields.io/badge/Status-Demo%20In%20Progress-orange)

## 项目概览

《背包幸存者》是一款个人独立开发的 Unity 3D 俯视角生存射击项目。玩家需要在持续增强的敌人波次中移动、战斗、搜刮宝箱和掉落物，并通过有限的网格背包完成武器摆放、合并升级和邻接构筑，最终在 15 分钟单局中争取生存、成长和结算收益。

这个项目的重点不只是实现单个功能，而是把一个玩法想法逐步推进成可运行、可迭代、可复盘的游戏工程：

- 战斗侧：主动射击、自动武器、敌人追击、危险区、投射物命中和伤害结算。
- 搜刮侧：经验球、金币、装备、宝箱、权重掉落、保底机制和磁吸拾取。
- 构筑侧：网格背包、多格占用、拖拽、旋转、丢弃、合并升级和邻接效果。
- 单局侧：15 分钟计时、经验升级三选一、波次导演、金币 HUD、血量/时间/等级 HUD、胜负结算与重开。

## 当前进度

截至 2026-08-04，项目已推进到 **第 27 课：数值调参与首轮平衡**，核心状态如下：

| 维度 | 当前状态 |
| --- | --- |
| 代码规模 | 36 次 Git 提交、67 个 C# 脚本、约 4.7k 行 C# 代码 |
| 开发阶段 | 15 分钟 Demo 冲刺期，核心玩法闭环已初步跑通 |
| 可玩性 | 已完成一轮 15 分钟试玩验证，终局压力、宝箱路线和背包整理行为成立 |
| 主要入口 | `Assets/BackpackSurvivor/Scenes/Run/01-Run.unity` |
| 当前重点 | 新手目标提示、武器稀有度/等级差异、攻击芯片效果、旋转邻接修正、主菜单和 Build 包装 |

> 说明：项目仍处于 Demo 开发中，README 中的“后续计划”不代表已经完成，实际进度以代码、提交记录和 `Docs/` 开发日志为准。

## 核心玩法循环

```text
进入单局
  -> 移动与战斗，清理敌群
  -> 拾取经验、金币、装备，寻找宝箱
  -> 打开背包，摆放/旋转/合并物品
  -> 通过武器激活、双持、芯片邻接提升战斗力
  -> 波次强度提升，精英和宝箱奖励期望提高
  -> 胜利/失败后进入结算并可重开
```

## 已实现系统

### 战斗与单局

- `IDamageable / Health / DamageInfo / WeaponBase` 统一伤害管线，主动武器和自动武器复用投射物命中逻辑。
- `TargetRegistry` 管理自动索敌目标，自动武器可根据最近敌人持续开火。
- `EnemyAI / EnemySpawner / WaveDirector` 支持敌人追击、刷怪上限、精英生成率、波次刷怪间隔和敌人血量成长。
- `GameSession / RunTimer / GameState / RunResult` 负责单局状态、15 分钟倒计时、暂停、死亡失败、时间到胜利、结算和重开。
- `RunHudView / ResultView` 显示血量、时间、经验、等级、金币、波次、结算统计等关键信息。

### 掉落、搜刮与经济

- `LootTableData / LootRoller / LootManager` 支持按权重和保底机制生成经验、金币、装备和宝箱奖励。
- `XpOrb / GoldOrb / DropItem / LootChest` 分别处理经验、金币、装备和宝箱交互。
- 掉落物支持散落飞出、磁吸拾取、交互拾取、超时回收和对象池复用。
- 金币已接入局内 HUD，背包总价值已接入结算快照，便于后续扩展评分和经济系统。
- 宝箱会随波次调整品质权重，并在 HUD 中显示最近未开宝箱距离，强化路线选择。

### 背包与构筑

- `InventoryGrid` 使用纯 C# 二维数组管理网格，占格、放置、移除、查找空位、合并升级和背包总价值计算都在数据层完成。
- 背包 UI 支持拖拽、放置预览、冲突提示、旋转、丢弃、物品等级显示和 Tooltip 详情展示。
- `Item` 拆分基础值与当前值，`ScoreValue / EffectValue` 会随等级成长，合并升级能真实影响价值和战斗收益。
- `AdjacencyRuleBook / AdjacencyEffectResolver` 处理邻接规则与有效效果筛选，避免“看起来相邻但规则不成立”的假触发。
- `BackpackWeaponActivator` 根据背包内武器激活玩家身边的自动武器，并支持双持、攻速芯片等构筑收益。

### 反馈与表现

- 命中闪白、玩家受击反馈、池化伤害数字、升级/拾取/开箱/开火/命中音效、轻量相机震动已经接入。
- 物品 Tooltip 将稀有度、尺寸、价值、效果等细节从格子中拆出，提升背包可读性。
- 首轮数值平衡中已统一真实伤害与伤害数字显示，避免 UI 显示和实际扣血不一致。

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
│  │  ├─ Art/                                # 原型美术、模型和材质资源
│  │  ├─ Prefabs/                            # 可复用游戏对象
│  │  ├─ Scenes/                             # Run 场景与输入资源
│  │  └─ Scripts/
│  │     ├─ Core/                            # 对象池、边界、通用接口
│  │     ├─ Data/                            # 掉落表与配置数据
│  │     ├─ GamePlay/                        # 战斗、敌人、掉落、单局、波次、玩家
│  │     ├─ Inventory/                       # 背包数据、物品、邻接规则
│  │     ├─ Presentation/                    # HUD、背包 UI、反馈表现
│  │     └─ Tests/                           # 后续测试入口
│  ├─ Packages/
│  └─ ProjectSettings/
├─ Docs/                                     # 逐课开发日志、复盘与 Bug 记录
├─ plan.md                                  # Demo 冲刺计划与里程碑
└─ 《背包幸存者》游戏设计与实施方案.md        # GDD 与实施方案
```

## 本地运行

1. 克隆仓库：

   ```bash
   git clone https://github.com/cpz66l/BackpackSurvivor.git
   ```

2. 使用 Unity Hub 打开仓库中的 `BackpackSurvivor` 文件夹。
3. 推荐使用 Unity `6000.3.20f1`，或兼容的 Unity 6 编辑器版本。
4. 打开运行场景：

   ```text
   Assets/BackpackSurvivor/Scenes/Run/01-Run.unity
   ```

5. 进入 Play Mode 体验当前 Demo 原型。

## 开发日志

项目采用“每课一个可验证目标”的方式推进，文档集中在 `Docs/` 目录中。

近期关键节点：

- [第20课：胜负结算与重开闭环](./Docs/第20课-胜负结算与重开闭环.md)
- [第21课：构筑最小兑现](./Docs/第21课-构筑最小兑现.md)
- [第22课：内容面铺开](./Docs/第22课-内容面铺开.md)
- [第23课：精英宝箱与终局压力强化](./Docs/第23课-精英宝箱与终局压力强化.md)
- [第24课：金币掉落与局内经济 HUD](./Docs/第24课-金币掉落与局内经济HUD.md)
- [第25课：背包价值与物品价值显示](./Docs/第25课-背包价值与物品价值显示.md)
- [第26课：合并升级收益兑现](./Docs/第26课-合并升级收益兑现.md)
- [第27课：数值调参台与首轮平衡](./Docs/第27课-数值调参台与首轮平衡.md)

完整规划见：[plan.md](./plan.md)

## 后续计划

Demo 冲刺期剩余重点：

- 新手目标提示与局内可读性：开局提示、当前目标、背包满/可合并/可邻接提示。
- 武器稀有度与等级差异：让不同稀有度/等级武器影响实际激活后的战斗数值。
- 攻击芯片效果扩展：在 FireRateBoost 之外加入 DamageBoost 等第一类攻击芯片。
- 旋转邻接方向修正：物品旋转后接口方向、UI 接口点和规则检测保持一致。
- 主菜单与 Build 流程：补齐开始游戏、退出游戏、结算返回和 Windows Build。
- 演示包装：README、录屏脚本、截图清单、简历项目描述和技术亮点提炼。

## 求职展示重点

该项目主要用于展示 Unity 游戏客户端实习岗位所需的以下能力：

- 能从玩法设计出发拆分系统，并持续推进到可运行 Demo。
- 能用 C# 编写模块化 Gameplay 代码，处理事件、对象池、UI、配置和运行时状态。
- 能把背包规则等核心玩法先做成纯数据逻辑，再接入 UGUI 表现层。
- 能围绕玩家体验做迭代：战斗反馈、伤害显示一致性、数值平衡、目标提示和可读性。
- 能持续记录开发日志、Bug 复盘和面试问答，把项目经验沉淀为可表达的工程能力。

## 项目说明

本仓库为个人学习、玩法验证与求职作品展示项目。当前仍在持续开发中，目标是在 2026 年 8 月中旬完成一个可录屏、可试玩、可用于简历展示的 Windows Demo。
