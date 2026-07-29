# 第16课 · 单局框架与基础 HUD

> 课程周期：2026-07-29
> 前置：第 15 课背包武器激活
> 关键词：GameSession、RunTimer、GameState、HUD 快照、血量 HUD、事件刷新、暂停恢复、胜负入口、15 分钟 Demo 骨架

---

## 一、这节课实现了什么

### 任务 1 · 单局状态枚举

- 新增 `GameState`，明确一局生命周期：`NotStarted / Running / Paused / Victory / Defeat`。
- `NotStarted` 用来区分“场景已加载”和“本局已开始”，避免刚进场时系统误判。
- `Paused` 不等于失败或胜利，它只是 `Running` 的临时冻结态。

### 任务 2 · 纯计时器

- 新增 `RunTimer` 普通 C# 类，不继承 `MonoBehaviour`。
- 只负责时间事实：总时长、已过时间、剩余时间、归一化进度、是否到时。
- `Tick(deltaTime)` 只推进时间，不裁决胜负。
- `Remaining` 和 `Normalized` 做边界保护，HUD 显示不会出现负时间或超过 100% 的进度。

### 任务 3 · 本局主人 GameSession

- 新增 `GameSession`，作为一局运行状态的唯一主人。
- 持有 `RunTimer`，在 `Running` 状态下推进计时。
- 监听玩家 `Health.OnDeath`，玩家死亡后进入 `Defeat`。
- 监听 `XpOrb.OnCollected`，累计本局经验并广播给 HUD。
- 15 分钟倒计时结束后进入 `Victory`。
- `StartRun()` 负责初始化时间、经验、等级和状态，并向 HUD 广播初始快照。

### 任务 4 · 基础 HUD

- 新增 `RunHudView`，负责显示血量、时间、经验、等级和状态文本。
- HUD 消费 `GameSession` 的只读属性/事件，也订阅玩家 `Health.OnHealthChanged`，不直接修改任何局内数据。
- `OnEnable` 订阅事件，`OnDisable` 镜像退订。
- `Start()` 主动拉一次当前快照，解决事件广播和 UI 初始化的执行顺序问题。
- `Health` 暴露 `CurrentHp / MaxHp / OnHealthChanged`，HUD 用 Slider 和 HP 文本显示当前血量。

### 任务 5 · 暂停 / 继续边界

- `GameInput.inputactions` 新增 `Pause` 动作，绑定 `<Keyboard>/escape`。
- `InputReader` 新增 `OnPause` 事件和 `Pause(InputAction.CallbackContext ctx)` 方法。
- `GameSession` 订阅 `InputReader.OnPause`，只允许 `Running ↔ Paused` 来回切换。
- 暂停时 `Time.timeScale = 0f`，恢复时 `Time.timeScale = 1f`。
- `Victory / Defeat` 后再按暂停键不会回到 `Running`，避免结算状态被输入误改。

---

## 二、最终代码（关键片段）

### GameState：显式表达局内状态

```csharp
namespace BS.GamePlay.Run
{
    public enum GameState
    {
        NotStarted,
        Running,
        Paused,
        Victory,
        Defeat
    }
}
```

状态枚举的价值是让代码少靠布尔变量猜含义。`isRunning / isPaused / isGameOver` 很快会互相打架；一个状态机枚举能保证同一时刻只有一个主状态。

### RunTimer：只记录时间事实

```csharp
public class RunTimer
{
    private float duration;
    private float elapsed;

    public float Duration => duration;
    public float Elapsed => elapsed;
    public float Remaining
    {
        get
        {
            float remaining = duration - elapsed;
            return remaining < 0f ? 0f : remaining;
        }
    }

    public float Normalized
    {
        get
        {
            if (duration <= 0f) return 1f;

            float normalized = elapsed / duration;
            return normalized < 0f ? 0f : (normalized > 1f ? 1f : normalized);
        }
    }
    public bool IsFinished => elapsed >= duration;

    public RunTimer(float duration)
    {
        this.duration = duration;
        elapsed = 0f;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime < 0f) return;
        elapsed += deltaTime;
    }

    public void Reset()
    {
        elapsed = 0f;
    }
}
```

