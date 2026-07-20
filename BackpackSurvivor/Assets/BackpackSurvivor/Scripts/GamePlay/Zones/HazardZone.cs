using BS.GamePlay.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace BS.GamePlay.Zones
{
    public class HazardZone : MonoBehaviour
    {
        [SerializeField] private float damagePerSecond = 10f;
        [SerializeField] private float tickInterval = 0.5f;

        private readonly List<IDamageable> targetsInZone = new List<IDamageable>();
        private float tickTimer = 0f;

        void Update()
        {
            tickTimer -= Time.deltaTime;
            if (tickTimer <= 0)
            {
                tickTimer = tickInterval;
                for (int i = targetsInZone.Count - 1; i >= 0; i--)
                //若目标死亡，直接从缓存中移除，列表索引向前补齐，避免索引混乱，所以倒序遍历
                {
                    if (targetsInZone[i] == null || targetsInZone[i].IsDead)
                    {
                        targetsInZone.RemoveAt(i);
                        continue;
                    }
                    DamageInfo info = new DamageInfo(damagePerSecond * tickInterval, null, targetsInZone[i].Position, false, 0f);
                    targetsInZone[i].TakeDamage(info);
                }
            }

        }

        private void OnTriggerEnter(Collider other)
        {
            //先判读是否是可伤害对象
            IDamageable target = other.GetComponent<IDamageable>();
            if (target == null) return;
            //判断阵营
            if (target.Faction != Faction.Player) return;
            //登记进缓存，之后 tick 直接用，不再做物理查询
            if (!targetsInZone.Contains(target)) targetsInZone.Add(target);
        }
        private void OnTriggerExit(Collider other)
        {
            IDamageable target = other.GetComponent<IDamageable>();
            if (target == null) return;

            targetsInZone.Remove(target);
        }

    }
}
