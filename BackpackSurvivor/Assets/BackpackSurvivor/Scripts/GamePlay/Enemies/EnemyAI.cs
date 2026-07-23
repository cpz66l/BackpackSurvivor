using BS.Core;
using BS.GamePlay.Combat;
using BS.GamePlay.Loot;
using UnityEditor.EditorTools;
using UnityEngine;

namespace BS.GamePlay.Enemies
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]//组件契约
    //确保挂载EnemyAI脚本时，自动挂上。
    public class EnemyAI : MonoBehaviour, IPoolable
    {
        //追击
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float viewRange = 10f;//视野范围
        //攻击
        [SerializeField] private float attackRange = 3f;
        [SerializeField] private float attackInterval = 3f;
        [SerializeField] private float contactDamage = 10f;
        [SerializeField] private float knockbackForce = 1f;

        private CharacterController cc;
        private Health health;
        private Transform playerTf;
        private Health playerHealth;
        private float attackTimer = 0f;
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
            cc = GetComponent<CharacterController>();
            health = GetComponent<Health>();
        }
        private void Start()
        {
            //获取玩家信息
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                GetComponent<EnemyAI>().enabled = false;
                return;
            }
            playerTf = player.transform;//查询位置
            playerHealth = player.GetComponent<Health>();//方便查询死亡状态


        } 

        private void Update()
        {
            if (playerTf == null || playerHealth == null || playerHealth.IsDead) return;

            // 模型或碰撞体可能相对根节点有偏移，距离应以实际受击中心计算。
            Vector3 toPlayer = playerHealth.Position - health.Position;
            toPlayer.y = 0;
            float distance = toPlayer.magnitude;//取向量模长

            if (distance > viewRange) return;
            else if (distance > attackRange)
            {
                //转向玩家
                Quaternion lookTarget = Quaternion.LookRotation(toPlayer);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, lookTarget, 360f * Time.deltaTime);
                //移动
                cc.SimpleMove(toPlayer.normalized * moveSpeed);
            }
            else//攻击
            {
                attackTimer += Time.deltaTime;
                if(attackTimer >= attackInterval)
                {
                    attackTimer -= attackInterval;
                    DamageInfo info = new DamageInfo(contactDamage, gameObject, playerHealth.Position ,false,knockbackForce);
                    playerHealth.TakeDamage(info);
                }
            }


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
            lootManager.TrySpawnDrop(health.Position);
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
        }

        public void OnReturnPool()
        {

        }
    }
}
