using BS.Core;
using BS.Data;
using BS.GamePlay.Combat;
using BS.GamePlay.Loot;
using System;
using UnityEngine;

namespace BS.GamePlay.Enemies
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(Health))]//组件契约
    //确保挂载EnemyAI脚本时，自动挂上。
    public class RangedEnemyAI : MonoBehaviour, IPoolable
    {
        //追击
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float viewRange = 50f;//视野范围
        [SerializeField] private float rotateSpeed = 180f;
        //攻击
        [SerializeField] private float preferredRange = 8f; //远程怪希望保持的射击距离。
        [SerializeField] private float tooCloseRange = 5f;  //玩家贴脸时它应该后退。
        [SerializeField] private Transform firePoint;

        [SerializeField] private float attackInterval = 3f;
        [SerializeField] private float projectileDamage = 5f;
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private float projectileMaxDistance = 30f;
        //掉落物束
        [SerializeField] private LootTableData lootTable;


        private EnemyMovement movement;
        private Health health;
        private Transform playerTf;
        private Health playerHealth;
        private float attackTimer = 0f;
        private ObjectPool projectilePool;
        //掉落物管理
        private LootManager lootManager;

        //对象池
        private ObjectPool pool;
        public void SetPool(ObjectPool p) => pool = p;
        //对应的敌人认领对应的池子
        //若不是从对象池出来的敌人，p =null,即pool =null;
        //后续无法调用pool.Return();

        private void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<EnemyMovement>();
        }
        private void Start()
        {
            //获取玩家信息
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                GetComponent<RangedEnemyAI>().enabled = false;
                return;
            }
            playerTf = player.transform;//查询位置
            playerHealth = player.GetComponent<Health>();//方便查询死亡状态
            ResolveProjectilePool();


        }

        private void Update()
        {
            if (playerTf == null || playerHealth == null || playerHealth.IsDead) return;

            // 模型或碰撞体可能相对根节点有偏移，距离应以实际受击中心计算。
            Vector3 toPlayer = playerHealth.Position - health.Position;
            toPlayer.y = 0;
            float sqrDistance = toPlayer.sqrMagnitude;//取向量模长

            if (sqrDistance > viewRange * viewRange) return;
            else if (sqrDistance > preferredRange * preferredRange)
            {
                movement.Move(toPlayer, moveSpeed);
            }
            else if (sqrDistance < tooCloseRange * tooCloseRange)
            {
                movement.Move(-toPlayer, moveSpeed);
            }//躲避
            else
            {
                movement.Stop();
                RotateTowardsPlayer(toPlayer);  //转向玩家
                TryAttack();
            }
        }

        private void TryAttack()
        {
            attackTimer += Time.deltaTime;

            if (attackTimer < attackInterval)
                return;

            if (projectilePool == null)
            {
                ResolveProjectilePool();
                if (projectilePool == null)
                    return;
            }

            attackTimer -= attackInterval;

            Vector3 fireOrigin = firePoint != null? firePoint.position : health.Position + Vector3.up * 0.6f;
            Vector3 direction = (playerHealth.Position - fireOrigin).normalized;

            GameObject projectileObj = projectilePool.Get(fireOrigin);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(
                    projectileSpeed,
                    projectileDamage,
                    Faction.Player,
                    projectileMaxDistance,
                    direction,
                    0f,
                    gameObject,
                    false
                );
            }
        }

        private void RotateTowardsPlayer(Vector3 toPlayer)
        {
            Quaternion toTarget = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toTarget, rotateSpeed * Time.deltaTime);
        }


        private void OnEnable()
        {
            TargetRegistry.Register(health);//注册
            //订阅死亡事件
            health.OnDeath += Die;
        }

        private void OnDisable()
        {
            TargetRegistry.Unregister(health);//注销
            health.OnDeath -= Die;
        }
        private void Die()
        {
            //广播死亡
            EnemyAI.RaiseEnemyDied();
            //生成掉落物
            lootManager.TrySpawnDrop(health.Position, lootTable);
            //防御，防止忘设pool，或者是没经过池子的敌人
            if (pool != null) pool.Return(gameObject);
            else gameObject.SetActive(false);
            //没有池子地址的敌人Die(),调用pool.Return(),会瞬间NRE
        }

        /// <summary>
        /// 从池子出队，重置血量与攻击计时器等
        /// </summary>
        public void OnGetFromPool()
        {
            health.ResetToFull();
            attackTimer = 0f;
            lootManager = FindAnyObjectByType<LootManager>();
            ResolveProjectilePool();
        }

        public void OnReturnPool()
        {

        }

        //因为远程敌人不仅要实现敌人本体，还要实现武器开火，作为预制体无法拖场景中的对象池，所有要通过子弹池提供器脚本绑定子弹池
        private void ResolveProjectilePool()
        {
            if (projectilePool != null) return;

            projectilePool = ProjectilePoolProvider.FindProjectilePool();
        }
    }
}
