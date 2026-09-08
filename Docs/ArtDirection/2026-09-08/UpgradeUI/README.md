# 获批简化版升级三选一 UI

已按用户确认的简洁插画设计完成 UI 实现：保留「选择一项强化」、等级、三张蓝灰卡片和一行操作提示；每张卡片展示类别、独立装备插画、升级名称和原始效果。卡框与文字由 Unity 原生 UGUI 绘制，插画使用真实透明图集，与夜战回收场的蓝灰色调一致。

目标场景为 [01-Run_ArtFull.unity](<E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor/Assets/BackpackSurvivor/Scenes/Run/01-Run_ArtFull.unity>)，地图半径仍为 60 米。13 项插画、实际文案、资源、输入和暂停恢复检查均已通过，场景已保存并退出 Play 模式。

## Unity 实际画面

回收与构筑选项已具备独立插画：磁吸背包、战术记录终端和扩展武器挂架。截图使用固定审阅组合便于比较，正常游戏仍按原等级门槛与概率抽取。

![Unity 中的回收与构筑升级卡片](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/unity-upgrade-recovery-build.png>)

[查看火力、生存、机动组合](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/unity-upgrade-main.png>) · [查看 1024×768 窗口效果](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/unity-upgrade-1024x768.png>)。

## 设计与交互

布局基准为 **1360×848**，单卡 **400×550**、中心间距 **426**，按窗口可用空间等比缩放。插画显示区域为 328×224，保持原始比例；只有 1 或 2 项可选时自动居中。标题与效果使用项目现有思源黑体 SDF，允许在限定字号范围内自动缩放。

- 初次打开不选中、不默认高亮任何卡片；鼠标悬停或方向键移动焦点后才出现强调效果。
- 点击整张卡片，或按主键盘／小键盘 1、2、3 直接选择。
- 左右方向键移动焦点，回车／空格确认当前确实选中的卡片。点击背景取消焦点后，回车不使用旧的缓存索引提交。
- Tab 已移出升级导航，避免与原背包快捷键共用。
- 升级时淡出底层 HUD，并阻挡底层点击；关闭后恢复其原透明度和交互设置。Canvas、GameSession 与升级控制器继续保持活动。
- 入场与悬停动画使用未缩放时间，在 `Time.timeScale=0` 时仍能响应；天气与战斗继续按游戏暂停规则冻结。

升级仍走原来的 `GameSession` 与 `LevelUpOptionGenerator`：**没有调整抽取概率、最低等级、可选次数、数值或效果**。图集按 `LevelUpOptionId` 精确绑定，新增插画不改变玩家能抽到哪些选项。选择入口保留提交锁与状态检查；0 项结果使用原有完成选择流程退出暂停。

## 5 类、13 项独立插画

同一类别中的不同强化也有各自插画。

| 类别 | 升级名称 | 原始效果 | 插画 / Sprite 名 |
|---|---|---|---|
| 火力 | 火力强化 | 伤害 +15% | 战术步枪 / `DamageUp` |
| 火力 | 快速射击 | 射速 +15% | 加强双弹匣 / `FireRateUp` |
| 火力 | 精准校准 | 暴击率 +10% | 精密瞄具 / `CritChanceUp` |
| 火力 | 高速弹体 | 子弹速度 +15% | 加速枪弹 / `ProjectileSpeedUp` |
| 火力 | 扩展索敌 | 武器射程 +15% | 光学望远镜 / `WeaponRangeUp` |
| 火力 | 弱点打击 | 暴击伤害 +25% | 击穿装甲 / `CritDamageUp` |
| 生存 | 应急装甲 | 最大生命值 +25 | 医疗装甲背心 / `MaxHpUp` |
| 生存 | 战术护甲 | 受到伤害 -10% | 防护装甲板 / `DamageReductionUp` |
| 机动 | 轻装移动 | 移速 +10% | 战术靴 / `MoveSpeedUp` |
| 回收 | 磁吸背包 | 拾取范围 +20% | 磁吸背包 / `PickupRangeUp` |
| 回收 | 战斗学习 | 经验获取 +20% | 战术记录终端 / `XpGainUp` |
| 回收 | 淘金直觉 | 金币获取 +20% | 金币补给袋 / `GoldGainUp` |
| 构筑 | 扩展武装槽 | 激活武器上限 +1 | 扩展武器挂架 / `ActiveWeaponLimitUp` |

