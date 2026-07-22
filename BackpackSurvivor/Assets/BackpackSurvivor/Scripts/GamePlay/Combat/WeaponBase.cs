using BS.GamePlay.Combat;
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

        protected void Fire(Vector3 direction)
        {
            //创造一个空物体，在枪口的位置
            GameObject bulletObj = new GameObject("bullet");
            bulletObj.transform.position = firePoint.position;
            //挂上 Projectile 组件（此刻它的 Awake 立即执行：造出黄色小球视觉）
            Projectile bullet = bulletObj.AddComponent<Projectile>();
            bullet.Initialize(projectileSpeed, damage, targetFaction, maxDistance, direction, 0f, gameObject);
        }
    }
}
