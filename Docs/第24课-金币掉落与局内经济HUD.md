# 第24课 · 金币掉落与局内经济 HUD

> 课程周期：2026-08-02  
> 前置：第23课《精英宝箱与终局压力强化》  
> 关键词：金币掉落、局内经济、HUD、池化、磁吸、散落飞出、Unity 序列化字段改名

---

## 一、这节课实现了什么

第24课补齐了 Demo 经济系统的第一块拼图：金币现在不是“策划表里的概念”，而是能从怪物/宝箱掉出来、飞散、被磁吸、进入本局统计，并显示在 HUD 上的真实资源。

### 任务 1 · LootManager 接入 Gold 分支

- `LootManager` 从装备/经验两类掉落扩展为装备、经验、金币三类。
- 装备继续走 `dropPool`。
- 经验走 `xpOrbPool`。
- 金币走新增的 `goldOrbPool`。
- 经验和金币都从死亡点或开箱点先飞散到周围，再恢复磁吸。

### 任务 2 · 新增 GoldOrb

- `GoldOrb` 独立于 `XpOrb`，实现 `IPoolable` 和 `ICollectable`。
- 使用 `PickUpMagnet` 做自动吸附，和经验球保持一致的拾取手感。
- 使用 `OnCollected` 静态事件把金币收集事实广播给 `GameSession`。
- 支持 15 秒自然回收，避免场上长期残留。

### 任务 3 · XP/Gold 掉落飞散统一

- `XpOrb` 和 `GoldOrb` 都加入 `PlayScatterFlight()`。
- 飞行期间临时关闭 `PickUpMagnet`，避免刚生成就被玩家隔空吸走。
- 落地后恢复磁吸并调用 `StateReset()`，避免池化对象带着旧吸附状态复活。

### 任务 4 · GameSession 统计本局金币

- `GameSession` 新增 `totalGold` 和只读属性 `TotalGold`。
- 新增 `OnGoldChanged` 事件。
- `StartRun()` 开局重置金币并广播 0。
- `HandleGoldCollected()` 在 Running 状态下累加 `LootEntry.amount`，再广播给 HUD。

### 任务 5 · RunHudView 显示金币

- `RunHudView` 新增 `goldText`。
- 订阅 `GameSession.OnGoldChanged`。
- `Start()` 主动拉取 `gameSession.TotalGold`，避免初始化时序漏掉开局广播。
- 场景中已接入金币文本和金币图标。

### 任务 6 · 结算页金币显示暂缓

本课原计划包含结算页金币显示，但当前用户选择先不接入结算页。  
这是合理裁剪：Demo 冲刺期先保证局内经济反馈成立，结算页统计可以和第25课“背包价值/结算分数”一起统一设计。

---

## 二、最终代码（关键片段）

### 1. LootManager 的金币生成分支

```csharp
else if (entry.category == DropCategory.Gold)
{
    Vector2 randomOffset = Random.insideUnitCircle * offset;
    Vector3 target = position + new Vector3(randomOffset.x, 0, randomOffset.y);

    GoldOrb goldOrb = goldOrbPool.Get(position).GetComponent<GoldOrb>();
    goldOrb.Initialize(entry);
    goldOrb.PlayScatterFlight(position, target);

    return goldOrb.gameObject;
}
```

这段的价值不只是“多了一个 if”。  
它把 Gold 纳入原有掉落管线，让敌人、宝箱、未来 GM 工具或商店都能复用 `SpawnEntry()`。

### 2. GoldOrb 的收集事件

```csharp
public static event Action<LootEntry> OnCollected;

public void Collect()
{
    if (isCollected) return;
    isCollected = true;

    OnCollected?.Invoke(lootEntry);
    Recycle();
}
```

`GoldOrb` 不自己改 HUD，也不自己改存档。  
它只广播“我被收集了，数值是多少”，本局金币事实由 `GameSession` 统一维护。

### 3. 池化复活时重置飞行和磁吸

```csharp
public void OnGetFromPool()
{
    if (flightRoutine != null)
    {
        StopCoroutine(flightRoutine);
        flightRoutine = null;
    }

    pum.enabled = true;
    pum.StateReset();

    survivalTimer = 0f;
    isCollected = false;
}
```