`RunTimer` 不判断胜负。为什么？因为“时间到了”是事实，“时间到了是否胜利”是玩法规则。事实层和规则层分开，后面做 Boss 战、撤离点、加时机制时不会把计时器污染掉。

### GameSession：本局规则主人

```csharp
public event Action<GameState> OnStateChanged;
public event Action<float, float> OnTimeChanged;
public event Action<int, int> OnXpChanged;

public GameState State => state;
public float Elapsed => timer.Elapsed;
public float Remaining => timer.Remaining;
public float TimeNormalized => timer.Normalized;
public int TotalXp => totalXp;
public int Level => level;
```

`GameSession` 对外给只读属性和事件。HUD 可以看状态，但不能改状态；经验球可以通知被拾取，但不能决定玩家升级；玩家血量可以通知死亡，但不能决定本局结算页面怎么走。

### StartRun：开局快照必须广播

```csharp
public void StartRun()
{
    timer.Reset();
    totalXp = 0;
    level = 1;

    SetState(GameState.Running);
    OnXpChanged?.Invoke(totalXp, level);
    OnTimeChanged?.Invoke(timer.Elapsed, timer.Remaining);
}
```

初始广播很重要。事件驱动 UI 不能只等“变化”，因为 UI 生成时就需要当前值。开局主动发一次快照，HUD 就不会出现空文本或旧值。

### Update：只在 Running 推进计时

```csharp
private void Update()
{
    if (state != GameState.Running) return;

    timer.Tick(Time.deltaTime);
    OnTimeChanged?.Invoke(timer.Elapsed, timer.Remaining);

    if (timer.IsFinished)
        SetState(GameState.Victory);
}
```

暂停、胜利、失败后都不再推进本局时间。这里不是靠 HUD 停止显示，而是根上停止本局逻辑推进。

### 玩家死亡和经验拾取

```csharp
private void HandlePlayerDeath()
{
    if (state != GameState.Running) return;
    SetState(GameState.Defeat);
}

private void HandleXpCollected(LootEntry entry)
{
    if (entry == null) return;
    if (state != GameState.Running) return;

    totalXp += entry.amount;
    OnXpChanged?.Invoke(totalXp, level);
}
```

经验本课只累计和显示，不做升级。升级规则留到第 17 课，这样第 16 课只完成“本局框架和 HUD”，不把成长系统提前混进来。

### 暂停切换：只允许 Running 与 Paused 互转

```csharp
private void TogglePause()
{
    if (state == GameState.Running)
        PauseRun();
    else if (state == GameState.Paused)
        ResumeRun();
}

private void PauseRun()
{
    if (state != GameState.Running) return;
    Time.timeScale = 0f;
    SetState(GameState.Paused);
}

private void ResumeRun()
{
    if (state != GameState.Paused) return;
    Time.timeScale = 1f;
    SetState(GameState.Running);
}
```

`else if` 是关键。否则 `Running` 先进入 `Paused`，下一行又立刻满足 `Paused` 条件，表现上就像按键完全没响应。

### InputReader：输入只翻译意图

```csharp
public event Action OnPause;

public void Pause(InputAction.CallbackContext ctx)
{
    if (ctx.performed)
        OnPause?.Invoke();
}
```

这里不写 `Time.timeScale`。输入层只负责把按键翻译成“暂停意图”，是否允许暂停、暂停后世界怎么停，由 `GameSession` 裁决。


### Health：血量变化事件

```csharp
public float CurrentHp => currentHp;
public float MaxHp => maxHp;
public event Action<float, float> OnHealthChanged;
```

```csharp
currentHp = Mathf.Clamp(currentHp - info.damage, 0f, maxHp);
OnHealthChanged?.Invoke(currentHp, maxHp);
```

血量 HUD 不每帧轮询，而是由 `Health` 在血量变化时广播。这样受伤、重置血量、未来回血都能走同一条 UI 更新链路。

### RunHudView：订阅事件 + 拉取快照

