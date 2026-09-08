# V0.4 背包美术接入

状态：已接入最终 v0.4 美术重构场景，并完成本轮 Unity Editor 编译与交互验证。本文保留实现说明与历史验收边界；一次性运行审计脚本已在仓库整理阶段移除。

## 入口与资产

`BackpackSurvivor.EditorTools.V04InventoryArtBuilder.ApplyToActiveScene()`，菜单 `Tools / Backpack Survivor / Art / Apply V04 Inventory`。也提供 `ApplyToActiveScene(string frameSpritePath)`。

- 默认外框：`Assets/BackpackSurvivor/Art/UI/V04/InventoryFrame.png`。必须是 Sprite，四边 `spriteBorder` 对应透明开口；四边均须大于零。Builder 先验证外框与已有绑定，再改场景。外框的导入、透明处理与压缩由总集成负责。
- 生成变体：`Assets/BackpackSurvivor/Prefabs/UI/V04/ItemView_V04.prefab`。通过连接原 `Prefabs/UI/ItemView.prefab` 的实例保存，生成后明确验证 `PrefabAssetType.Variant`。重新运行可更新同一路径。
- 复用 `SourceHanSansCN-Normal SDF.asset`、原物品图标、原星级 Sprite 与 `UpgradeRoundedGraphic` 的默认共享 UGUI 材质。不生成字体、图标、贴图或材质副本。
- Builder 只标记当前场景 dirty，不保存场景；总集成负责保存。构建必须在非 Play 模式。

## 对齐与交互边界

BagPanel 的位置、锚点、pivot 与 420×560 真实放置/丢弃区域保持不动。原 CanvasScaler 不改。

| 视觉层 | 新布局 |
|---|---|
| ItemLayer | 相对 BagPanel 左上角，pivot 左上，位置 (0,0)，420×560；控制器原有 `(x*70,-y*70)` 保留 |
| CellLayer | 相对同一左上角，位置 (2.5,-2.5)，415×555；6列8行，65×65，间距5 |
| 物品色边 | 原逻辑物品矩形内缩2.5；1格物品正好对齐65像素的格子 |
| 外框 | 九宫格边界换算为 UI 单位，外框宽高为420/560加左右/上下 border；中心开口严格420×560 |

标题、价值、操作提示与外框均在 BagPanel 的既有 CanvasGroup 内。新装饰根命名 `V04InventoryDecoration`，不进入会被 Redraw 清空的 ItemLayer。保留原 TotalValueText 对象和控制器引用。旧 BagFrame 停用并清空 Sprite；旧格子 Image 换成同对象上的轻量圆角 Graphic，原48格子与 GridLayoutGroup 保留。

装饰与 Tooltip 不参与 raycast。物品新色边 Graphic 参与 raycast，指针事件沿父链交给原 ItemView，不另写拖拽处理。

## 变体与展示组件

`V04InventoryItemArt`：读取原 ItemView 的 Item.Rarity 和背景 Image 已有颜色状态。原 Image 保留绑定但停止绘制；原 Bind / SetValidColor 继续写它。普通品质为灰白边，不凡绿、稀有蓝、史诗紫、传说红；放置合法/非法时给出柔和绿/红反馈。组件只在颜色或稀有度变化时刷新 Graphic，没有物品/库存写操作。

原星级、武器激活、邻接条、物品图标和字段引用均保留；没有改 ItemView 核心算法、InventoryUIController 或 Inventory 数据。

`V04InventoryTooltipClamp`：仅在显示时把原 Tooltip 限定于屏幕12像素边距内；继续使用既有跟随指针逻辑、属性文字与武器数值解析。Tooltip 换成346×254圆角面板和共享中文字体。

## 涉及文件

- 新增 `Assets/BackpackSurvivor/Editor/V04InventoryArtBuilder.cs`
- 新增 `Assets/BackpackSurvivor/Scripts/Presentation/Inventory/V04InventoryItemArt.cs`
- 新增 `Assets/BackpackSurvivor/Scripts/Presentation/Inventory/V04InventoryTooltipClamp.cs`
- 构建时生成 `Assets/BackpackSurvivor/Prefabs/UI/V04/ItemView_V04.prefab` 变体和对应 Unity meta。

## 运行验证建议

1. 外框导入并完成 Unity 编译后执行入口两次，确认第二次无重复节点、仍是 Prefab Variant、旧控制器绑定仍完整。
2. 1600×900 与1024×768打开背包，确认6×8格子、外框开口、1×1 / 多格物品边缘相合，标题/价值/操作说明可读。
3. 拖至四角格子、旋转2×1物品、合法/非法位置反馈、同类合并、邻接条与武器激活标记；对比原行为。中心网格之外松手仍按原代码丢弃，装饰外框不扩展放置区域。
4. 鼠标经过物品时标题/属性正确，右下与右上屏边 Tooltip 不出屏；打开升级/暂停时由总集成管理 CanvasGroup 隐藏与恢复。
5. 关闭/重开背包、触发 Redraw 后仍保留装饰；确认原图标、原星级与灰/金邻接反馈正确。

静态检查已核对48格、6列、70节距和420×560数学关系；检查了 UGUI 每对象仅一个 Graphic 的限制，格子旧 Image 在新增圆角 Graphic 前移除。Unity 编译与交互结果已汇总到本目录的 v0.4 实施记录与审计 JSON。

### 历史运行审计

本轮曾使用一次性 `Editor/V04InventoryAudit.cs` 做运行审计，仓库整理阶段已移除该脚本，仅保留结果记录：

- 对齐审计：Play 中 Start 完成且至少经过一帧后检查真实48格、真实物品视图、真实九宫格开口、装饰与物品的 raycast 路由、四角屏幕坐标映射；只读。
- 控制器交互审计：没有用户拖拽时，经原网格 Place 建立两件0价值/0效果的临时芯片，经原控制器 BeginDrag / Dragging / EndDrag 和原 HandleRotate 测试放置、四次旋转归位与合并。finally 分别移除临时物品，恢复测试拖拽引用，调用原 Redraw 并逐格检查原物品引用未改变。

历史方法返回结构化 Report，并追加写入 `Docs/ArtDirection/V0.4/inventory-ui-audit.json`。交互审计使用原控制器正常的延迟 Destroy，调用后至少经过一帧再作截图/对齐检查。世界越界丢弃、邻接与实际键盘输入由总集成单独实测，不由静态说明替代。