这一段是池化课反复强调的铁律：新增运行期字段的同一分钟，必须进 `OnGetFromPool()` 归零。  
飞行协程、磁吸状态、自然回收计时、幂等守卫都属于“前世记忆”。

### 4. 散落飞出协程

```csharp
private IEnumerator FlyRoutine(Vector3 from, Vector3 to)
{
    pum.enabled = false;

    float t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime / flightDuration;
        if (t > 1f) t = 1f;

        Vector3 horizontal = Vector3.Lerp(from, to, t);
        float height = arcHeight * 4f * t * (1f - t);
        transform.position = horizontal + Vector3.up * height;

        yield return null;
    }

    transform.position = to;
    pum.enabled = true;
    pum.StateReset();
    flightRoutine = null;
}
```

这段让金币不再是“刷在原地的数字”，而是有一个短促、可见、可感知的掉落反馈。

### 5. GameSession 作为本局金币事实源

```csharp
private int totalGold;
public int TotalGold => totalGold;

public event Action<int> OnGoldChanged;

private void HandleGoldCollected(LootEntry entry)
{
    if (entry == null) return;
    if (State != GameState.Running) return;

    totalGold += entry.amount;
    OnGoldChanged?.Invoke(totalGold);
}
```

金币属于“本局资源”，所以放在 `GameSession` 里比放在 HUD 里更正确。  
HUD 是显示器，`GameSession` 才是规则事实源。

### 6. RunHudView 的金币显示

```csharp
private void HandleGoldChanged(int totalGold)
{
    if (goldText == null) return;
    goldText.text = $"{totalGold}";
}
```

HUD 只订阅事件并刷新文本，不参与金币计算。  
这种分工能让后续结算页、背包价值页、战后统计页都从同一个事实源取数。

---

## 三、周期链路

### 敌人掉金币链路

```text
EnemyAI.Die()
  ↓
lootManager.TrySpawnDrop(health.Position, lootTable)
  ↓
LootRoller.RollBundle(bundle)
  ↓
LootManager.SpawnEntry(entry, position)
  ↓
entry.category == Gold
  ↓
goldOrbPool.Get(position)
  ↓
GoldOrb.Initialize(entry)
  ↓
GoldOrb.PlayScatterFlight(position, target)
  ↓
落地后恢复 PickUpMagnet
  ↓
玩家靠近后 Collect()
```

### 金币进入本局统计链路

```text
GoldOrb.Collect()
  ↓
GoldOrb.OnCollected(lootEntry)
  ↓
GameSession.HandleGoldCollected(entry)
  ↓
totalGold += entry.amount
  ↓
OnGoldChanged(totalGold)
  ↓
RunHudView.HandleGoldChanged(totalGold)
  ↓
HUD 金币文本刷新
```

### 散落飞行与磁吸恢复链路

```text
OnGetFromPool()
  ↓
停止旧 flightRoutine
  ↓
启用 PickUpMagnet 并 StateReset
  ↓
PlayScatterFlight()
  ↓
飞行期间关闭 PickUpMagnet
  ↓
抛物线飞到落点
  ↓
重新启用 PickUpMagnet 并 StateReset
  ↓
进入正常磁吸拾取
```

---

## 四、设计思考

### 1. 为什么金币要独立成 GoldOrb，而不是复用 XpOrb？

经验和金币都可以磁吸，但它们不是同一种资源。  
经验影响升级节奏，金币影响经济和结算价值，未来还可能影响商店、撤离收益、局外评价。

所以正确拆法是：

```text
共享：PickUpMagnet / IPoolable / ICollectable / 散落飞行手感
区分：XpOrb.OnCollected → 经验成长
区分：GoldOrb.OnCollected → 本局经济
```

复用组件，不复用概念，这是长期可扩展性。

### 2. 为什么金币统计放在 GameSession？

金币是“一局 Run 的状态”，不是 UI 状态。  
如果 HUD 自己记录金币，结算页、调试面板、未来存档奖励都要再问 HUD，方向就反了。

当前分层是：

```text
GoldOrb：事件源，只说“我被捡了”
GameSession：事实源，统计“本局拿了多少”
RunHudView：表现层，只显示“现在是多少”
```

这也是以后扩展 RunResult 的最短路径。

### 3. 为什么飞行期间要禁用磁吸？

