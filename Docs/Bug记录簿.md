# 《背包幸存者》Bug 记录簿

> 用途：记录开发中真实发现并修复的 Bug，供复盘与面试使用。
> 维护规则：每次发现并修复一个值得记录的 Bug，按模板追加一条，编号递增。
> 面试话术建议：每条按「现象 → 排查思路 → 根因 → 修复 → 沉淀的规则」讲，重点讲**排查思路**，那是工程师和打字员的区别。

## 记录模板

```
### BUG-XXX · 标题
- 日期 / 所属系统：
- 现象：
- 复现步骤：
- 排查过程：
- 根因：
- 修复：
- 沉淀规则（一句话）：
```

---

## BUG-001 · 拖拽中自动入包，手持物品蒸发 + 幽灵卡屏

- **日期**：2026-07-25 · **所属系统**：背包 UI / 拾取（InventoryUIController + InventorySystem）
- **现象**：拖着物品 A 不松手时，磁吸把物品 B 吸入背包；松手后 A 从背包里消失，且 A 的拖拽残影（ghost）卡在屏幕上好一阵子
- **复现步骤**：① 背包有若干物品 ② 拖起 A（A 离包）③ 捡拾 B（自动入包）④ 松手
- **排查过程**：从"回滚必成功"的设计假设入手——回滚依赖"A 的旧格子仍然空着"，但 B 入包走 TryFindFreeArea，恰好会抢占 A 腾出的格子。推演 Place 调用链发现两级 Place（目标格 / 旧锚点）全部 return false，A 未落任何位置；失败的 Place 不发 OnChanged，Redraw 不触发，ghost 残留——两个症状同一病根
- **根因**：回滚路径只有一层兜底（旧锚点），且该兜底依赖的"格子仍空"不变量被自动入包破坏；失败后无事件、无重绘，造成数据蒸发 + 视图卡屏双症状
- **修复**：EndDrag 改三级回退链——目标格 → 旧锚点 → TryFindFreeArea 任意空位 → 全失败则恢复拖拽状态继续手持（isDragging 归位，引用不清空）；任一成功即触发 OnChanged 统一重绘。**任何分支下物品都不可能丢失**
- **沉淀规则**：回滚路径必须在数学上不可能失败；一层兜底不够就三层，最后一层永远是"不丢东西"

---

## 历史精选（补录）

### BUG-000A · 一只怪掉两个球（第 7 课）
- **现象**：击杀一只敌人掉落两个物品
- **根因**：Die() 里调了一次掉落，OnReturnPool() 里也写了掉落——而 pool.Return 会自动触发 OnReturnPool，副作用挂了两个事件源
- **修复**：掉落只挂 Die，且放在 pool.Return 之前
- **沉淀规则**：副作用只挂一个事件源；"死亡的结果"不要挂在"回池"上

### BUG-000B · Die() 里找不到 lootManager（CS0120，第 7 课）
- **现象**：编译错误，方法内查到的管理器引用在另一个方法里"不存在"
- **根因**：`LootManager lootManager = Find...()` 声明的是局部变量，方法结束即销毁
- **修复**：提升为类级字段，赋值时去掉类型前缀
- **沉淀规则**：有类型名 = 新建局部变量；无类型名 = 使用已有变量

### BUG-000C · 测试全绿但验证的东西是错的（第 9 课）
- **现象**：重叠放置测试如预期返回 false，实际放的却是同一个 item1（撞自己），而非计划的 item2 撞 item1
- **根因**：测试剧本笔误；结果"碰巧正确"形成假阳性
- **修复**：修正剧本并追问每条用例"绿的理由对不对"；顺带挖出"同一实例可占两块地"的真设计漏洞，给 Place 加 Contains 实例守卫
- **沉淀规则**：测试通过 ≠ 测试有效；小笔误背后常有大漏洞

### BUG-000D · 拖拽永远弹回原位（第 10 课）
- **现象**：拖拽跟手正常，但松手后物品永远回到原格
- **根因**：两层叠加——① ItemLayer pivot 在中心，ScreenPointToLocalPoint 的 localPos 原点在面板正中，格坐标换算整体偏半屏；② 修复时 `Input.mousePosition`（新输入系统下禁用，抛异常）只改了一半，异常中断导致 target 坐标永不更新
- **修复**：ItemLayer pivot 改 (0,1)；全部改用 PointerEventData.position
- **沉淀规则**：异常中断行之后的代码不执行；UI 数学先确认 pivot；改 bug 要查全所有调用点，改一半等于没改

### BUG-000E · 点击物品只有正中间有效（第 10 课）
- **现象**：大尺寸物品只有中央一小块能点中拖拽
- **根因**：根物体 Image 的 RaycastTarget 被关，只有子物体小字（84×27px）响应射线
- **修复**：根 Image 开 RaycastTarget，子 Text 关闭
- **沉淀规则**：纯展示文字的 Raycast Target 一律关闭；点击区域异常先查射线开关分布

