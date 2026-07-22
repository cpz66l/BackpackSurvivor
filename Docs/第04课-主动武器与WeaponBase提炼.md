# 第 4 课：主动武器 + WeaponBase 提炼——双战斗系统合龙

> 项目：《背包幸存者》Backpack Survivor | 日期：2026-07-22 | 阶段：V0.1 战斗核心原型 · 第 4 课
> 对应 GDD 9.1/9.2：主动攻击武器与被动自动武器并存，共用伤害结算。

## 一、实现了什么

按住鼠标左键朝鼠标指向连续射击，松开即停；与自动武器并行工作——自动清杂、手动点杀的"双战斗系统"正式合龙。同时完成项目首次**基于真实重复的架构提炼**：WeaponBase 基类。

| 文件 | 职责 |
| --- | --- |
| `Combat/WeaponBase.cs`（新建） | 抽象武器基类：弹速/伤害/阵营/射程/firePoint 字段 + `Fire()` 发射 |
| `Combat/ActiveWeapon.cs`（新建） | 主动武器：读输入状态位，冷却到点后朝鼠标地面点射击 |
| `Combat/AutoWeapon.cs`（重构） | 继承 WeaponBase，只保留索敌/转向/角度门槛等差异化逻辑 |
| `Player/InputReader.cs`（升级） | 新增 Attack 输入通道（performed/canceled 状态位）；公开字段升级为属性 |
| `Project/Input/GameInput.inputactions`（升级） | PlayerNormal 新增 Attack 动作（鼠标左键 Button） |

### 核心代码 1：WeaponBase（提炼后的公共部分）

```csharp
namespace BS.GamePlay.Combat
{
    public abstract class WeaponBase : MonoBehaviour   // abstract：只用于继承，禁止直接挂载
    {
        [SerializeField] protected float projectileSpeed = 20f;   // protected：子类可见
        [SerializeField] protected float damage = 5f;
        [SerializeField] protected Faction targetFaction = Faction.Enemy;
        [SerializeField] protected float maxDistance = 30f;
        [SerializeField] protected Transform firePoint;

        protected void Fire(Vector3 direction)
        {
            GameObject bulletObj = new GameObject("bullet");
            bulletObj.transform.position = firePoint.position;
            Projectile bullet = bulletObj.AddComponent<Projectile>();
            bullet.Initialize(projectileSpeed, damage, targetFaction, maxDistance, direction, 0f, gameObject);
        }
    }
}
```

### 核心代码 2：ActiveWeapon

```csharp
namespace BS.GamePlay.Combat
{
    public class ActiveWeapon : WeaponBase
    {
        [SerializeField] private float fireInterval = 1f;
        InputReader ir;
        private float fireTimer = 0f;

        private void Awake()
        {
            ir = GetComponentInParent<InputReader>();   // 武器可能在 Gun 子物体上
            if (firePoint == null) firePoint = transform;
            fireTimer = fireInterval;   // 关键手感：第一发立即出膛，不等冷却
        }

        private void Update()
        {
            fireTimer += Time.deltaTime;
            if (fireTimer > fireInterval && ir.AttackHeld)
            {
                Vector3 direction = ir.worldPoint - firePoint.position;
                direction.y = 0f;                            // 拍平：瞄准点在地面，枪口有高度
                if (direction.sqrMagnitude < 0.0001f) return; // 拍平后判零（顺序不能反！）
                Fire(direction.normalized);
                fireTimer = 0f;
            }
        }
    }
}
```

### 核心代码 3：InputReader 的 Attack 通道

```csharp
public bool AttackHeld { get; private set; }   // 状态位：外部只读，内部可写

// Invoke Unity Events 模式下，动作的每个阶段都会回调此方法
public void Attack(InputAction.CallbackContext ctx)
{
    if (ctx.performed) AttackHeld = true;        // 按下 → 进入连发状态
    else if (ctx.canceled) AttackHeld = false;   // 松开 → 停止
}
```

## 二、关键设计决策与思考

1. **三次法则的正式落地**：第二把武器出现后，对比 AutoWeapon/ActiveWeapon——字段与 Fire 完全相同，差异只在"目标来源（索敌 vs 鼠标）与触发方式（计时 vs 按住）"。相同上提为 WeaponBase，差异留在子类。**基于真实重复的抽象，比凭空设计的基类扎实。**
2. **abstract 的语义**：基类不是给场景直接挂的，`abstract` 把这个意图变成编译器约束。
3. **protected 字段 + [SerializeField]**：继承体系下的标准写法——子类可用，Inspector 可调，外部不可见。
4. **输入三阶段模型**：started/performed/canceled 每阶段都会回调；"按住连发"用 performed/canceled 翻转**状态位**，武器每帧读状态——监听单次事件无法实现持续行为。
5. **瞄准方向拍平 Y 轴**：鼠标地面点 y=0、枪口 y≈1，不拍平子弹会扎地板——与敌人追击忽略 Y 轴同理。
6. **拍平后才判零**：守卫顺序错误等于没守（拍平前 y 分量撑着向量长度）——**顺序敏感的守卫要成对检查**。
7. **第一发即时出膛**：`fireTimer` 初始化为满冷却值，按下即射；手感的一半藏在这种细节里。
8. **重构纪律**：先 commit（回滚点）→ 改结构 → 验证行为零变化。"重构 = 改结构不改行为"，行为变了说明搬错了。

## 三、踩过的坑（排错记录）

- **零向量永生子弹**：鼠标指脚下时方向归零，子弹不飞不死不销毁，内存泄漏——拍平后判零守卫。
- **守卫顺序 bug**：先判零后拍平，y 分量让判零永远失效——逻辑顺序写进 review 清单。
- **按住只打一发**：只监听 performed；正确做法是状态位跟踪。
- **命名空间第三次裸奔**：WeaponBase/ActiveWeapon 落进全局空间——同模块必须同命名空间，写文件头时顺手确认。
- **公开属性小写**：`attackHeld` → `AttackHeld`，公开成员 PascalCase 无例外。

## 四、面试会怎么问

- **Q：什么时候该提炼基类？** A：第二个真实案例出现时，对比异同——相同上提、差异保留；差异是数据的场合甚至不提炼（走配置）。能背出 Rule of Three 并结合自己项目的 WeaponBase/Zone 两个案例。
- **Q：新输入系统怎么接"按住连发"？** A：CallbackContext 三阶段（started/performed/canceled）+ 状态位；PlayerInput 四种 Behavior 的区别（SendMessages/Broadcast/Unity Events/C# Events）能说出本项目用的 Invoke Unity Events 的接线方式。
- **Q：双武器系统怎么避免代码重复？** A：抽象基类 WeaponBase 承载公共发射逻辑，子类只写触发；伤害结算进一步复用 Projectile/DamageInfo 管线——同一份弹道代码被两种武器复用。
- **Q：重构怎么保证安全？** A：先提交回滚点、小步搬运、行为对比验证；IDE 重构工具只做机械移动，语义验证靠运行。

## 五、习惯养成清单

- [x] 重构前必先 commit，改结构不改行为
- [x] 输入行为用状态位建模，不用单次事件
- [x] 方向向量使用前三问：归一化了吗？拍平了吗？判零了吗？
- [x] 手感细节主动打磨（首发即时、冷却减法）
- [ ] （持续欠债）新文件先写命名空间再写类

## 六、下一步

第 5 课：刷怪器 + 对象池——敌人持续生成与高频对象内存复用，V0.1 收官。
