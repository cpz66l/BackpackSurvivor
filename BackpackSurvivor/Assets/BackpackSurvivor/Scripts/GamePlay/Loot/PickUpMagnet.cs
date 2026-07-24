using BS.GamePlay.Combat;
using UnityEngine;

namespace BS.GamePlay.Loot
{
    public class PickUpMagnet : MonoBehaviour
    {
        [Header("磁吸参数")]
        [SerializeField] private float attractRange = 4f; //磁吸半径
        [SerializeField] private float collectRange = 1f; //拾取半径

        [Header("加速度模式")]
        [SerializeField] private float acceleration = 15f;    // 每秒钟速度增加
        [SerializeField] private float maxSpeed = 20f;

        private float currentSpeed = 0f;

        //引用
        private Transform playerTf;
        private Health playerHealth;
        private DropItem dropItem;
        //状态字段
        private MagnetState magentState = MagnetState.Idle;

        private Vector3 direction;

        private void Awake()
        {
            dropItem = GetComponent<DropItem>();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            playerTf = player.transform;
            playerHealth = player.GetComponent<Health>();
        }

        private void Update()
        {
            if (playerTf == null) return;
            direction = playerHealth.Position - transform.position;
            direction.y = 0f;
            float sqrDistance = direction.sqrMagnitude;

            switch (magentState)
            {
                case MagnetState.Idle:
                    if (sqrDistance < attractRange * attractRange)
                    {
                        StartAttract();
                    }
                    break;

                case MagnetState.Attracted:
                    MoveTowardsPlayer();

                    if (sqrDistance <= collectRange * collectRange)
                    {
                        dropItem.Collect();
                    }
                    break;
            }

        }

        public void StartAttract() => magentState = MagnetState.Attracted;

        private void MoveTowardsPlayer()
        {
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            transform.position += direction.normalized * currentSpeed * Time.deltaTime;
        }

        public void StateReset()
        {
            magentState = MagnetState.Idle;
            currentSpeed = 0f;
        }

        private enum MagnetState
        {
            Idle,
            Attracted,
        }
    }
}