### BUG-002 · 敌人死亡不消失（第 11 课 · 池化系列 ②）
- **现象**：部分敌人死后不消失，继续追人；武器也不再攻击它
- **根因**：装备预制体摘掉了 PickUpMagnet 组件，但 `DropItem.OnGetFromPool()` 里 `pum.StateReset()` 调用残留 → 复活即 NRE → 异常中断 `Die()` 后续流程（回池语句没执行到）。同步掉落经验+装备的敌人必现
- **修复**：用户自己读 Debug 日志定位（导师此前"池槽拖错"的推测错误，认账）；清除 DropItem 里 pum 的字段/GetComponent/调用全部三处引用
- **沉淀规则**：组件摘除后必须全局搜引用（字段、获取、调用三处）；异常会静默中断当前方法的后续语句——"死了一半"的状态先查 NRE

### BUG-003 · 经验球范围外出生即吸（第 11 课 · 池化系列 ③）
- **现象**：有的经验球掉在磁吸范围外正常待命，有的一掉出来就隔空飞向玩家
- **根因**：`XpOrb.OnGetFromPool()` 空实现——被吸过的球带 Attracted 状态+满档速度回池，复活首帧直接进 MoveTowardsPlayer，绕过 Idle 分支的距离判断。"正常球"=池预热新球（默认值 Idle），"秒吸球"=复用球
- **修复**：`OnGetFromPool()` 加 `pum.StateReset()`（归零状态机+速度）
- **沉淀规则**：池化对象的"前世记忆"只信本次写入；区分"新对象 vs 复用对象"的行为差异是定位池化 bug 的第一刀

### BUG-004 · 经验球出生即死（第 11 课 · 池化系列 ④）
- **现象**：不捡经验球，一段时间后击杀敌人不再掉球；捡过的球却能正常复用
- **根因**：新增 `survivalTimer`（15s 自然清场）没进 `OnGetFromPool`——超时回池的球带 15s+ 计时复活，首帧即触发 Recycle，"毒球"出生即死亡；被捡的球回池时计时器读数小，暂时看不出（但逐轮累积，久了也会毒发）
- **修复**：`OnGetFromPool()` 加 `survivalTimer = 0f`
- **沉淀规则**：**给池化类新增运行期字段的同一分钟，必须去 OnGetFromPool 归零它**——不等 bug 出现，机械执行（池化状态归零第四次复发后的升级流程）

### BUG-005 · 掉落物双重回收（第 12 课 · 池化系列 ⑤）
- **现象**：Debug 出现"DropItem 已在池中忽略重复归还"警告
- **根因**：拾取成功后探测器 `CurrentTarget` 要等下一轮 0.1s 扫描才刷新，窗口内连按 E 会对同一具"尸体"二次 `Collect()` → 重复归还；且第二次 `Collect` 还重复广播 `OnCollected`（HandleCollected 每次 new 新 Item 实例，背包 Contains 守卫接不住）→ 双重入包
- **修复**：双端设防——生产者 `Collect()` 幂等化（`isCollected` 守卫+`OnGetFromPool` 归零，同一生命周期只收一次）；消费者 `Interact()` 成功后立即清空双目标并广播 null（顺手治好提示残留 0.1s）
- **沉淀规则**：池的防重警告是报警器不是噪音；**缓存的池化对象引用=会过期的支票**——持票人用完即弃，出票人让重复兑现无害（幂等）

### BUG-006 · 提示框吃全屏射线，背包拖不动（第 13 课 · 射线家族 ②）
- **现象**：背包物品几乎拖不动，只有特定区域能响应拖拽
- **根因**：PromptPanel 锚点全屏拉伸 + 透明 Image（alpha=0）+ RaycastTarget 开——看不见 ≠ 不存在，它铺满全屏吞掉了所有射线（BUG-000E"射线开关分布"问题家族复发）
- **修复**：PromptPanel 挂 CanvasGroup，`blocksRaycasts = false`——整棵子树射线一刀切，不靠逐个控件记忆
- **沉淀规则**：不响应点击的 UI，射线一律不通；批量管理用 CanvasGroup，不靠人肉逐个检查；透明 ≠ 无害

### BUG-007 · 背包已满闪字三连环（第 13 课）
- **现象**：没按 E 走路也自动闪"背包已满"；按 E 拾取后提示和闪字两条文字一起消失
- **根因**：三处镜像全错位——① 协程点亮 promptBagFull，熄灭的却是 promptPanel（对象错位），闪字从第一次触发后永远残留 active；② 残留 active 的子物体在 promptPanel 每次显示时"借爹复活"= 没按 E 自动闪；按 E 隐藏 promptPanel 时父子同灭 = 两条同消；③ OnDisable 退订写成 `+=`，每次禁用/启用循环叠加订阅（定时炸弹，本次未引爆）
- **修复**：镜像三行——协程关闪字本身（0.5s）；Start 显式双关（panel + bagFull 各自自洽）；OnDisable 改 `-=`
- **沉淀规则**：订阅/显隐/点亮熄灭，凡开关必镜像；每个 UI 元素的显隐状态各自自洽，不靠爹遮盖；审 UI bug 先问"谁点亮的它，谁负责熄灭它"

