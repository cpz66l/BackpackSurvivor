using UnityEngine;

namespace BS.GamePlay.Enemies
{
    [RequireComponent(typeof(CharacterController))]
    public class EnemyMovement : MonoBehaviour
    {
        private readonly Collider[] neighborBuffer = new Collider[8];
        private readonly RaycastHit[] obstacleHits = new RaycastHit[1];
        private readonly RaycastHit[] sideObstacleHits = new RaycastHit[1];

        [SerializeField] private float rotateSpeed = 360f;

        [SerializeField] private float separationRadius = 1.2f; //分离力检测半径
        [SerializeField] private float separationWeight = 1.2f; //分离力强度

        [SerializeField] private float obstacleDetectDistance = 1.2f;   //障碍物检测距离
        [SerializeField] private float obstacleAvoidWeight = 1.5f;  //避免障碍物力强度

        [SerializeField] private LayerMask enemyLayer;  //敌人层
        [SerializeField] private LayerMask obstacleLayer;   //障碍物层

        [SerializeField] private float directionUpdateInterval = 0.1f; //距离更新间隔
        [SerializeField] private float directionUpdateJitter = 0.05f;   //更新间隔偏移量
        [SerializeField] private float directionSmoothSpeed = 20f; //方向平滑速度

        private Vector3 desiredMoveDirection;   //低频计算出来的目标方向
        private Vector3 cachedMoveDirection;    //缓存方向
        private float directionUpdateTimer;     //计时器
        private float currentUpdateInterval;    //经过偏移后的更新间隔
        private int avoidSide = 1;              //1 = 右绕，-1 = 左绕

        private CharacterController cc;
        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            avoidSide = (GetInstanceID() & 1) == 0 ? 1 : -1;
            ResetDirectionUpdateTimer();
            directionUpdateTimer = Random.Range(0f, currentUpdateInterval); //初次错峰
        }

        public void Move(Vector3 direction, float moveSpeed)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            directionUpdateTimer += Time.deltaTime;

            if (directionUpdateTimer >= currentUpdateInterval ||    //计时到了就更一次方向
                desiredMoveDirection.sqrMagnitude <= 0.0001f)       //或者上一次目标方向无效就再次更新
            {
                desiredMoveDirection = CalculateMoveDirection(direction);    //更新一次目标方向
                directionUpdateTimer = 0f;
                ResetDirectionUpdateTimer();    //重新抽取更新间隔
            }

            if (cachedMoveDirection.sqrMagnitude <= 0.0001f)
            {
                cachedMoveDirection = desiredMoveDirection;
            }
            else if (desiredMoveDirection.sqrMagnitude > 0.0001f)
            {
                cachedMoveDirection = Vector3.Slerp(
                    cachedMoveDirection,
                    desiredMoveDirection,
                    Mathf.Clamp01(directionSmoothSpeed * Time.deltaTime)
                ).normalized;
            }

            Vector3 moveDirection = cachedMoveDirection;

            //如果缓存方向无效，就临时回退到追逐方向，避免停住
            if (moveDirection.sqrMagnitude <= 0.0001f)
                moveDirection = direction.normalized;

            RotateTowards(moveDirection);
            cc.SimpleMove(moveDirection * moveSpeed);
        }

        private Vector3 CalculateMoveDirection(Vector3 direction)
        {
            Vector3 chaseDirection = direction.normalized; //追逐方向向量
            Vector3 separation = CalculateSeparation();     //分离力向量
            Vector3 obstacleAvoidance = CalculateObstacleAvoidance(chaseDirection); //躲避障碍物方向向量

            Vector3 finalDirection =
                chaseDirection
                + separation * separationWeight
                + obstacleAvoidance * obstacleAvoidWeight;

            finalDirection.y = 0f;

            if (finalDirection.sqrMagnitude <= 0.0001f)
                finalDirection = chaseDirection;
            Vector3 moveDirection = finalDirection.normalized;
            return moveDirection;
        }

        private void RotateTowards(Vector3 direction)
        {
            Quaternion lookTarget = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                lookTarget,
                rotateSpeed * Time.deltaTime
            );
        }

        private Vector3 CalculateSeparation()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, //中心位置
                separationRadius,   //检测半径
                neighborBuffer,     //用于碰撞体缓存的数组
                enemyLayer          //检测的层级
            );

            Vector3 separation = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Collider hit = neighborBuffer[i];
                if (hit == null) continue;
                if (hit.transform == transform) continue;

                Vector3 away = transform.position - hit.transform.position;
                away.y = 0f;

                float sqrDistance = away.sqrMagnitude;
                if (sqrDistance <= 0.0001f) continue;

                separation += away.normalized / sqrDistance;
                //分离向量为排斥力向量的累积，排斥力向量与距离的平方为反比，越近的敌人排斥越强，远一点的影响更弱。
            }

            return Vector3.ClampMagnitude(separation, 1f);
        }

        
        private Vector3 CalculateObstacleAvoidance(Vector3 moveDirection)
        {
            if (moveDirection.sqrMagnitude <= 0.0001f)
                return Vector3.zero;

            Vector3 origin = transform.position + Vector3.up * 0.5f;    //起点：将射线起点抬高 0.5 个单位
            Vector3 direction = moveDirection.normalized;   //移动方向

            int hitCount = Physics.SphereCastNonAlloc(
                origin, //起点
                0.4f,   //发射球型射线的球体半径
                direction,  //方向
                obstacleHits,       //检测到后缓存的位置
                obstacleDetectDistance,     //射线距离
                obstacleLayer,      //检测障碍物层
                QueryTriggerInteraction.Ignore  //忽略触发器
            );

            if (hitCount <= 0)  //前方没有障碍
                return Vector3.zero;

            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;    //向量叉乘计算前进方向的右方向
            Vector3 left = -right;

            bool rightBlocked = IsBlocked(origin, right);
            bool leftBlocked = IsBlocked(origin, left);

            if (avoidSide >= 0 && !rightBlocked) //优先沿用上一次绕行方向，避免左右摇摆
                return right;

            if (avoidSide < 0 && !leftBlocked)
                return left;

            if (!rightBlocked)
            {
                avoidSide = 1;
                return right;
            }

            if (!leftBlocked)
            {
                avoidSide = -1;
                return left;
            }

            return -direction;  //如果左右都有障碍物就后退
        }

        private bool IsBlocked(Vector3 origin, Vector3 direction)
        {
            int count = Physics.SphereCastNonAlloc(
                origin,
                0.4f,
                direction,
                sideObstacleHits,
                obstacleDetectDistance * 0.75f,
                obstacleLayer,
                QueryTriggerInteraction.Ignore
            );

            return count > 0;
        }

        //当正前方无阻碍时 → 不修正（返回零）。
        //正前方有阻碍时：优先沿用上一次绕行方向，避免障碍边缘左右摇摆。
        //当前绕行方向不通时再切到另一侧；左右都不通则后退。

        //如果所有敌人都每 0.15 秒同一帧更新，就会形成周期性尖峰。加一点随机偏移，让计算摊开。
        private void ResetDirectionUpdateTimer()
        {
            currentUpdateInterval = directionUpdateInterval
                + Random.Range(0f, directionUpdateJitter);
        }

        public void Stop()
        {
            desiredMoveDirection = Vector3.zero;
            cachedMoveDirection = Vector3.zero;
        }
    }
}
