using BS.GamePlay.Stats;
using System;
using UnityEngine;

namespace BS.GamePlay.Combat
{
    public class Health : MonoBehaviour, IDamageable
    {
        //事件定义
        public event Action<DamageInfo> OnDamaged;
        public event Action OnDeath;
        public event Action<float, float> OnHealthChanged;
        //血量
        [SerializeField] private float maxHp = 100f;
        private float currentHp;
        private float baseMaxHp;
        public float CurrentHp => currentHp;
        public float MaxHp => maxHp;//给外部一个只读接口
        //位置
        [SerializeField] private Transform aimPoint;
        private Collider cachedCollider;
        //阵营
        [SerializeField] private Faction faction;
        //接口实现
        public Faction Faction => faction;
        public bool IsDead => currentHp <= 0f;
        public Vector3 Position
        {
            get
            {
                if (aimPoint != null) return aimPoint.position;
                if (cachedCollider != null) return cachedCollider.bounds.center;
                return transform.position;
            }
        }
        //定义
        private PlayerRunStats stats;

        void Awake()
        {
            cachedCollider = GetComponent<Collider>();
            if (cachedCollider == null)
            {
                cachedCollider = GetComponentInChildren<Collider>();
            }
            //注意这里得区分敌人与玩家，玩家身上有PlayerRunStats组件，这样敌人就不会吃到加成了
            stats = GetComponent<PlayerRunStats>();

            currentHp = maxHp;
            baseMaxHp = maxHp;
        }
        public void TakeDamage(DamageInfo info)
        {
            //如果已经死亡或者无敌帧还没结束，就不处理伤害
            if (IsDead) return;
            //计算最终伤害
            //处理免伤效果，只有玩家有效
            float finalDamage = info.damage;
            if (stats != null) 
                finalDamage *= 1f - stats.DamageReduction; 

            currentHp = Mathf.Clamp(currentHp - finalDamage, 0f, maxHp);
            OnDamaged?.Invoke(info);//触发受伤事件
            OnHealthChanged?.Invoke(currentHp, maxHp);
            //如果死亡，触发死亡事件
            if (currentHp <= 0f)
            {
                currentHp = 0f;//确保血量不会为负数
                OnDeath?.Invoke();//触发死亡事件
            }
        }

        public void ResetToFull()
        {
            currentHp = maxHp;
            OnHealthChanged?.Invoke(currentHp, maxHp);
        }

        public void SetMaxHpAndReset(float newMaxHp)
        {
            if (newMaxHp <= 1f) return;
            baseMaxHp = newMaxHp;
            maxHp = newMaxHp;
            currentHp = maxHp;
            OnHealthChanged?.Invoke(currentHp, maxHp);
        }

        public void ApplyMaxHpBonus(float bonus)
        {
            float newMaxHp = baseMaxHp + bonus;
            float delta = newMaxHp - maxHp;

            maxHp = newMaxHp;
            if (delta > 0f)
                currentHp = Mathf.Min(currentHp + delta, maxHp);
            else
                currentHp = Mathf.Min(currentHp, maxHp);

            OnHealthChanged?.Invoke(currentHp, maxHp);
        }
    }
}
