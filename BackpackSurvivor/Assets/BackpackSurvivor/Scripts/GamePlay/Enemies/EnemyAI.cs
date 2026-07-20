using BS.GamePlay.Combat;
using UnityEngine;

namespace BS.GamePlay.Enemies
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]//组件契约
    //确保挂载EnemyAI脚本时，自动挂上。
    public class EnemyAI : MonoBehaviour
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
            //订阅死亡事件
            health.OnDeath += Die;
        } 

        private void Update()
        {
            if (playerTf == null || playerHealth.IsDead) return;
            Vector3 toPlayer = playerTf.position - transform.position;
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

        private void Die()
        {
            Debug.Log($"{gameObject.name} 被击杀");
            health.OnDeath -= Die;
            Destroy(gameObject);
        }
    }
}
