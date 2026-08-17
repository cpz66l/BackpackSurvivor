using BS.GamePlay.Player;
using UnityEngine;

namespace BS.GamePlay.Combat
{
    public class ActiveWeapon : WeaponBase
    {
        [SerializeField] private float fireInterval = 1f;

        //获取输入
        private InputReader ir;

        private float fireTimer = 0f;

        private void Awake()
        {
            ir = GetComponentInParent<InputReader>();
            if (firePoint == null) firePoint = transform;
            fireTimer = fireInterval;
            CacheStats();
            CacheSfx();
            CacheGameSession();
        }


        private void Update()
        {
            fireTimer += Time.deltaTime;
            //开火
            float finalFireInterval = fireInterval / stats.FireRateMultiplier;
            if (fireTimer > finalFireInterval && ir.AttackHeld)
            {
                if (!ir.TryGetMousePointOnPlane(firePoint.position.y, out Vector3 aimPoint))
                    return;

                Vector3 direction = aimPoint - firePoint.position;
                direction.y = 0f;

                if (direction.sqrMagnitude < 0.0001f)
                    return;

                Fire(direction.normalized);
                fireTimer = 0f;
            }
           
        }
    }
}