### BUG-008 · R 键旋转完全无效（第 13 课）
- **现象**：拖拽中按 R 毫无反应，表面上无任何报错
- **根因**：OnEnable 先于 Start 执行——订阅时 inputReader 还是 null（它在 Start 才赋值），NRE 悄悄中断，订阅从未发生；异常埋尸在 Console 最开头的启动期刷屏里
- **修复**：引用缓存挪 Awake（Awake 拿引用 → OnEnable 订阅 → 事件才接得上）
- **沉淀规则**：Awake 拿自己的引用 / OnEnable 做订阅 / Start 拿别人的引用；功能"完全无反应且无报错"，先翻 Console 最开头的启动期 NRE

---

## BUG-009 · 拖拽丢弃激活武器后，新顶上的武器 UI 标记不刷新（第 15 课）

- **日期**：2026-07-29 · **所属系统**：背包 UI / 背包武器激活（InventoryUIController + BackpackWeaponActivator）
- **现象**：背包中有多把武器且自动武器上限为 1 时，拖拽当前带激活角标的武器并丢出背包；玩家身边的新武器实体已经正确激活，但背包里新顶上的武器没有立刻出现激活角标，要等下一次 Redraw 才刷新。
- **复现步骤**：① 背包内放两把可激活武器 ② 确认左上优先武器显示激活角标 ③ 拖拽这把武器到背包外丢弃 ④ 观察玩家身边武器切换但背包角标暂时缺失。
- **排查过程**：先确认 `BackpackWeaponActivator` 的激活实体逻辑正确，再反查 UI 标记来源；发现 `BeginDrag()` 中 `grid.Remove(item)` 会触发 `OnChanged`，但 `Redraw()` 因 `isDragging` 直接返回，导致这次数据变化被有意拦截；面板外丢弃分支没有 `Place()`，因此后续也没有新的 `OnChanged` 来补画。
- **根因**：拖拽门闸只负责保护 ghost 不被全量重绘销毁，但没有记录“拖拽期间漏过一次重绘”；当拖拽结局不走 Place 时，UI 永远错过这次刷新。
- **修复**：新增 `needsRedrawAfterDrag`。`Redraw()` 在拖拽期间不画但置位；EndDrag 中面板外丢弃、合并、背包满丢弃等不会自然触发 Place 的分支，在清理 `dragItem/ghost` 后判断并补一次 `Redraw()`。
- **沉淀规则**：事件门闸必须成对设计“拦截记录 + 结束补偿”；只拦不补，会把数据变化吞进 UI 临时态里。

---

## BUG-010 · 伤害数字固定屏幕中心且没有正对摄像机（第 19 课）

- **日期**：2026-07-30 · **所属系统**：战斗反馈 / 世界空间 UI（DamageNumberView + DamageNumber prefab）
- **现象**：敌人受击后伤害数字会跳出，但最初位置一直在屏幕中间；改到世界附近后，又出现数字斜着、不正对屏幕的问题。
- **复现步骤**：① 攻击敌人触发 `Health.OnDamaged` ② `DamageNumberSpawner` 从池中生成伤害数字 ③ 观察数字显示位置和朝向。
- **排查过程**：先确认 `DamageInfo.hitPoint` 和生成位置不是固定值，再检查预制体 Canvas 设置；发现 Screen Space Canvas 不吃世界坐标语义。改为 World Space 后位置正确，但 World Space UI 不会自动朝向摄像机，于是继续补 Billboard。
- **根因**：两个坐标系问题叠加：① Canvas 使用 Screen Space，导致世界坐标生成被 UI 屏幕空间解释；② World Space Canvas 保留自身旋转，不会自动面向主摄像机。
- **修复**：DamageNumber 预制体改为 World Space Canvas，并调整缩放；`DamageNumberView` 在播放期间执行 `FaceCamera()`，让数字使用 `Camera.main.transform.rotation` 对齐摄像机。
- **沉淀规则**：世界空间 UI 的三件套是 Render Mode、Scale、Billboard；位置对了不代表朝向也对。

---

## BUG-011 · HUD 血条 Slider 被 A/D 移动键控制（第 20 课）