如果刚生成就允许磁吸，金币可能从敌人死亡点直接飞向玩家。  
玩家看到的是“怪死了，钱消失了，数字变了”，掉落反馈就弱。

短暂禁用磁吸的目的，是先完成掉落演出，再进入拾取规则：

```text
掉落反馈优先
落点稳定
之后才允许自动吸附
```

这会让搜刮体验更像“掉出了东西”，而不是后台结算。

### 4. 为什么本课不强行接结算页？

本课的核心是局内经济闭环：可掉、可捡、可看。  
结算页金币显示和背包价值、物品价值、最终评分天然是一组问题，放到第25课一起设计更干净。

Demo 冲刺期不是每个计划点都要硬塞完。  
当一个功能会牵动结算模型时，先把它挂到同一类系统里统一处理，反而更省。

### 5. 为什么字段改名是 Unity 项目的高风险操作？

`goldOrbPool` 这种 `[SerializeField] private` 字段虽然是 private，但 Unity 会把它序列化到场景 YAML。  
一旦字段从 `glodOrbPool` 改名成 `goldOrbPool`，Unity 不一定知道这是同一个字段，Inspector 引用就可能丢失。

所以这类改名后必须检查：

```text
Inspector 引用是否还在
Scene/Prefab 是否保存
YAML 里是否还残留旧字段名
Play 后 Console 是否有 NRE
```

必要时用 `[FormerlySerializedAs("oldName")]` 保引用。

---

## 五、踩坑记录

### 1. 字段改名导致对象池引用丢失

- **现象**：击杀敌人后在 `lootManager.TrySpawnDrop(health.Position, lootTable)` 附近出现空引用。
- **排查**：表面看像 `EnemyAI` 或 `lootManager` 为空，但真正的空引用发生在 Gold 分支内部。
- **根因**：字段从拼错的 `glodOrbPool` 改为 `goldOrbPool` 后，场景中原来的 Inspector 引用没有自动迁移。
- **修复**：重新把 `goldOrbPool` 接到场景中的 GoldOrb 对象池，并保存场景。
- **沉淀**：Unity 序列化字段改名后，必须检查 Inspector 引用；需要保引用时用 `[FormerlySerializedAs]`。

### 2. 池化对象新增协程状态必须归零

- **现象风险**：一个金币球飞行中回池，再复用时可能继续旧协程或磁吸状态异常。
- **根因**：协程句柄、磁吸状态、自然回收计时都属于运行期状态。
- **修复**：`OnGetFromPool()` 停止旧协程、重置磁吸、清零计时和收集守卫。
- **沉淀**：池化对象不相信默认值，只相信本次出池明确写入的值。

### 3. HUD 初始化不能只等事件

- **现象风险**：`GameSession.StartRun()` 可能先广播初始金币，`RunHudView` 后订阅，导致 HUD 初始显示不刷新。
- **修复**：`RunHudView.Start()` 主动读取一次 `gameSession.TotalGold`。
- **沉淀**：事件驱动 UI 最稳的结构是“订阅未来变化 + Start 主动拉当前快照”。

### 4. 结算页金币显示临时后移

- **现象**：本课原计划包含结算页金币显示，但实际实现先聚焦局内 HUD。
- **判断**：这不是功能缺陷，是范围裁剪。
- **原因**：金币结算、背包价值、最终评分属于同一类结算模型，适合第25课统一接入。
- **沉淀**：冲刺期要区分“闭环必须项”和“同类系统统一项”。

---

## 六、面试问答

**Q1：金币掉落是怎么接入原有掉落系统的？**  
> 我没有另写一套金币生成器，而是在 `LootEntry.category` 的 Gold 分支里接入 `goldOrbPool`。敌人、宝箱和未来其他掉落来源都继续走 `LootManager.SpawnEntry()`，只是在品类分发时选择不同对象池。

**Q2：为什么 GoldOrb 不直接修改 HUD？**  
> 因为 HUD 是表现层，不应该拥有经济数据。`GoldOrb` 只负责广播收集事件，`GameSession` 作为本局规则主人统计 `totalGold`，然后通过 `OnGoldChanged` 通知 HUD。这样结算页和其他系统也能复用同一个事实源。

