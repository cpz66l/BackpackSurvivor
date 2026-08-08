# 《背包幸存者》开发推进计划（Orchestrator 执行蓝图）

> 依据：《背包幸存者》游戏设计与实施方案.md 第三部分实施计划
> 角色定位：以游戏大厂导师身份带教，边做边学，一步步推进。

## 现状盘点（2026-07-20 侦察结论）

- Unity 6000.3.20f1 + URP 17.3.0 + Input System 1.19.0，工程已建 Git。
- 已实现：玩家 WASD 移动（CharacterController）、鼠标地面投影朝向、Cinemachine 跟随相机、01-Run 原型场景。
- 代码仅 2 个文件：PlayerController.cs / InputReader.cs（GBK 编码、无命名空间）。
- Core/Data/Inventory/Presentation/Tests、GamePlay 子目录全部为空。
- 对照文档第 25 节"第一批具体任务"：任务 1-5 基本完成，任务 6-10 未开始。

## 阶段划分（对齐文档 V0.1 → V0.3 与四阶段排期）

## 2026-07-28 大局观重排：8月15 Demo 冲刺版

目标从“系统深挖”切换为“15 分钟完整可玩 Demo + 简历项目材料”。当前距离 2026-08-15 约 19 个自然日，课程必须先铺开完整体验面，再回头补构筑深度。

### 8月15 Demo Definition of Done（必须达成）

- 能从主场景开始一局，连续玩 15 分钟，有明确胜利/失败/结算/重开。
- 玩家移动、自动武器、主动射击、危险区、敌人追击、掉落、拾取、背包管理全部串起来。
- 背包里的武器会在玩家身边生成对应自动武器，并能随背包变化生成/回收。
- 装备能掉落、拾取、放入背包、旋转、丢弃、合并升级；至少 1 条邻接效果能被看见，最好有 1 条能影响战斗。
- 经验/升级三选一、波次递增、精英/宝箱/简单 Boss 或 15 分钟终局压力至少有一个完整节奏设计。
- 有基础 HUD：血量、时间、经验/等级、金币、当前武器/背包提示、结算分数。
- 能导出可演示 Build，并准备 README、录屏、截图、项目亮点说明，用于简历和实习投递。

### 优先级裁剪

- P0：完整单局闭环、武器激活、成长、波次、结算、Build。
- P1：DualWield 真实兑现、1~2 个基础芯片、简单 Boss/精英节奏、金币掉落/HUD、背包价值结算、合并升级收益、UI/音效反馈。
- P2：物品/邻接配置数据化、旋转接口方向、Gold 商店、附近战利品面板、世界空间提示、真实冷却遮罩。
- P3：网络编程、复杂动画系统、米哈游风格 RPG 动作项目；这些作为下一个项目或 Demo 后专题，不挤占 8月15 目标。

### 日期里程碑

- 7/28~7/31：补齐“背包构筑能影响战斗”的最短路径：第14课提交、武器激活、基础 HUD、经验成长、波次雏形。
- 8/01~8/04：插入高性价比体验课：战斗反馈快包、胜负结算、DualWield 最小兑现、内容面铺开。
- 8/03~8/06：补齐 Demo 经济与构筑反馈：金币掉落/HUD、物品价值显示、背包总价值、合并升级收益、数值调参台、怪物血量随波次成长。
- 8/07~8/10：利用超前进度补高性价比深度：武器稀有度/等级差异、攻击芯片效果、物品图标与背包可读性、主菜单场景。
- 8/11~8/12：做 15 分钟 Demo 的可读性和稳定性：主菜单入口、场景氛围、完整通关验收、Profiler 快扫。
- 8/13~8/15：Build、README、录屏脚本、简历项目描述与功能冻结缓冲；只修阻断 bug、调数值、补反馈，不再开新系统。

### 高性价比插课原则

- 优先做“玩家一眼能感到变化”的课：命中反馈、音效、敌人差异、金币/价值、升级选择、目标提示。
- 优先做“降低后续调试成本”的课：数值调参台、完整通关验收、Profiler 快扫。
- 暂缓做“工程很漂亮但 Demo 不一定更好玩”的课：完整配置数据化、复杂邻接互斥、Gold 商店完整循环、网络、复杂动画。金币掉落与 HUD 不是商店系统，已提升到 Demo 冲刺期。