- **日期**：2026-07-31 · **所属系统**：HUD / UGUI 输入焦点（RunHudView + HpSlider + EventSystem）
- **现象**：玩家按 A/D 左右移动时，血条 Slider 的显示也跟着变化，像是移动键在操控血条。
- **复现步骤**：① Play 后让 HpSlider 可交互 ② 鼠标/键盘焦点落到 HpSlider 或 UI 导航选中它 ③ 按 A/D 或方向键 ④ 观察血条值被 UI 系统调整。
- **排查过程**：先排除 PlayerController 和 Health 逻辑，因为玩家移动不应该写血量；随后检查场景中的 EventSystem，发现使用 InputSystemUIInputModule，且 HpSlider 仍是可交互 Slider，Navigation 为 Automatic。由此确认是 UI Move 输入在控制 Selectable。
- **根因**：HpSlider 是 UGUI `Slider`，默认继承 `Selectable`，可被 EventSystem 选中并响应左右输入；但它在 HUD 中只是显示器，不应该接收玩家输入。
- **修复**：将 HpSlider 的 `Interactable` 关闭，Navigation 改为 None；纯显示用 Image/TMP 关闭 Raycast Target，避免 HUD 控件参与 UI 焦点和导航。
- **沉淀规则**：纯 HUD 控件必须退出交互系统；显示器不是输入控件，Slider/Button-like 组件默认会被 EventSystem 当成可操作对象。

---

## BUG-012 · TMP 中文显示方块（第 22 课）

- **日期**：2026-08-01 · **所属系统**：UI 字体 / TextMesh Pro（TMP Settings + TMP Font Asset）
- **现象**：局内中文文本显示为方块，包括胜利/失败、背包提示、按钮文本等中文内容存在显示风险。
- **复现步骤**：① 替换或删除原中文字体资产 ② 打开 `01-Run` 场景进入 Play ③ 观察 TMP 文本中的中文字符。
- **排查过程**：先确认文本内容本身没问题，再全局扫描旧字体 GUID、旧 TMP 材质 fileID 和场景/prefab 的 `m_fontAsset / m_sharedMaterial` 引用；继续检查 `TMP Settings.asset` 的默认字体和 fallback，最后检查新 SDF 是否绑定源 TTF、是否开启 Dynamic Atlas 与 Multi Atlas。
- **根因**：旧中文 SDF 字体资产无法稳定覆盖项目中文字形，且场景、prefab、TMP 默认设置中可能残留旧字体或旧材质引用；TMP 缺少 glyph 时就会显示方块。
- **修复**：替换为 `SourceHanSansCN-Normal.ttf` 与 `SourceHanSansCN-Normal SDF.asset`；统一场景、`DamageNumber.prefab`、`ItemView.prefab` 的字体和材质引用；`TMP Settings.asset` 默认字体与 fallback 指向新 SDF；新 SDF 开启 Dynamic、Multi Atlas，并关闭 Build 时清空动态数据。
- **沉淀规则**：中文显示方块要查整条字体资产链，不能只改单个 Text；字体、SDF、材质、TMP Settings 和 `.meta` 都是可复现工程的一部分。

---

### BUG-013 · GLB 新模型受击后不再闪白（第 23 课）

- **日期**：2026-08-02 · **所属系统**：战斗反馈 / 模型材质（DamageFlashView + Health.OnDamaged）
- **现象**：替换基础 GLB 敌人模型后，敌人受击仍然扣血、跳伤害数字，但原本的白光反馈消失；手动拖 Renderer 也没有看到明显效果。
- **复现步骤**：① 使用新导入的 NormalEnemy / EliteEnemy 模型 ② 玩家攻击敌人 ③ 观察伤害数字正常但敌人模型没有闪白。
- **排查过程**：先怀疑 Renderer 没抓到，于是确认 `GetComponentsInChildren<Renderer>(true)`；再通过日志验证 `DamageFlashView.OnEnable`、`Health.TakeDamage`、`HandleDamaged` 都已触发，说明事件链路没断。最后把问题定位到材质表现路径：旧方案改 `renderer.material.color`，但 GLB 模型材质/Shader 不一定吃这个颜色属性。
- **根因**：旧闪白方案依赖材质支持颜色属性，换成 GLB 新模型后，受击事件正常但材质颜色修改没有稳定可见效果。
- **修复**：`DamageFlashView` 改为缓存每个 Renderer 的 `sharedMaterials`，受击时临时替换为 `flashMaterial`，等待 `flashDuration` 后恢复；`OnDisable()` 兜底恢复材质，防止池化对象带着闪白材质回池。
- **沉淀规则（一句话）**：视觉反馈失效先分清“事件没来”还是“表现方式不生效”；跨模型/跨 Shader 的 Demo 反馈，临时材质替换比改颜色更稳。

---

### BUG-014 · 字段改名导致 GoldOrb 池引用丢失，敌人死亡掉落时报 NRE（第 24 课）

