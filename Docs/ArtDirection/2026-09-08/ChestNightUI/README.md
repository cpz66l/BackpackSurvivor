# 夜战回收场：宝箱、氛围与升级界面

本轮已将五级掉落宝箱、轻量夜战粒子和升级界面接入 [01-Run_ArtFull.unity](<E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor/Assets/BackpackSurvivor/Scenes/Run/01-Run_ArtFull.unity>)。地图仍为 **半径 60 米、直径 120 米**，沿用已有实体边界、中央设施和可受光照影响的地面材质。

**最新 UI：** 用户已批准简化插画设计，已完成三张独立卡片与 5 类、13 项专属插画的实现。新版资源、布局、重建入口和当前测试状态统一记录在 [UpgradeUI/README.md](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/README.md>)。本页继续作为宝箱与夜战模块的交付说明；下方 UI 运行结果已同步最新验证。

## 实际画面

夜战地图保留冷蓝灰钢材、地面纹路与局部暖灯，玩家附近增加低密度风尘和少量细雨。

![Unity 夜战运行画面](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/ChestNightUI/unity-night-gameplay.png>)

[五级宝箱并排渲染](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/ChestNightUI/chest-lineup.png>) 由最终 GLB 在 Blender 中渲染。新版三选一的概念参考、插画透明度检查与 Unity 验收记录见 [新版 UI 文档](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/README.md>)，其中概念图和插画检查图均与 Unity 实际截图区分。

## 五级掉落宝箱

五档宝箱共用蓝灰钢壳与炭黑结构件，沿用现有稀有度主色，同时用轮廓、装甲、锁具及 1–5 道标记区分级别。史诗档保留项目实际使用的靛蓝色。

| 等级 | 主色与轮廓 | 三角形 | GLB 文件 |
|---|---|---:|---:|
| Common／普通 | 白色工具箱、提把、中央单扣 | 924 | 71.4 KiB |
| Uncommon／不普通 | 青绿加强箱、双扣、贯穿绑带 | 1,100 | 84.5 KiB |
| Rare／稀有 | 浅蓝运输箱、四角装甲、加厚顶肋 | 1,320 | 100.7 KiB |
| Epic／史诗 | 靛蓝能量箱、侧面保护框、发光护轨 | 1,540 | 116.5 KiB |
| Legendary／传说 | 橙色核心保险箱、八角锁芯、盾形顶冠 | 1,508 | 113.9 KiB |
| 五档合计 | 10 个独立网格 | **6,392** | **487.0 KiB** |

每次生成通过原有掉落束表直接确定外形，掉落权重与内容继续由原系统决定。单箱只保留 Body、Lid 两个 Renderer，所有档位共用 **1 个材质、3 张 32×2 色板贴图**，没有为每个实例复制材质。开箱使用属性块降低光条亮度。

箱盖绕后方铰链，在 **0.32 秒**内沿本地 X 轴打开 **-100°**。开箱同时关闭交互触发器和实心箱体碰撞器，避免重复开启或挡住散落物；保持原来的 **3 秒回收时间**。归还对象池时重置盖子、发光和碰撞，换稀有度复用时直接切换共享网格。对象池预热 **5 个**，当前场上宝箱上限仍为 **5 个**。

详细尺寸、铰链、色板和 Blender 重建步骤见 [宝箱资产说明](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/ChestNightUI/chest-assets.md>)。本轮使用确定性 Blender 建模，没有新增 Meshy 付费生成任务。

## 夜战氛围与可调参数

沿用现有 1 盏主光和 4 盏工业 Spot。主光改为冷蓝白、强度 **0.82**，保持三色环境补光；暖色 Spot 强度 **5.6**，冷色 **5.1**，工业灯继续使用无阴影设置。线性远雾在相机距离 **28–108 米**间渐入，使远端场地融入夜色。

`NightAtmosphere_Local_176ParticleCap` 跟随玩家水平位置，粒子采用世界空间模拟。风尘默认 **10 个/秒、上限 120 个**；细雨 **6 个/秒、上限 56 个**。合计硬上限 **176 个**，覆盖玩家附近 **26×4.8×26 米**空间。风速、颜色、透明度、覆盖范围和发射量可以在 `NightAtmosphere` Inspector 中调整；发射总量硬限制为 18 个/秒。

两套粒子共用 1 张 **64×64 RGBA32** 径向贴图和 1 个透明 Unlit 材质。Shader 只采样一次贴图，包含相机近距离与地面高度淡出，不采样屏幕深度。没有粒子碰撞、粒子灯、拖尾或新增实时光源。天气随游戏时间暂停；升级面板自身动画使用未缩放时间，暂停选择期间仍可交互。

如需长期保留新的场景亮度，应同步调整 [NightAtmosphereBuilder.cs](<E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor/Assets/BackpackSurvivor/Editor/NightAtmosphereBuilder.cs>) 的 `ApplyNightLighting()`；直接修改 Inspector 适合即时调试，下次构建会恢复 Builder 的参数。更多说明见 [夜战模块说明](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/ChestNightUI/night-atmosphere.md>)。