### 阶段 1：V0.1 战斗核心原型（✅ 已于 2026-07-22 交付，超前完成）

目标：30 秒内能体验"移动 → 自动攻击 → 主动射击 → 区域压力"的核心闭环。

- 第 1 课 伤害管线：IDamageable / Health / 阵营区分（所有战斗共用）✅
- 第 2 课 敌人基础：追击 AI、近战攻击、受击、死亡回收 ✅
- 第 3 课 自动索敌：TargetRegistry 目标注册表 + 最近目标查询 ✅
- 第 4 课 双武器：WeaponBase 基类 + 自动武器 + 主动武器（左键射击），共用投射物 ✅
- 第 5 课 刷怪器 + 对象池：环形刷怪、场上上限、敌人/子弹双池化 ✅
- 区域危险：HazardZone 持续掉血（阵营可配/中立通吃）✅
- 工程 hygiene：.editorconfig 统一 UTF-8 BOM（命名空间/asmdef 遗留至第 6 课）
- 复盘文档：Docs/V0.1阶段复盘.md

### 阶段 2：搜刮与构筑（V0.2）——当前阶段
- 第 6 课 工程 hygiene 收尾（PlayerController/InputReader 历史债 + asmdef 评估）
- 第 7 课 掉落系统：权重随机 + 保底计数（GDD 11.4/11.5）✅
- 第 8 课 拾取系统：磁吸范围 + 自动拾取 ✅
- 第 9 课 背包纯数据网格：二维数组占格/放置/移除（纯 C#，可单元测试）✅
- 第 10 课 背包 UI：拖拽 + 放置预览 + 冲突提示（UI 只是数据投影）✅
- 第 11 课 掉落分层 + 拾取分化（对齐搜打撤设想与 GDD 191/201/718 行）：经验球（复用 PickUpMagnet 磁吸）+ 敌人分级掉落表（普通=经验球为主/精英=蓝装地板/Boss 专属表）+ 两级束表（先掷品类再掷子表）+ 保底修正（装备掷骰才计保底）+ IInteractable 交互系统（E 键：地面装备摘磁吸改为近身提示+按键拾取，收货口 OnCollected 不变）✅
- 第 12 课 容器搜刮：宝箱/隐藏宝箱（必出装备，隐藏箱绿装地板，GDD 11.5/11.6；宝箱实现 IInteractable 复用 E 交互）✅（含追加：宝箱池化+残骸计时回收、ChestSpawner 击杀触发+等级掷骰+拒绝采样、飞出散落协程、MapBounds 40m 圆形竞技场+玩家 clamp）
- 第 13 课 背包交互补丁课 ✅（2026-07-26 用户提出插入并定序）：① 提示框全屏透明面板吃射线（BUG-006，CanvasGroup blocksRaycasts 系统化解法）② 丢弃功能（拖出面板松手=世界丢弃；LootManager 提炼 SpawnEntry 公共生成口；Item→LootEntry 还原+往返保真；散落协程复用）③ R 键旋转（Item Rotated 标志位+TryFindFreeArea 内置双朝向诚实契约；BUG-008 OnEnable 早于 Start）④ 收货口请求-确认（IInteractable.Interact 改 bool+CanAccept 侦察兵+满包 DiscardToWorld 兜底，吞物品治本；BUG-007 闪字三连环）⑤ 轻量优化模块：MapBounds 迁 Scripts/Core/、Faction.neutral→Neutral 顺手清债
- 第 14 课 合并升级 + 第一条邻接联动（前置清账已随第 13 课全部结清）✅：① 同名同级 2→1 合并升级（纯数据层 + UI 拖拽触发 + 等级显示）② 邻接系统第一版只做“检测 + 表现”：ItemTag / ConnectableSides / 方向受限规则表，双手枪左右相邻 → DualWield 标记；背包 UI 显示接口点（灰=可连接，金=已触发），不急着接真实战斗加成
- 第 15 课（7/28~7/29）Demo 收口闸 + 背包武器激活 ✅：先提交第14课；清掉调试残留和临时注释；建立“背包武器物品 → 玩家身边自动武器实体”的生成/回收关系；默认自动武器激活上限=1；背包内显示激活标记；接口点/激活角标要随 ItemView 尺寸自适应贴边；真实冷却遮罩后移
- 第 16 课（7/29）单局框架与基础 HUD ✅：RunTimer / GameSession / GameState；15 分钟计时、暂停/继续边界、死亡失败入口、时间到胜利入口；HUD 显示血量、时间、经验、等级、状态；本课目标“一局有开始和正在进行”已完成
- 第 17 课（7/30）经验成长与三选一 ✅：经验条、升级、暂停战斗弹出三选一；新增 LevelProgress / LevelUpOption / LevelUpOptionGenerator / PlayerRunStats / LevelUpChoiceView；升级效果统一落到 PlayerRunStats，消费侧只读倍率
- 第 18 课（7/30）波次导演与 15 分钟节奏雏形 ✅：新增 WaveDirector，按时间推进刷怪强度；EnemySpawner 变成可被调度的执行器；HUD 显示波次名与阶段颜色；本课目标“0 到 15 分钟压力递增”已完成
- 第 19 课（8/01）战斗反馈快包 ✅：命中闪白/受击闪红、池化伤害数字、开火/命中/拾取/升级/受伤/开箱音效、Cinemachine 轻量相机震动；本课目标“同样的系统立刻更像游戏”已完成
- 第 20 课（8/02）胜负结算与重开闭环 ✅：RunResult 结算快照、Result 页、击杀/等级/经验/时间统计、重开按钮、退出按钮、环形经验 HUD；本课目标“完整跑一局并回到下一局”已完成
- 第 21 课（8/03）构筑最小兑现 ✅：新增 AdjacencyRuleBook / AdjacencyEffectResolver，UI 与战斗共用 validEffects；DualWield 每把武器最多参与 1 组，左右相邻双手枪可突破默认 activeWeaponLimit=1 激活第二把自动手枪；三把手枪横排最多 1 组双持，UI 只金色显示真实生效接口
- 第 22 课（8/04）内容面铺开 ✅：LootEntry 从源头承载 itemTag / connectableSides / scoreValue / effectValue；普通到传说长期物品池、普通/优秀/稀有/史诗宝箱表已定；手枪/步枪/霰弹枪三类自动武器接入；FireRateBoost 作为第一个可堆叠芯片收益跑通；中文字体替换为 SourceHanSansCN SDF
- 第 23 课（8/05 计划，8/02 提前完成）精英/宝箱/终局压力强化 ✅：普通/精英敌人分池生成，WaveDirector 下发 eliteSpawnChance；普通怪回归经验为主，精英怪承担更高价值掉落；GLB 新模型受击闪白改为临时材质替换；宝箱节奏和品质权重接入 15 分钟波次，终局阶段形成高压精英潮与高品质宝箱期待；文档 Docs/第23课-精英宝箱与终局压力强化.md
- 第 24 课（8/03）金币掉落与局内经济 HUD ✅：补齐 DropCategory.Gold 分支；新增 GoldOrb + goldOrbPool；金币掉落物复用磁吸收集节奏和散落飞出协程；GameSession 统计本局金币并广播 OnGoldChanged；RunHudView 显示金币；结算页金币显示按本课取舍后移，不做商店。
- 第 25 课（8/04）背包价值与物品价值显示 ✅：LootEntry.scoreValue → Item.ScoreValue 链路打通；ItemView 显示单件价值；InventoryGrid.GetTotalScoreValue() 按唯一物品累加；InventoryUIController 显示背包总价值；RunResult/ResultView 接入背包携带价值快照；金币仍保持独立资源，暂不混入背包价值。
- 第 26 课（8/05）合并升级收益兑现 ✅：同名同级 2→1 不只改等级显示，ScoreValue / EffectValue 随等级成长；DiscardToWorld 改用 BaseScoreValue / BaseEffectValue 防止 Lv.2 当前值污染 Lv.1 基础值；FireRateBoost 读取升级后的 EffectValue，攻速上限改为可调；新增 ItemTooltipView，格子轻量展示、悬停面板显示价值/效果/稀有度；伤害数字向上取整误导已修复并归档 BUG-015；文档 Docs/第26课-合并升级收益兑现.md。
- 第 27 课（8/04，原 8/06 计划提前完成）数值调参台与首轮平衡 ✅：FireRateBoost 基础值回调为 10%/15%/20%，等级效果倍率改为 Lv.1=1.0x、Lv.2=1.5x、Lv.3=2.0x，攻速上限回调至 2.0x；伤害在 WeaponBase 源头 RoundToInt，DamageNumberView 同步显示真实伤害；WaveDirector/EnemySpawner 接入普通/精英直接血量成长；新增宝箱距离 HUD，显示最近未开宝箱距离但不做箭头；首轮 15 分钟试玩验证通过，运气不佳时剩 3 分钟失败，终局压力、背包整理和宝箱路线选择成立；文档 Docs/第27课-数值调参台与首轮平衡.md。
- 第 28 课（8/04，原 8/07 计划提前完成）旋转邻接方向修正 ✅：Item 从 bool Rotated 升级为四状态 RotationState，支持 0/90/180/270 度；GetWorldSides 统一本地方向→世界方向转换；ScanAdjacency 保持只扫右/下，但 TryMatchNeighbor 改为 forwardMatched/reverseMatched，确保 rule.TagA+SideA 与 rule.TagB+SideB 成对绑定；拖拽中旋转即时刷新 ghost 接口点；丢弃路径改用 BaseWidth/BaseHeight+LocalConnectableSides 恢复原始朝向；BUG-017 已归档；文档 Docs/第28课-旋转邻接方向修正.md。
- 第 29 课（8/05，原 8/08 计划提前完成）武器稀有度与等级差异 ✅：新增 WeaponItemStatResolver，把 Item.Rarity / Item.Level 解析为背包武器伤害倍率；WeaponBase 形成“武器基础伤害 × 玩家升级乘区 × 背包武器乘区”；BackpackWeaponActivator 激活时按具体 Item 实例注入倍率，DualWield 第二把武器也吃自己的品质/等级；TryMerge 成功升级后补发 OnChanged，确保伤害即时刷新；Tooltip 区分武器伤害提升、芯片效果和收集品价值；BUG-018/019 已归档；文档 Docs/第29课-武器稀有度与等级差异.md。
- 第 30 课（8/05，原 8/09 计划提前完成）攻击芯片效果实装 ✅：新增 DamageBoost 邻接效果，AttackDamageChip 通过 AdjacencyRuleBook / InventoryGrid.ScanAdjacency / AdjacencyEffectResolver 进入真实有效效果；WeaponBase 新增 backpackDamageBoostMultiplier，最终伤害形成“基础伤害 × 玩家升级 × 武器品质/等级 × 攻击芯片”四乘区；BackpackWeaponActivator 按真实激活武器累加 DamageBoost 并用 maxBackpackDamageBoostMultiplier 封顶，刷新时重置攻速/武器/伤害芯片三类倍率；Tooltip 区分攻速芯片与伤害芯片；文档 Docs/第30课-攻击芯片效果实装.md。
- 第 31 课（8/06，原 8/10 计划提前完成）物品图标与背包可读性 ✅：新增 ItemIconResolver，按 ItemTag 映射透明 PNG 图标；ItemView 接入图标显示、等级星星、透明稀有度底色；邻接接口从点升级为接边，灰边=可连接、金边=已生效；图标从正方形适配升级为矩形适配，改善步枪/霰弹枪/防具等 3x2 物品观感；文档 Docs/第31课-物品图标与背包可读性.md。
- 第 32 课（8/07）主菜单与场景流 ✅：新增 MainMenu 场景和 MainMenuController；主菜单支持开始游戏、退出游戏、制作者声明弹窗；Build Settings 修正为 MainMenu→01-Run；ResultView 退出改为返回主菜单，重开仍重载 Run；CanvasScaler 改为 1920x1080 Scale With Screen Size，修复分辨率变化 UI 偏移；BUG-020/021 已归档；文档 Docs/第32课-主菜单与场景流.md。
- 第 33 课（8/08）场景氛围与演示包装 ✅：地面贴图、边界提醒、宝箱模型/稀有度辨识、装备掉落散落协程、Tab 背包开关、背包 UI 美术、物品等级丢弃往返保真、重开静态状态重置、URP Lit 地面材质与阴影链路；危险区表现暂缓至 Demo 后 P2；文档 Docs/第33课-场景氛围与演示包装.md。
- 第 34 课（8/08）完整 15 分钟通关验收 ✅：外部试玩暴露邻接/武器激活理解门槛，MainMenu 新增玩法说明面板；精英敌人经验奖励上调；修正主动射击鼠标地面点与枪口高度弹道不一致（BUG-024）；试玩出现约 4000￥高价值背包但终局整理被偷袭失败，证明风险收益张力成立；全游戏链路巡检通过，文档 Docs/第34课-完整15分钟通关验收.md。
- 第 35 课（8/08，原 8/10~8/11 计划提前完成）Profiler 快扫与低风险优化 ✅：整理 Profiler 截图证据包，区分 EditorLoop/Live Display 观察开销、Loading/Texture Upload 资源尖刺和真实 PlayerLoop 热点；Build 实测后期波次无明显卡顿；修复 Build 中子弹/装备掉落物颜色异常（显式 URP Unlit 材质 + MaterialPropertyBlock）；原始 ProfilerCaptures 加入 .gitignore；文档 Docs/第35课-Profiler快扫与低风险优化.md。
- 第 36 课（8/12~8/13）Build 与演示包 ◀ 当前：Windows Build、分辨率/窗口模式、输入检查、干净启动场景；准备可交给别人直接运行的版本。
- 第 37 课（8/13~8/15）作品材料：README、录屏脚本、截图清单、简历项目描述、技术亮点提炼；把“我做了什么、难点在哪里、怎么验证”讲清楚。
- 第22课后内容系统性价比队列：P0 已完成 = 数据源保真、掉落入口可跑、三类自动武器基础差异、FireRateBoost 芯片真实收益、精英/宝箱节奏、史诗宝箱阶段展示、金币掉落/HUD、单件价值显示、背包价值结算、合并升级收益、物品详情 Tooltip、弹夹超模回调、怪物血量随波次成长、伤害显示统一、宝箱距离提示、首轮 15 分钟可玩验证、旋转邻接方向修正、武器稀有度/等级差异、攻击芯片效果、物品图标与背包可读性、主菜单场景、场景氛围包装、完整通关验收、玩法说明入口、主动瞄准一致性修正、Profiler 快扫与 Build 颜色异常修复；P1 继续 = Build 与作品材料；P2 Demo 后 = 新手目标提示、金币结算页取舍、危险区表现强化、完整物品配置 SO、Gold 商店、出售/撤离经济、全套词缀和复杂互斥规则
- 可选增强挂账：附近战利品面板（塔科夫式 loot panel，背包打开时拖拽地面物品入包，纯 UI 层拖拽复用现有三态）
- 邻接系统挂账优先级（第 30 课后重排）：P2 结算页/局内提示展示已生效芯片收益 → P2 CritBoost/BurningBullets 等第二类芯片 → P2 Resolver 层数上限策略数据化 → P2 物品/规则配置数据化 → P2 地面掉落保留运行时旋转态
- 第 11 课挂账余量：①②已随第 13 课结清；③ Gold 频道落地已提升为第 24 课；剩余 ④ 提示框世界空间化
原则：背包先写纯数据二维网格 + 单元测试，UI 只是投影；邻接不默认四边万能，必须由标签、接触方向和规则表共同决定；表现层必须让玩家看见可连接方向。
边界：主动撤离是 GDD 明确的 MVP 暂缓项（文档 966 行）；MVP 的"撤"= 积分结算，不提前做。

### 阶段 3：Demo 冻结与简历材料（V0.2 收尾，8/13~8/15）
功能冻结 → 阻断 bug 修复 → 录屏 → README → 简历项目描述。
原则：8/13 之后不再加新系统，只修 bug、调数值、补表现、做材料。

### 阶段 4：Demo 后扩展（V0.3 / 下一个项目前）
邻接系统完整版、物品配置数据化、Gold 商店、出售/撤离经济、附近战利品面板、世界空间提示、真实冷却遮罩、更多芯片、Boss 完整机制。
若转向米哈游风格 RPG 动作项目：动画系统、角色控制器手感、技能状态机、镜头、网络编程另开新项目专题，不把《背包幸存者》拖成无限大项目。

## 执行纪律（来自文档第 27 节）

- 优先顺序：核心手感 > 完整闭环 > 构筑深度 > 表现打磨 > 内容扩充
- 不服务"区域压力 + 背包构筑 + 双战斗系统"的功能一律进暂缓列表
- 每步产出必须可在 Unity 里实际运行验证