- **日期**：2026-08-02 · **所属系统**：掉落系统 / Unity 序列化 / 对象池（LootManager + GoldOrb Pool）
- **现象**：击杀敌人后出现空引用，表面定位在 `EnemyAI` 的 `lootManager.TrySpawnDrop(health.Position, lootTable)` 调用附近；金币掉落分支无法正常生成。
- **复现步骤**：① LootTable 中配置 Gold 掉落 ② Play 后击杀敌人 ③ 掉落流程进入 Gold 分支 ④ Console 报空引用。
- **排查过程**：先排除 `EnemyAI` 和 `lootManager` 本身为空，因为其他掉落分支能工作；继续钻进 `LootManager.SpawnEntry()`，发现空引用发生在 Gold 分支取 `goldOrbPool.Get(position)` 时。再检查场景 Inspector/YAML，发现字段曾从拼写错误的 `glodOrbPool` 改成 `goldOrbPool`，旧序列化引用没有自动迁移。
- **根因**：Unity 对 `[SerializeField] private` 字段按字段名序列化；字段改名后，场景中原来绑定到旧字段名的数据不会自动匹配到新字段，导致 `goldOrbPool` 变成 null。
- **修复**：在 `01-Run.unity` 中重新给 `LootManager.goldOrbPool` 接入 GoldOrb 对象池并保存场景；后续同类字段改名可使用 `[FormerlySerializedAs("旧字段名")]` 保护引用迁移。
- **沉淀规则（一句话）**：Unity 序列化字段改名后必须检查 Inspector 引用和 YAML 残留；NRE 的报错行是入口，不一定是真正为空的字段。

---

### BUG-015 · 伤害数字向上取整导致显示伤害高于真实扣血（第 26 课）

- **日期**：2026-08-03 · **所属系统**：战斗反馈 / 数值显示（DamageNumberView + Health）
- **现象**：子弹伤害数字显示 25，但打 50 血敌人需要 3 枪；当伤害调到 28 左右时才明显变成 2 枪击杀。
- **复现步骤**：① 设置敌人血量 50 ② 使用显示约 25 的子弹伤害攻击敌人 ③ 观察两枪后敌人未死，第三枪才死亡。
- **排查过程**：先确认 `Health.TakeDamage()` 是否按显示数字扣血，再检查伤害数字来源；发现扣血使用真实 `info.damage` 浮点值，而 `DamageNumberView.Play()` 显示时用了 `Mathf.CeilToInt(damage)`，例如真实 24.x 会显示为 25，造成玩家预期两枪击杀。
- **根因**：显示层向上取整，真实规则层仍按浮点伤害计算；UI 给出的承诺高于实际扣血。
- **修复**：将伤害数字显示从 `Mathf.CeilToInt(damage)` 改为不向上夸大显示；第27课继续统一评估“伤害源头整数化”或“UI 显示一位小数”的最终方案。
- **沉淀规则（一句话）**：战斗反馈不能 overpromise；显示值和真实规则必须语义一致，否则玩家会觉得系统不讲理。

---

### BUG-016 · Player 血量序列化为 0 导致敌人生成后不追击（第 27 课）

- **日期**：2026-08-04 · **所属系统**：敌人 AI / 生命值初始化 / 场景序列化（EnemyAI + Health + Player）
- **现象**：敌人能正常刷新出来，也能被玩家攻击并掉落物品，但刷新后不主动追击玩家，看起来像 EnemyAI 失效。
- **复现步骤**：① Play 进入 `01-Run` 场景 ② 等待敌人刷新 ③ 观察敌人停在原地不移动 ④ 玩家攻击敌人，扣血和掉落仍然正常。
- **排查过程**：先确认敌人生成、池化、受击、死亡掉落都正常，说明对象池和 Health 事件链路不是主因；再检查追击目标是否有效，发现 Player 的 `Health.maxHp` 在场景中被序列化成 0，开局 `currentHp = maxHp` 后玩家立即处于 `IsDead == true` 状态，敌人 AI 因目标死亡而不追击。
- **根因**：场景中 Player 的 `Health.maxHp` 被错误保存为 0；AI 逻辑本身没断，是目标状态非法。
- **修复**：在 Inspector 中把 Player 的最大血量恢复到正常值并保存场景；后续排查“敌人不动”时先查目标是否存在、是否存活、是否注册到目标系统。
- **沉淀规则（一句话）**：AI 不动不一定是 AI 坏了；先查目标有效性、死亡状态和注册链路，再查移动逻辑。

---

### BUG-017 · 旋转后接口 UI 正确但 ScanAdjacency 不跟随触发（第 28 课）

- **日期**：2026-08-04 · **所属系统**：背包邻接规则 / 旋转方向（Item + InventoryGrid + AdjacencyRule）
- **现象**：物品旋转后灰色接口点能跟着变化，但 `ScanAdjacency()` 的触发结果没有完全跟随 `RotationState`；某些旋转后理论可触发的布局不触发。
- **复现步骤**：① 配置 `Rifle.Down + Magazine.Up` 规则 ② Rifle 和 Magazine 同时顺时针旋转 90° ③ 将 Magazine 放在 Rifle 左边，使 Rifle 本地 Down 变世界 Left、Magazine 本地 Up 变世界 Right ④ 观察 UI 方向可见但 `FireRateBoost` 没按预期触发。
- **排查过程**：先确认 `Item.GetWorldConnectableSides()` 已经按 `RotationState` 正确换算，说明接口显示层不是主因；继续检查 `InventoryGrid.TryMatchNeighbor()`，发现旧逻辑直接用 `rule.SideA == sideA` 比较。`rule.SideA` 是规则表里的本地方向，而 `sideA` 是扫描得到的世界接触边，两者语义不同。初版正反向匹配又把 Tag 和 Side 拆散，导致 `rule.SideA` 可能被错误套到另一个物品上。
- **根因**：规则匹配层混淆了本地方向和世界方向，并且初版正反向判断没有保持 `TagA + SideA`、`TagB + SideB` 的成对绑定。
- **修复**：`Item` 新增 `RotationState` 和 `GetWorldSides(localSides)`，统一把本地方向转为世界方向；`TryMatchNeighbor()` 改为 `forwardMatched / reverseMatched` 两套完整匹配，确保 `rule.TagA` 和 `rule.SideA` 永远绑定同一个物品，匹配成功后 `AdjacencyEffect` 仍记录实际扫描到的世界边。
- **沉淀规则（一句话）**：规则表方向和运行时接触方向不是同一语义；匹配前必须转换，并保持标签和方向的归属关系不被拆散。

