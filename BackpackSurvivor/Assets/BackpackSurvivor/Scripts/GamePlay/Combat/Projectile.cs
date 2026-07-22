using UnityEngine;

namespace BS.GamePlay.Combat
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private Faction targetFaction = Faction.Enemy;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private float collisionRadius = 0.075f;

        private Vector3 direction = Vector3.zero;
        private float passDistance = 0f;
        private GameObject attacker;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[8];

        //视觉组件
        private GameObject _visualModel;//子弹模型

        public void Initialize(float speed, float damage, 
            Faction targetFaction, float maxDistance,
            Vector3 direction, float passDistance, GameObject attacker)
        {
            this.speed = speed;
            this.damage = damage;
            this.targetFaction = targetFaction;
            this.maxDistance = maxDistance;
            this.direction = direction;
            this.passDistance = Mathf.Max(0f, passDistance);
            this.attacker = attacker;

            //设置朝向，让子弹模型飞向发射方向
            if(direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

        }

        private void Awake()
        {
            //创建简易子弹模型
            _visualModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _visualModel.transform.SetParent(transform);//让模型的transform与子弹绑定为父子关系
            _visualModel.transform.localScale = Vector3.one * 0.15f;
            _visualModel.transform.localPosition = Vector3.zero;

            //移除碰撞器避免干扰射线检测 (子弹本身不需要物理碰撞)
            Destroy(_visualModel.GetComponent<Collider>());

            //添加简单材质以便视觉识别
            Renderer renderer = _visualModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.yellow;
            }
        }

        private void Update()
        {
            //计算本帧移动距离
            float step = speed * Time.deltaTime;
            //step先给射线用来检查，然后来拿来移位

            Vector3 origin = transform.position;
            Vector3 direct = direction.normalized;

            if (TryGetFirstHit(origin, direct, step, out RaycastHit hit))
            {
                //命中检测
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();//hit为第一次检查命中
                //hit.collider获得命中物体。
                if(damageable != null && damageable.Faction == targetFaction)
                {
                    //墙体也有碰撞体，也会被检测，但damageable == null
                    DamageInfo info = new DamageInfo(damage, attacker, hit.point,false, 0f);//hit.point直接获取受击坐标，方便特效
                    damageable.TakeDamage(info);
                }
                //击中碰撞体就销毁

                //后续可开发穿透飞行

                DestroyBullet();
                return;
            }
            else
            {
                transform.position += direct * step;//单位方向*移动距离 = 移动矢量
                passDistance += step;
            }

            if (passDistance > maxDistance) 
            {
                DestroyBullet();
            }
        }

        private bool TryGetFirstHit(Vector3 origin, Vector3 direct, float distance, out RaycastHit nearestHit)
        {
            nearestHit = default;
            float nearestDistance = float.MaxValue;
            bool foundHit = false;
            Transform attackerRoot = attacker != null ? attacker.transform.root : null;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                collisionRadius,
                direct,
                hitBuffer,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            //选择最近碰撞点，并且排除攻击者本身
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = hitBuffer[i].collider;
                if (hitCollider == null) continue;

                // 忽略发射者整套层级中的碰撞体，防止子弹命中玩家、枪或枪口自身。
                if (attackerRoot != null && hitCollider.transform.root == attackerRoot) continue;//忽略玩家的父子整体结构
                if (hitBuffer[i].distance >= nearestDistance) continue;//忽略第一次命中以外的命中

                nearestDistance = hitBuffer[i].distance;
                nearestHit = hitBuffer[i];
                foundHit = true;
            }

            return foundHit;
        }


        private void DestroyBullet()
        {

            //后续可换成进入对象池

            Destroy(gameObject);//销毁子弹
        }
    }
}