## 升级三选一界面

新版采用简洁的蓝灰卡片、标题与等级，以及每个升级选项独有的装备插画。5 类、13 项插画共用一张 1024² RGBA 图集，PNG 约 778.5 KiB，Windows BC7 无 Mipmap 的纹理载荷为 1 MiB；复用现有思源黑体 SDF 与 UGUI 材质。

- 初次弹出清空 EventSystem 选中对象，不默认聚焦或高亮第一张卡片。
- 鼠标悬停显示高亮，点击整张卡片即可选择；数字键 1／2／3 可直接选择。
- 左右方向键移动焦点后，回车／空格确认；**没有焦点时不会提交旧项或自动选择第一项**。Tab 已移出升级导航。
- 面板按可用窗口空间缩放；只有 1 或 2 项时居中排列，0 项时恢复战斗，避免卡在暂停状态。
- 仍通过原 `GameSession.ChooseLevelUpOption()` 应用数值，一次提交只生效一次；关闭后继续监听下一次升级事件。

基础布局更新为 1360×848，单卡 400×550，间距 426。升级时淡出底层 HUD，关闭后恢复原透明度与交互设置，保持 Canvas 和控制器活动。原升级数值、概率与等级规则保留。当前实现与新版测试范围详见 [升级 UI 说明](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/README.md>)。

## 打开与重建

直接打开 [完整场景](<E:/YouXiKaiFa/Backpack Survivor/BackpackSurvivor/Assets/BackpackSurvivor/Scenes/Run/01-Run_ArtFull.unity>) 后 Play 即可体验。非 Play 模式下，可执行菜单：

`Tools / Backpack Survivor / Art / Apply Chests Night and Upgrade UI to Full Map`

该入口调用 `BackpackSurvivor.EditorTools.NightContentBuilder.ApplyAndSave()`，保留未保存场景快照后，重建本轮内容并保存 FullMap。重复执行不会叠加宝箱系统、天气或升级面板。原有 `Build Full 120m Recovery Yard` 总构建入口也已接入这三项内容。

模块入口分别为 `RecoveryChestBuilder.ApplyToActiveScene()`、`NightAtmosphereBuilder.ApplyToActiveScene()`、`NightUpgradeUIBuilder.ApplyToActiveScene()`；它们只处理当前场景，由调用方统一保存。

## 资源与验证记录

| 项目 | 记录 |
|---|---|
| 五档宝箱模型文件 | 498,720 bytes，约 487.0 KiB |
| 宝箱共享资源内存估算 | 网格 982,160 bytes；材质 3,188 bytes；纹理 15,649 bytes，合计约 0.95 MiB |
| 单箱渲染结构 | 2 个 Renderer、1 个共享材质 |
| 天气硬预算 | 176 粒子、16 个/秒；最大 352 三角形 |
| 天气贴图像素数据 | 64×64 RGBA32、无 Mipmap，16 KiB |
| 新增实时灯 | 0；地图仍为 1 盏主光加 4 盏 Spot |
| 新增 UI 图片／材质 | 新版 1 张 1024² 图集、13 个 Sprite；不新增独立 UI 材质，复用原字体 |

内存数字来自 Unity `Profiler.GetRuntimeMemorySizeLong` 对唯一引用资产的估算；不等同于显存、场景总内存或最终安装包大小。模型文件与导入后网格内存是不同指标。

以下是宝箱、夜战与最新 UI 的真实运行记录；新版插画 UI 的验收以 [UpgradeUI 文档](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/README.md>) 为准：

| 检查 | 结果与证据 |
|---|---|
| 场景、资源与 Shader | 半径 60 米；1 个活动升级 View；5 盏活动灯；引用 Shader 均支持，错误／警告均为 0。[内容审计](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/ChestNightUI/unity-content-audit.json>) |
| 五档宝箱真实对象池流程 | 五档全部通过；首开掉落、重复开启不重复掉落、归还关闭、复用碰撞与网格恢复。[对象池验证](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/ChestNightUI/chest-pool-validation.json>) |
| 真实开盖动画 | 传说箱达到完整开盖进度，角度约 100°，实心碰撞关闭。[开盖验证](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/ChestNightUI/chest-opening-validation.json>) |
| 简化插画 UI：实际经验升级与按钮选择 | 经验触发 GameSession 事件后调用真实按钮；经验倍率 1→1.2，仅应用一次，恢复 Running／timeScale=1；选择期间天气冻结。[升级运行验证](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/ChestNightUI/upgrade-runtime-check.json>) |
| 简化插画 UI：可选项数量边界 | 0、1、2、3 项全部通过。[选项数量验证](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/ChestNightUI/upgrade-offer-count-check.json>) |

上述记录覆盖组件配置、短程运行和交互回归，没有据此推断整局帧率或 15 分钟压力测试结论。审计 JSON 可能记录在 Play 中暂停选择的快照，`isPlaying`、当前粒子数或窗口打开状态只描述采样当时。