---

### BUG-018 · 武器倍率通道写完但伤害不变（第 29 课）

- **日期**：2026-08-05 · **所属系统**：武器数值 / 场景接线（WeaponItemStatResolver + BackpackWeaponActivator）
- **现象**：`WeaponBase.Fire()` 已经乘上 `backpackWeaponMultiplier`，`BackpackWeaponActivator` 也调用了 Resolver，但实测不同稀有度/等级武器伤害仍然和之前一样。
- **复现步骤**：① 新增 `WeaponItemStatResolver` 代码 ② 在 `TryActivateItem()` 中读取倍率并写入 `AutoWeapon` ③ Play 后放入高稀有度或高等级武器 ④ 观察伤害数字没有变化。
- **排查过程**：先顺着开火链路确认 `WeaponBase.Fire()` 确实读取了 `backpackWeaponMultiplier`，再检查 `TryActivateItem()` 中是否能拿到 Resolver；最后用场景 YAML 搜索 `WeaponItemStatResolver`，发现场景里没有该组件，`FindAnyObjectByType<WeaponItemStatResolver>()` 找不到对象，逻辑一直走 `1f` 兜底。
- **根因**：代码通道已经写好，但 `WeaponItemStatResolver` 没有挂进 `01-Run` 场景，也没有被 `BackpackWeaponActivator` 引用；Unity 场景接线缺失导致倍率恒为 1。
- **修复**：在 `01-Run.unity` 中添加 `WeaponItemStatResolver`，配置稀有度倍率表与 `levelDamageMultiplier`，并将同一个 Resolver 引用拖给 `BackpackWeaponActivator` 和 `ItemTooltipView`。
- **沉淀规则（一句话）**：Unity 功能不生效要同时查代码链路和场景序列化；代码里有通道，不代表场景里已经接线。

---

### BUG-019 · 合并升级后武器伤害倍率不立刻刷新（第 29 课）

- **日期**：2026-08-05 · **所属系统**：背包数据事件 / 武器激活刷新（InventoryGrid + BackpackWeaponActivator）
- **现象**：同名同级武器合并后，背包 UI 能看到目标武器等级提升，但自动武器伤害倍率不会立刻变化，要等下一次背包重绘或布局变化后才生效。
- **复现步骤**：① 背包内放两把同名同级武器 ② 拖拽其中一把合并到另一把 ③ 观察 UI 等级提升 ④ 立即攻击敌人，伤害仍是旧等级倍率 ⑤ 再触发一次背包变化后伤害才更新。
- **排查过程**：先确认 `WeaponItemStatResolver.GetDamageMultiplier(item)` 对等级计算正确，再检查 `BackpackWeaponActivator` 的刷新触发来源；发现它只订阅 `InventoryGrid.OnChanged`。合并时 `BeginDrag()` 已经先 `Remove(source)` 并触发过一次事件，真正 `TryMerge()` 中 `target.IncreaseLevel()` 后再 `Remove(source)`，由于 source 已不在格子中，不会触发新的 `OnChanged`。
- **根因**：合并升级改变了 `target.Level`，但这个语义变化没有广播给监听者；UI 手动补画能看到等级，战斗系统却没有收到刷新事件。
- **修复**：`InventoryGrid.TryMerge()` 成功后记录 `sourceInGrid = Contains(source)`；如果 source 已经不在背包里，`Remove(source)` 不会触发事件，就由 `TryMerge()` 主动补发 `OnChanged?.Invoke()`。
- **沉淀规则（一句话）**：数据事件要表达语义变化，不只是数组结构变化；物品等级变了，也必须通知所有依赖者刷新。

---

### BUG-020 · Build Settings 残留旧 Run1 场景路径（第 32 课）

