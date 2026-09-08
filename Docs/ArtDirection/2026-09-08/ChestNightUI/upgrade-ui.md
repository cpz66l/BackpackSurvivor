# 升级 UI 文档入口

当前版本已实施用户批准的简化插画设计。完整说明已迁移到 [UpgradeUI/README.md](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/README.md>)，该文档统一维护实际资源预算、布局、交互、重建步骤与本版测试状态。

新版保留标题、等级、三张蓝灰卡片与一行操作提示；5 类、13 项升级各有独立插画，共用一张 1024² RGBA 图集。PNG 为 797,206 bytes，Windows BC7 无 Mipmap 的图集纹理载荷为 1 MiB，不包含字体或总显存。布局基准为 1360×848，单卡 400×550，间距 426。

初始没有默认高亮；支持鼠标、1／2／3、左右方向键和当前焦点的回车／空格确认。背景点击取消焦点后不会提交旧项；Tab 已移出升级导航。原数值、概率、等级与可选次数规则继续由原 GameSession 和生成器负责。

非 Play 模式可执行 `Tools / Backpack Survivor / Art / Apply Approved Illustrated Upgrade UI` 导入、重建并保存。底层接口仍为 `NightUpgradeUIBuilder.ApplyToActiveScene()`，重复构建不会叠加控制器。

新版已通过 13 项图文检查、真实经验与按钮选择、键盘输入及 HUD 恢复检查。运行 JSON 的最新结果已集中复制至 UpgradeUI 目录，详见 [新版文档](<E:/YouXiKaiFa/Backpack Survivor/Docs/ArtDirection/2026-09-08/UpgradeUI/README.md>)。