下面是最终透明图集在深蓝底上的检查图，**不是 Unity 运行截图**。[简化版概念图](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/approved-concept.png>) 留作设计参考。

![13 项插画在深蓝底上的透明度检查](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/illustrations-on-dark.png>)

[查看浅灰背景轮廓检查图](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/illustrations-on-grey.png>)。去底、边缘处理、切分坐标和来源详见 [插画资产说明](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/illustration-assets.md>)。

## 轻量资源预算

| 项目 | 配置 / 大小 |
|---|---|
| 图集 | 1 张 1024×1024 RGBA PNG，13 个独立命名 Sprite |
| PNG 磁盘文件 | **797,206 bytes，约 778.5 KiB** |
| Windows Standalone | **BC7、无 Mipmap，纹理载荷 1 MiB** |
| Unity 编辑器纹理原生资源估算 | **2,098,048 bytes，约 2.00 MiB** |
| 13 个 Sprite 原生资源估算合计 | 19,513 bytes，约 19.1 KiB |
| 未压缩 RGBA8 对照 | 同尺寸无 Mipmap 的像素数据为 4 MiB |
| 采样 | sRGB、Bilinear、Clamp、Non-readable |
| Sprite 几何 | Full Rect，不生成物理轮廓 |
| 字体 | 复用现有思源黑体 SDF |
| 独立 UI 材质 / 动画控制器 | 不新增，使用 UGUI 材质与轻量脚本动画 |

**1 MiB 是 BC7 图集的纹理载荷，不包含 Sprite／对象开销、共享字体、场景其他资源或总显存。** PNG 文件大小与运行时内存不同。2.00 MiB 来自本次运行中 `Profiler.GetRuntimeMemorySizeLong(texture)` 对编辑器对象的原生资源估算，不等同于 BC7 载荷或准确显存；完整数值已写入审计 JSON，不据此推断帧率。

13 个图案位于 4×4 图集的前 13 格，其余格透明。每格保留透明间距，Sprite 使用紧边界加 padding；通过稳定 ID 重导入，避免重新切图后丢失卡片引用。

## 打开与重建

在非 Play 模式打开 [完整地图场景](<E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor/Assets/BackpackSurvivor/Scenes/Run/01-Run_ArtFull.unity>)，执行：

`Tools / Backpack Survivor / Art / Apply Approved Illustrated Upgrade UI`

该入口调用 `UpgradeIllustrationImporter.ApplyApprovedUiAndSave()`，保留未保存场景快照，导入图集、重建新版 UI 并保存场景。重复执行会重建自有 `NightUpgradeChoice` 和 `NightUpgradeController`，不会叠加控制器。原 `Apply Chests Night and Upgrade UI to Full Map` 与完整地图构建入口也使用当前 UI Builder。

需要重新处理原始插画时，在仓库根目录运行：

```powershell
python Tools/ArtPipeline/prepare_upgrade_illustrations.py
```

该脚本使用 Pillow、NumPy、SciPy 进行本地去底、透明边缘处理、缩放和图集压缩，输出 PNG 与切分清单。随后重新执行上述 Unity 菜单。菜单 `Import Upgrade Illustrations` 只负责导入切片，`Apply Night Upgrade UI` 只重建当前场景 UI，不自动保存。

主要文件：