- **日期**：2026-08-07 · **所属系统**：场景流 / Build Settings（MainMenu + 01-Run）
- **现象**：工程中实际存在的运行场景是 `Assets/BackpackSurvivor/Scenes/Run/01-Run.unity`，但 Build Settings 里仍记录旧路径 `Assets/BackpackSurvivor/Scenes/Run/Run1.unity`，后续打包或按 Build Index 启动存在风险。
- **复现步骤**：① 打开 `ProjectSettings/EditorBuildSettings.asset` ② 检查 `m_Scenes` ③ 发现路径指向不存在或旧命名的 `Run1.unity`。
- **排查过程**：在开主菜单课前先列出 `Scenes` 目录中的 `.unity` 文件，再对照 `EditorBuildSettings.asset`；发现磁盘场景和 Build Settings 场景清单不一致。
- **根因**：场景曾改名或迁移为 `01-Run.unity`，但 Build Settings 没有同步更新；Unity 不会自动帮你清掉旧场景路径。
- **修复**：新增 `MainMenu.unity` 后重排 Build Settings：`0 = MainMenu.unity`，`1 = 01-Run.unity`，移除旧 `Run1.unity` 路径。
- **沉淀规则（一句话）**：做场景流和 Build 前必须检查 Build Settings；磁盘有场景不代表打包清单里是正确场景。

---

### BUG-021 · 主菜单 UI 分辨率变化后偏移，Scrollbar 死活不好用（第 32 课）

- **日期**：2026-08-07 · **所属系统**：主菜单 UI / UGUI 布局（CanvasScaler + ScrollRect + Scrollbar）
- **现象**：主菜单在屏幕尺寸变化时按钮、背景和面板位置发生明显偏移；制作者声明里的 `Scrollbar Vertical` 初始阶段无法正常滚动或看起来像没生效。
- **复现步骤**：① 打开 `MainMenu` 场景 ② 在 Game 视图切换 1920x1080、1600x900、1366x768、1280x720 等分辨率 ③ 观察 UI 位置漂移；打开声明面板后尝试拖动滚动条。
- **排查过程**：先检查 `CanvasScaler`，发现 `UI Scale Mode = Constant Pixel Size`；再查 Scroll View 的 YAML，发现早期 `Viewport` 和 `Scrollbar Vertical` RectTransform 高度为 0，`BodyText` 高度也未能把 `Content` 撑开。
- **根因**：两个布局问题叠加：① Canvas 没切到 `Scale With Screen Size`，导致固定像素布局在不同分辨率下漂移；② ScrollRect 结构中 Viewport/Scrollbar 没有正确拉伸，Content 没有按文本首选高度撑开。
- **修复**：CanvasScaler 改为 `Scale With Screen Size`，参考分辨率 `1920x1080`，Match `0.5`；Viewport 全屏拉伸并给右侧滚动条留空间；Scrollbar Vertical 右侧上下拉伸；Content 使用 `ContentSizeFitter` + `VerticalLayoutGroup`，BodyText 关闭 Raycast Target 并允许文本撑高。
- **沉淀规则（一句话）**：UGUI 适配先查 CanvasScaler，再查锚点；ScrollRect 不滚，优先查 Viewport 面积、Scrollbar 高度和 Content 是否真的比视口高。

---

### BUG-022 · 背包丢弃再拾取导致物品等级重置（第 33 课）

- **日期**：2026-08-08 · **所属系统**：背包物品状态 / 世界掉落往返（InventorySystem + LootEntry + Item）
- **现象**：背包中已经合并升级的物品，从背包拖出丢到世界后再拾取，等级会回到 Lv.1；武器倍率、芯片效果、价值等依赖等级的收益随之丢失。
- **复现步骤**：① 在背包中合成或放入 Lv.2 物品 ② 拖出背包丢弃到世界 ③ 靠近按 E 拾取 ④ 观察背包中该物品等级变回 Lv.1。
- **排查过程**：先确认 `Item.Level` 在背包中提升正常，再追踪丢弃链路：`DiscardToWorld()` 会把 `Item` 还原成 `LootEntry`，`DropItem.Initialize(entry)` 保存掉落数据，拾取时 `CreateItemFromLootEntry()` 再 new 出 Item。旧链路里 `LootEntry` 没有 level 字段，`Item` 构造函数也默认 `Level = 1`。
- **根因**：`Item -> LootEntry -> Item` 往返链路没有携带运行时等级；世界掉落实例只保存了静态配置和基础数值，丢失了已经升级后的运行时状态。
- **修复**：`LootEntry` 新增 `level = 1`；`DiscardToWorld()` 写入 `level = item.Level`；`CreateItemFromLootEntry()` 把 `entry.level` 传给 `Item` 构造函数；`Item` 构造函数将传入等级钳制到 `1 ~ MaxLevel`。
- **沉淀规则（一句话）**：运行时物品状态跨载体流转时，要检查完整往返链路；不能只保证静态配置能还原。

---

### BUG-023 · 结算后重新开始疑似未完整刷新（第 33 课）