```csharp
private void OnEnable()
{
    if (gameSession == null) return;

    gameSession.OnTimeChanged += HandleTimeChanged;
    gameSession.OnXpChanged += HandleXpChanged;
    gameSession.OnStateChanged += HandleStateChanged;
}

private void Start()
{
    if (gameSession == null) return;

    HandleTimeChanged(gameSession.Elapsed, gameSession.Remaining);
    HandleXpChanged(gameSession.TotalXp, gameSession.Level);
    HandleStateChanged(gameSession.State);
}
```

订阅负责后续变化，`Start()` 主动刷新负责初始显示。这是 UI 事件系统里非常实用的组合拳。

---

## 三、周期链路

### 单局开始链路

```text
Scene Load
  ↓
GameSession.Awake()
  ↓
new RunTimer(runDurationSeconds)
  ↓
GameSession.Start()
  ↓
StartRun()
  ↓
State = Running
  ↓
广播初始 XP / 时间 / 状态
  ↓
RunHudView 显示 HP、15:00、XP、Lv
```

### 时间胜利链路

```text
Update()
  ↓
state == Running ?
  ↓
RunTimer.Tick(Time.deltaTime)
  ↓
OnTimeChanged(elapsed, remaining)
  ↓
HUD 倒计时刷新
  ↓
timer.IsFinished == true
  ↓
SetState(Victory)
  ↓
HUD 显示 VICTORY
```

### 玩家死亡链路

```text
Health.TakeDamage()
  ↓
currentHp <= 0
  ↓
Health.OnDeath
  ↓
GameSession.HandlePlayerDeath()
  ↓
SetState(Defeat)
  ↓
HUD 显示 DEFEAT
```

### 经验显示链路

```text
XpOrb.Collect()
  ↓
XpOrb.OnCollected(lootEntry)
  ↓
GameSession.HandleXpCollected()
  ↓
totalXp += entry.amount
  ↓
OnXpChanged(totalXp, level)
  ↓
RunHudView 刷新 XP / Lv
```

### 血量 HUD 链路

```text
Health.TakeDamage() / ResetToFull()
  ↓
OnHealthChanged(currentHp, maxHp)
  ↓
RunHudView.HandleHealthChanged()
  ↓
hpSlider.normalizedValue = current / max
hpText 显示 HP current/max
```
### 暂停恢复链路

```text
Esc
  ↓
PlayerInput PlayerNormal/Pause
  ↓
InputReader.Pause(ctx)
  ↓
InputReader.OnPause
  ↓
GameSession.TogglePause()
  ↓
Running -> Paused: Time.timeScale = 0
Paused -> Running: Time.timeScale = 1
  ↓
RunHudView 显示 / 隐藏 PAUSED
```

---

## 四、设计思考

### 1. 为什么需要 GameSession？

因为 Demo 开始进入“局”的概念了。以前系统像一个个功能零件：敌人会刷、掉落会出、背包能放、武器能激活。但玩家还没有明确的一局目标：从什么时候开始？什么时候赢？什么时候输？暂停算不算进行中？

`GameSession` 的价值就是把这些散落的系统收束成一局规则主人。它不替 Health 管血量，不替 XpOrb 管拾取，不替 HUD 管显示；它只裁决“这些事件对本局意味着什么”。

### 2. 为什么 RunTimer 不继承 MonoBehaviour？

计时器本质是数据和算法：给它一个 `deltaTime`，它更新 `elapsed`。它不需要挂场景，不需要生命周期，也不需要知道 Unity 场景里有什么。

做成普通 C# 类有两个好处：边界清楚，未来也容易做测试。`MonoBehaviour` 留给真正需要场景生命周期的 `GameSession`。

### 3. 为什么 HUD 不直接读 Time.time 或经验球？

HUD 是表现层。它应该显示“已经被规则层整理好的结果”，而不是自己去拼规则。血量也一样：`Health` 负责血量事实和变化事件，HUD 只把 `CurrentHp / MaxHp` 投影成 Slider 和文本。

如果 HUD 自己读时间、自己加经验，后面做暂停、升级、结算、重开时会出现多个状态主人。现在 HUD 只订阅 `GameSession`，信息源单一，bug 面就小很多。

### 4. 为什么要同时“订阅事件”和“Start 拉快照”？

