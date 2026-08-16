using BS.Core;
using BS.GamePlay.Stats;
using BS.Presentation;
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

        protected float backpackWeaponMultiplier = 1f;
        protected float backpackDamageBoostMultiplier = 1f; // 邻接攻击芯片
        protected float backpackCritChanceBonus = 0f; //邻接瞄准镜，加暴击

        protected SfxPlayer sfx;
        protected PlayerRunStats stats;

        private void Awake()
        {
            CacheStats();
            CacheSfx();
        }
        protected void Fire(Vector3 direction)
        {
            sfx?.PlayShoot();
            //处理暴击效果
            float rawDamage = damage * stats.DamageMultiplier * backpackWeaponMultiplier * backpackDamageBoostMultiplier;
            float finalCritChance = Mathf.Clamp01(stats.CritChance + backpackCritChanceBonus);
            bool isCrit = Random.value < finalCritChance;
            if (isCrit) 
                rawDamage *= stats.CritDamageMultiplier;

            //处理子弹速度
            float finalProjectileSpeed = projectileSpeed * stats.ProjectileSpeedMultiplier;
            if (bulletPool != null)
            {
                // 池子路线：Get 已把子弹摆到指定位置并激活
                Projectile bullet = bulletPool.Get(firePoint.position).GetComponent<Projectile>();
                //重置参数
                float finalDamage = Mathf.RoundToInt(rawDamage);

                bullet.Initialize(finalProjectileSpeed, finalDamage, targetFaction, maxDistance, direction, 0f, gameObject,isCrit);
            }
            else //无池兜底
            {
                //创造一个空物体，在枪口的位置
                GameObject bulletObj = new GameObject("bullet");
                bulletObj.transform.position = firePoint.position;
                //挂上 Projectile 组件（此刻它的 Awake 立即执行：造出黄色小球视觉）
                Projectile bullet = bulletObj.AddComponent<Projectile>();

                float finalDamage = Mathf.RoundToInt(rawDamage);

                bullet.Initialize(finalProjectileSpeed, finalDamage, targetFaction, maxDistance, direction, 0f, gameObject, isCrit);
            }
        }

        protected void CacheStats()
        {
            stats = GetComponentInParent<PlayerRunStats>();
            if (stats == null)
                stats = FindAnyObjectByType<PlayerRunStats>();
        }
        protected void CacheSfx()
        {
            if (sfx == null)
                sfx = FindAnyObjectByType<SfxPlayer>();
        }

        public void SetBackpackWeaponMultiplier(float multiplier)
        {
            backpackWeaponMultiplier = Mathf.Max(1f,multiplier);
        }

        public void SetBackpackDamageBoostMultiplier(float multiplier)
        {
            backpackDamageBoostMultiplier = Mathf.Max(1f, multiplier);
        }

        public void SetBackpackCritChance(float critChance)
        {
            backpackCritChanceBonus = Mathf.Max(0f, critChance);
        }
    }
}
