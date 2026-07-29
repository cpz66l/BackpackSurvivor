using BS.Core;
using BS.GamePlay.Stats;
using UnityEngine;

namespace BS.GamePlay.Combat
{
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected float projectileSpeed = 20f;
        [SerializeField] protected float damage = 5f;
        [SerializeField] protected Faction targetFaction = Faction.Enemy;
        [SerializeField] protected float maxDistance = 30f;

        [SerializeField] protected Transform firePoint;
        [SerializeField] protected ObjectPool bulletPool;
        protected PlayerRunStats stats;

        private void Awake()
        {
            CacheStats();
        }
        protected void Fire(Vector3 direction)
        {
            if (bulletPool != null)
            {
                // 池子路线：Get 已把子弹摆到指定位置并激活
                Projectile bullet = bulletPool.Get(firePoint.position).GetComponent<Projectile>();
                //重置参数
                float finalDamage = damage * stats.DamageMultiplier;
                bullet.Initialize(projectileSpeed, finalDamage, targetFaction, maxDistance, direction, 0f, gameObject);
            }
            else //无池兜底
            {
                //创造一个空物体，在枪口的位置
                GameObject bulletObj = new GameObject("bullet");
                bulletObj.transform.position = firePoint.position;
                //挂上 Projectile 组件（此刻它的 Awake 立即执行：造出黄色小球视觉）
                Projectile bullet = bulletObj.AddComponent<Projectile>();
                float finalDamage = damage * stats.DamageMultiplier;
                bullet.Initialize(projectileSpeed, finalDamage, targetFaction, maxDistance, direction, 0f, gameObject);
            }
        }

        protected void CacheStats()
        {
            stats = GetComponentInParent<PlayerRunStats>();
            if (stats == null)
                stats = FindAnyObjectByType<PlayerRunStats>();
        }
    }
}