只订阅事件会遇到时序问题：如果 `GameSession.StartRun()` 先广播，HUD 后订阅，就会漏掉初始值。

只拉快照也不够，因为后续 XP、时间、状态变化都要实时刷新。

所以正确组合是：`OnEnable` 订阅变化，`Start` 拉一次当前状态。这就是事件驱动 UI 里“初始值 + 后续变化”的完整模型。

### 5. 为什么暂停放 GameSession，不放 InputReader？

Esc 是按键，Pause 是玩家意图，真正能不能暂停是局内规则。

胜利后按 Esc 不应该回到游戏，死亡后按 Esc 也不应该复活。这个判断只有 `GameSession` 知道。`InputReader` 如果直接改 `Time.timeScale`，就会绕过状态机，把输入层变成规则层。

### 6. 为什么第 16 课不做升级三选一？

因为第 16 课的目标是“局内骨架成立”。经验现在只累计和显示，已经足够支撑 HUD 和下一课入口。

第 17 课再做经验阈值、升级暂停、三选一、属性修改，会更干净：本课打地基，下课盖成长层。

---

## 五、踩坑记录

### 1. UI 事件广播有初始化时序

- **现象**：如果 HUD 只等事件，初始时间/经验可能不显示。
- **原因**：`GameSession.StartRun()` 和 `RunHudView.Start()` 的执行顺序不应该靠猜。
- **处理**：HUD 订阅事件，同时在 `Start()` 主动拉当前快照。
- **沉淀**：事件用于变化，快照用于初始值。

### 2. Pause 动作要打完整链路

- **现象**：InputActions 里有按键，不代表游戏里真的会响应。
- **链路**：`GameInput.inputactions` 新增 Action → `PlayerInput` 生成对应 UnityEvent → UnityEvent 绑定 `InputReader.Pause` → `InputReader.OnPause` → `GameSession.TogglePause`。
- **沉淀**：新 Input System 的 Invoke Unity Events 模式下，新增 Action 后一定要检查场景事件接线是否同步。

### 3. 暂停切换要用互斥分支

- **现象**：如果 `Running` 和 `Paused` 用两个连续 `if` 判断，可能同一次输入里先暂停再恢复。
- **处理**：使用 `if / else if`，确保一次输入只走一个状态迁移。
- **沉淀**：状态机迁移必须互斥；同一帧内不要让状态变化继续命中下一条分支。

### 4. 全局时间缩放必须有退出口

- **现象**：暂停通过 `Time.timeScale = 0f` 实现，退出或禁用时要恢复。
- **处理**：恢复游戏时设回 `1f`，对象禁用/退出口也兜回 `1f`。
- **沉淀**：凡是改全局状态的系统，都要负责把全局状态还原。

---

## 六、面试问答

**Q1：你为什么要做 GameSession？**
> 因为项目从功能原型进入单局 Demo，需要一个“本局规则主人”。GameSession 负责把时间、死亡、经验、暂停、胜负收口成统一状态，其他系统只提供事件或显示结果。

**Q2：RunTimer 为什么不直接判断胜利？**
> RunTimer 只表达时间事实，GameSession 才表达玩法裁决。这样计时器可以复用，未来如果有 Boss 战、撤离胜利、加时机制，都不用改计时器本身。

**Q3：HUD 为什么不自己计算倒计时和经验？**
> HUD 是表现层，不能成为第二个状态主人。它订阅 GameSession 的局内事件，也订阅 Health 的血量变化事件，只显示结果，不裁决规则。这样暂停、胜负、重开和血量变化都不会出现 UI 与逻辑不一致。

**Q4：为什么 HUD 既订阅事件，又在 Start 主动刷新？**
> 订阅事件只能保证后续变化，不能保证不会错过初始化广播。Start 主动拉一次 GameSession 当前快照，可以解决 Unity 生命周期顺序导致的初始显示空值问题。

**Q5：暂停为什么由 InputReader 发事件，而不是直接暂停？**
> InputReader 只翻译输入意图，不拥有局内规则。是否能暂停、暂停后是否显示 PAUSED、胜利/失败后是否拒绝暂停，都应该由 GameSession 根据状态机裁决。

