using UnityEngine;

namespace BS.GamePlay.Combat
{
    public class AutoWeapon : WeaponBase
    {
        //武器参数
        [SerializeField] protected float attackInterval = 0.5f;
        [SerializeField] private float maxRange = 15f;        
        //枪身旋转
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float fireAngleTolerance = 3f;//开枪允许的最大偏差
        //枪身位置与枪口位置
        [SerializeField] private Transform aimPivot;

        private float attackTimer = 0f;
        private IDamageable currentTarget;

        private void Awake()
        {
            //缓存枪身与枪口位置，如果没有就返回武器根位置
            if (aimPivot == null) aimPivot = transform;
            if (firePoint == null) firePoint = aimPivot;
        }

        private void LateUpdate()
        {
            currentTarget = TargetRegistry.GetNearestTarget(firePoint.position, maxRange, targetFaction);
            if (currentTarget == null)
            {
                attackTimer = 0f;
                return;
            }

            Vector3 direction = currentTarget.Position - firePoint.position;
            if (direction.sqrMagnitude < 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

            // Player 完成当帧转向后，再写入枪的世界旋转，避免父物体覆盖枪口朝向。
            aimPivot.rotation = Quaternion.RotateTowards(
                aimPivot.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            attackTimer += Time.deltaTime;
            if (attackTimer < attackInterval) return;

            // 枪口尚未对准时先不开火，避免视觉方向和真实弹道明显分离。
            if (Quaternion.Angle(aimPivot.rotation, targetRotation) > fireAngleTolerance) return;

            attackTimer -= attackInterval;
            Fire((currentTarget.Position - firePoint.position).normalized);
        }

        
    }
}
