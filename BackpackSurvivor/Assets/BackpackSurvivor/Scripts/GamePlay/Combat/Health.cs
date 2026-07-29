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

        void Awake()
        {
            cachedCollider = GetComponent<Collider>();
            if (cachedCollider == null)
            {
                cachedCollider = GetComponentInChildren<Collider>();
            }

            currentHp = maxHp;
        }
        public void TakeDamage(DamageInfo info)
        {
            //如果已经死亡或者无敌帧还没结束，就不处理伤害
            if (IsDead) return;
            //计算最终伤害，这里可以加入防御力、抗性等因素
            currentHp = Mathf.Clamp(currentHp - info.damage, 0f, maxHp);
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
    }
}