**Q6：为什么暂停只允许 Running 和 Paused 互转？**
> 因为 Victory 和 Defeat 是终局状态，不能被 Esc 输入拉回 Running。状态机要保护结算结果，避免输入绕开流程。

**Q7：为什么第 16 课只显示等级，不做升级？**
> 当前课目标是建立一局骨架和 HUD。等级先作为显示字段留口，真正的经验阈值、升级暂停、三选一和属性修改放到第 17 课，系统边界更清楚。

---

## 七、习惯清单

1. 新增局内规则时，先问“谁是状态主人”，不要让 HUD、输入、掉落各自改状态。
2. 普通 C# 类优先承载纯算法；需要场景生命周期时再用 `MonoBehaviour`。
3. 事件订阅必须镜像退订，特别是 `Health.OnDeath`、`Health.OnHealthChanged`、`XpOrb.OnCollected`、`InputReader.OnPause`。
4. UI 初始化用“订阅事件 + 拉当前快照”，不要只靠某一次广播。
5. 新 Input System 新增 Action 后，必须检查 PlayerInput 的 UnityEvent 是否真的接上。
6. 状态机分支要互斥，状态改变后不要继续命中下一条判断。
7. 修改 `Time.timeScale` 这种全局状态时，必须设计恢复口和退出兜底。
8. Demo 冲刺期先打通闭环，XP 本课只累计显示，升级选择放到下一课。

---

## 八、思考题（附复习参考）

**Q1：为什么 `GameState` 比多个 bool 更适合当前项目？**
> 多个 bool 很容易出现非法组合，比如 `isRunning == true` 且 `isPaused == true`。枚举状态天然互斥，一局同一时间只能处于一个主状态，状态迁移也更容易 review。

**Q2：如果以后做重开按钮，最应该调用谁？**
> 应该调用 `GameSession.StartRun()` 或由更高层的场景/流程控制器触发它。不要让 HUD 自己重置经验、时间和状态；HUD 只发“玩家点了按钮”的意图，规则主人执行重开。

**Q3：为什么暂停时 `Update()` 不推进 RunTimer？**
> 因为 `Update()` 先判断 `state != Running` 就返回。暂停后状态是 `Paused`，即使某些对象还在执行 Update，本局计时也不会推进。

**Q4：时间显示为什么用 `Remaining` 而不是 `Elapsed`？**
> 对 15 分钟生存 Demo 来说，剩余时间更直接表达目标压力：玩家要知道还要撑多久。`Elapsed` 仍然保留给内部逻辑或未来统计使用。

**Q5：为什么经验拾取后还不升级？**
> 因为“累计经验”和“达到阈值升级”是两层规则。本课只验证经验进入本局账本和 HUD 可见；下一课再接阈值、升级暂停、三选一奖励，避免一次课范围过大。

**Q6：如果暂停后 UI 仍然能响应按钮，这合理吗？**
> 合理。`Time.timeScale = 0` 停的是受缩放时间影响的游戏世界，不等于禁用 UI 输入。暂停菜单、继续按钮、升级选择都需要在暂停时继续响应。

---

## 九、下一步

**第 17 课**：经验成长与三选一。把当前 `totalXp / level` 从显示字段升级为成长系统：经验阈值、升级触发、暂停战斗、三选一 UI、选择后应用属性效果并恢复游戏。

**第 18 课**：波次导演与 15 分钟节奏雏形。让第 16 课建立的 15 分钟时钟真正驱动刷怪强度、精英出现和终局压力。

**提交前检查**：确认 `GameState.cs / RunTimer.cs / GameSession.cs / RunHudView.cs` 及对应 `.meta` 都进入提交；`Health.cs` 已保存 `CurrentHp / MaxHp / OnHealthChanged`；`GameInput.inputactions` 已保存 Pause 动作；`01-Run.unity` 已保存 `GameSession`、HUD、血量 Slider/HP 文本和 PlayerInput 接线；`runDurationSeconds` 最终为 `900`；提交前继续跑 using 闸。

**本课 commit 建议**：`第16课 单局框架与基础HUD`
