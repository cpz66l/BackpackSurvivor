using UnityEngine;
using BS.GamePlay.Player;

namespace BS.GamePlay.Combat
{
    public class ActiveWeapon : WeaponBase
    {
        [SerializeField] private float fireInterval = 1f;

        //获取输入
        InputReader ir;

        private float fireTimer = 0f;
        private void Awake()
        {
            ir = GetComponentInParent<InputReader>();
            if (firePoint == null) firePoint = transform;
            fireTimer = fireInterval;
        }


        private void Update()
        {
            fireTimer += Time.deltaTime;
            if (fireTimer > fireInterval && ir.AttackHeld)
            {
                Vector3 direction = ir.worldPoint - firePoint.position;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.0001f) return;
                Fire(direction.normalized);
                fireTimer = 0f;
            }
        }
    }
}