- [最终图集](<E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor/Assets/BackpackSurvivor/Art/UI/Upgrades/UpgradeIllustrations.png>)、[切分清单](<E:/YouXiKaiFa/Backpack Survivor/Tools/ArtPipeline/Source/upgrade-illustrations-manifest.json>)、[图集处理脚本](<E:/YouXiKaiFa/Backpack Survivor/Tools/ArtPipeline/prepare_upgrade_illustrations.py>)。
- [UpgradeIllustrationImporter.cs](<E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor/Assets/BackpackSurvivor/Editor/UpgradeIllustrationImporter.cs>) 控制压缩与稳定切片；[NightUpgradeUIBuilder.cs](<E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor/Assets/BackpackSurvivor/Editor/NightUpgradeUIBuilder.cs>) 控制布局与字号。
- [UpgradeChoiceCard.cs](<E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor/Assets/BackpackSurvivor/Scripts/Presentation/LevelUp/UpgradeChoiceCard.cs>) 控制插画绑定、类别色和悬停；[LevelUpChoiceView.cs](<E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor/Assets/BackpackSurvivor/Scripts/Presentation/LevelUp/LevelUpChoiceView.cs>) 控制输入、提交和 HUD 恢复。

## 验证状态

已读取真实 Play 模式生成的 [illustration-ui-audit.json](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/illustration-ui-audit.json>)，`allPassed=true`：

| 检查 | 本次结果 |
|---|---|
| 实际定义与图文 | 13/13 对应正确，标题与原始效果逐项匹配，无截断 |
| 图像资源 | 13 个独立 Sprite，共用 1 张 1024² BC7 纹理 |
| 可绘制内容 | 各项插画与文字均具有有效 CanvasRenderer 网格 |
| 初次打开 | 所有审阅分页均无默认选中对象 |
| UI Shader | 错误 0、警告 0 |
| 审阅副作用 | 随机状态恢复；属性、选择次数、等级、经验均未变化 |
| 结束状态 | 恢复 Running，timeScale=1 |

补充交互检查已通过 [input-ui-audit.json](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/input-ui-audit.json>)，`allPassed=true`。虚拟键盘的按下与释放由正常游戏帧处理，没有手动调用 InputSystem.Update：无焦点 Enter／Space 不提交；右、右、左依次选择第 1、2、1 张；清除焦点后 Enter 不提交；Tab 确实到达 InputReader，但背包保持原状态；数字 2 只应用一次强化，重复按下不重复应用；HUD 恢复；第二次真实经验升级仍无默认焦点。测试后输入设备、配对和设置全部恢复。

[真实按钮验证](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/upgrade-runtime-check.json>) 也通过：真实经验触发后选择战斗学习，经验倍率由 1 变为 1.2，重复按钮事件只产生一次属性变更，并恢复 Running／timeScale=1。[0–3 项边界](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/upgrade-offer-count-check.json>) 全部通过。

[截图检查](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/visual-review.json>) 覆盖 1600×900 与 1024×768。[最终编辑器状态](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/final-editor-audit.json>) 使用 Unity 原生计数，错误 0、警告 0、普通日志 1；场景无未保存修改，后台运行设置已恢复。截图实验曾在 MCP 回调中调用 EditorApplication.Step，触发 PlayerLoop 递归诊断；已停用该截图方式并保留 [诊断记录](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/editor-capture-diagnostic.json>)，改用正常更新帧后重新验证通过。MCP 文本过滤还会把成功日志堆栈的 `Finish(System.Exception)` 误判为异常，因此最终以原生控制台计数为准。

审计由 [SimpleUpgradeAudit.cs](<E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor/Assets/BackpackSurvivor/Editor/SimpleUpgradeAudit.cs>) 的 `CheckAllIllustrations()` 在 Play 中执行：从当前实际定义分批展示 13 项，检查名称与效果、Sprite 对应、1 张 1024² BC7 纹理、文本截断、可绘制网格、初始焦点与 Shader 错误。不授予升级效果，结束时恢复 Running 与随机状态，并核对等级、经验、属性和选择次数未变化。

`ShowReviewOffer(0–4)` 仅供固定分组的画面检查，保留 Selecting 状态；它展示实际定义，不修改定义的等级门槛或权重。真实经验触发、按钮／键盘、暂停恢复和 HUD 状态由上述独立运行检查验证；本目录集中保存本次简化插画界面的验收结果。