**Q3：金币和经验都能磁吸，为什么不做成同一个 Orb？**  
> 它们共享拾取手感，但语义不同。经验影响升级，金币影响经济。把磁吸抽成 `PickUpMagnet` 复用，把资源类型保留成 `XpOrb / GoldOrb` 分开，能避免后续经济逻辑污染经验逻辑。

**Q4：掉落飞出时为什么要临时关闭 PickUpMagnet？**  
> 如果飞行期间磁吸开启，掉落物可能还没完成演出就被吸走，玩家感知不到奖励落地。先关磁吸，等抛物线飞到落点后再开启，可以保证“掉落反馈”和“自动拾取”两个阶段都清楚。

**Q5：Unity 里为什么改字段名会导致引用丢失？**  
> `[SerializeField]` 字段会按字段名写进场景或 prefab 的序列化数据。字段改名后，Unity 可能认为这是一个新字段，旧字段的数据就对不上了。要么手动重接引用，要么改名前加 `[FormerlySerializedAs]` 保迁移。

**Q6：事件驱动 HUD 为什么还要在 Start 主动刷新？**  
> 因为 Unity 生命周期里，事件可能在 HUD 订阅前已经广播过。HUD 在 `Start()` 拉一次当前快照，可以补上初始化时序差，之后再靠事件实时更新。

**Q7：本课为什么暂不做金币商店？**  
> Demo 冲刺期的目标是先让金币成为可感知资源。商店会引出购买 UI、价格平衡、刷新规则、局内经济循环，成本明显更高。当前只做金币可掉、可捡、可看，性价比最高。

---

## 七、习惯清单

1. 新品类优先接入已有管线，不为 Gold 另开平行系统。
2. 资源事实放规则层，HUD 只显示，不记账。
3. 事件驱动 UI 要“订阅变化 + 主动拉快照”。
4. 池化类新增字段后，同一分钟检查 `OnGetFromPool()`。
5. 飞行动画、磁吸、回收计时这些临时态都要在出池时归零。
6. Unity `[SerializeField]` 字段改名后，必须检查 Inspector 引用。
7. NRE 定位不要只看报错行表面调用点，要继续钻到被调用方法内部。
8. 冲刺期允许把同类结算问题合并到下一课统一做，不硬塞。
9. commit 前继续扫 `UnityEditor` / `ShadowCascadeGUI` / 旧字段名残留。

---

## 八、思考题（附复习参考）

**Q1：如果后续要让金币进入结算页，优先改哪里？**  
> 优先扩展 `RunResult`，让 `GameSession.EndRun()` 把 `TotalGold` 写进结算快照，再让 `ResultView` 只显示 `RunResult`。不要让 `ResultView` 直接查 `GameSession.totalGold`。

**Q2：如果未来金币能受“金币倍率芯片”影响，倍率应该放在 GoldOrb 里吗？**  
> 不应该。`GoldOrb` 是掉落物，不知道玩家的经济倍率。倍率可以放在 `PlayerRunStats` 或专门的经济统计层，由 `GameSession.HandleGoldCollected()` 结算时应用。

**Q3：如果金币飞散时偶尔穿地或飞太高，优先调什么？**  
> 先调 `arcHeight` 和 `flightDuration`，再看落点 `offset`。如果地形高度变化明显，后续需要用地面采样修正 `target.y`，而不是只靠固定 y。

**Q4：如果一个金币球被重复 Collect，会发生什么？**  
> 当前 `isCollected` 会让 `Collect()` 幂等，同一生命周期只广播一次 `OnCollected`，避免重复加钱和重复回池。

**Q5：如果字段改名想保留 Inspector 引用，Unity 推荐怎么做？**  
> 在新字段上临时加 `[FormerlySerializedAs("旧字段名")]`，让 Unity 序列化系统迁移旧数据。确认场景/prefab 保存后，再视情况移除或保留。

---

## 九、下一步

**第25课：背包价值与物品价值显示。**

第24课已经让金币成为局内可见资源，下一课开始把“搜刮价值”呈现给玩家：

- `ItemView` 显示单件价值。
- 背包面板显示当前背包总价值。
- 结算分数从纯战斗统计扩展到“战斗表现 + 搜刮价值”。
- 金币是否进入结算页，可以和背包价值一起统一接入，不在第24课硬塞。

**本课 commit 建议**：`第24课 金币掉落与局内经济HUD`