- **日期**：2026-08-08 · **所属系统**：单局状态重置 / 静态运行时状态（GameSession + TargetRegistry + LootChest）
- **现象**：一局结束后点击重新开始，场景看似重新加载，但部分运行态疑似没有像新一局一样刷新，影响后续目标注册或宝箱状态判断。
- **复现步骤**：① 完成或失败一局 ② 在结算页点击重新开始 ③ 进入新一局 ④ 观察场景状态、敌人目标注册、宝箱数量/距离提示是否像新局一样干净。
- **排查过程**：先确认 `ResultView` 已经加载 `01-Run`，再检查有哪些状态不是普通场景对象字段。发现 `TargetRegistry` 和 `LootChest` 都有 static 容器/计数，用来记录敌人目标、未开启宝箱和场上宝箱数量。
- **根因**：static 运行时状态不属于场景对象生命周期；重载场景不能作为这些容器一定清空的保证，尤其在 Unity Enter Play Mode 设置或多场景流程中更容易暴露。
- **修复**：`TargetRegistry` 新增 `Clear()`；`LootChest` 新增 `ResetRuntimeState()`，清空 `unopenedChests` 并重置 `ActiveCount`；`GameSession.StartRun()` 在新一局开始时主动调用这些重置入口。
- **沉淀规则（一句话）**：本局状态如果放进 static，就必须有显式 Reset，并在新一局入口调用。

---

---

### BUG-024 · 主动射击鼠标指向与子弹视觉方向不一致（第 34 课）

- **日期**：2026-08-08 · **所属系统**：主动武器 / 鼠标瞄准 / 等距相机坐标（InputReader + ActiveWeapon + PlayerController）
- **现象**：玩家鼠标指向的是地面上的某个点，但子弹从枪口高度沿水平面飞出，视觉上会感觉“鼠标指的位置”和“子弹飞的方向”有偏差。
- **复现步骤**：① 进入 Run 场景 ② 使用主动武器持续射击 ③ 将鼠标放在玩家周围不同距离和方向 ④ 观察子弹飞行方向与鼠标屏幕位置存在轻微错层。
- **排查过程**：先查 `InputReader.worldPoint`，确认它来自摄像机射线与 `y=0` 地面平面的交点；再查 `ActiveWeapon`，发现旧逻辑从 `firePoint.position` 指向 `worldPoint` 后又把 `direction.y = 0`，实际弹道变成枪口高度的水平线。等距相机下，同一条屏幕射线打到地面和平行的枪口高度平面，XZ 交点本来就不同。
- **根因**：瞄准点语义没有分层：角色转向和地面交互使用地面点合理，但枪口发射应该使用枪口高度平面上的鼠标交点；旧逻辑把地面点和枪口水平弹道混用。
- **修复**：`InputReader` 新增 `TryGetMousePointOnPlane(float y, out Vector3 point)`；`ActiveWeapon` 按 `firePoint.position.y` 获取鼠标在枪口高度平面上的交点，再计算水平弹道；`PlayerController` 同步用 `bodyPivot.position.y` 的平面交点做视觉转向，使身体朝向和主动射击方向一致。
- **沉淀规则（一句话）**：鼠标屏幕位置不是单一世界点；不同高度平面会得到不同交点，瞄准、移动、交互要按系统语义选择平面。

---

### BUG-025 · Build 中子弹与装备掉落物颜色异常（第 35 课）

- **日期**：2026-08-08 · **所属系统**：战斗表现 / 掉落物表现 / URP Build（Projectile + DropItem）
- **现象**：Editor/Play Mode 中子弹和装备掉落物颜色看起来正常，但在 Development Build/Player 中颜色不对劲，像是颜色没有正确渲染或没有吃到脚本设置。
- **复现步骤**：① 打包 Development Build ② 进入 Run 场景实机游玩 ③ 观察子弹模型与装备掉落物模型颜色 ④ 对比 Editor Play Mode 的颜色表现。
- **排查过程**：先确认 Build 后期波次没有明显卡顿，说明问题不是性能掉帧导致的视觉错觉；再检查 `Projectile` 和 `DropItem`，发现两者都在 `Awake()` 中用 `GameObject.CreatePrimitive(PrimitiveType.Sphere)` 运行时生成视觉模型，并直接通过 `renderer.material.color` 改色；对应 prefab 本身没有显式 Renderer/Material 资产承载这套颜色表现。
- **根因（初判）**：运行时临时 Primitive 依赖 Unity 默认材质/默认 Shader，再用 `renderer.material.color` 动态改色。Editor 中默认资源完整可用，Build 中 URP Shader stripping、默认材质变体和颜色属性映射更容易暴露差异，导致颜色表现不稳定。
- **修复**：新增 `M_RuntimeVisual_Unlit_Base` 显式 URP Unlit 材质资产；`Projectile` 和 `DropItem` 增加序列化材质引用，并在对应 prefab 上接入该材质；颜色改为通过 `MaterialPropertyBlock` 同时写入 `_BaseColor` / `_Color`，保留子弹黄色与装备掉落物按稀有度变色的表现，同时避免继续依赖运行时默认材质。
- **沉淀规则（一句话）**：Build 里要稳定显示的视觉资源必须显式资产化；运行时临时创建的默认材质，在 Editor 里能看不代表 Player 里可靠。

---

> 下一条从 BUG-026 开始。
