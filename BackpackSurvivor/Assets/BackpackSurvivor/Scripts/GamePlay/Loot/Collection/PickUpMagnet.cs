using BS.GamePlay.Combat;
using BS.GamePlay.Stats;
using BS.GamePlay.Inventory;
using BS.GamePlay;
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
        private ICollectable collectable;
        private PlayerRunStats playerStats;
        [SerializeField] private InventorySystem inventorySystem;
        [SerializeField] private float maxBackpackPickupRangeBonus = 1f;

        private readonly BackpackPassiveCollector passiveCollector = new BackpackPassiveCollector();
        private BackpackGlobalModifier globalModifier;
        private bool subscribedToGrid;
        //状态字段
        private MagnetState magentState = MagnetState.Idle;

        private Vector3 direction;

        private void Awake()
        {
            collectable = GetComponent<ICollectable>();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            playerTf = player.transform;
            playerHealth = player.GetComponent<Health>();
            playerStats = player.GetComponent<PlayerRunStats>();
        }

        private void OnEnable()
        {
            if (inventorySystem == null)
                inventorySystem = FindAnyObjectByType<InventorySystem>();

            if (inventorySystem == null || inventorySystem.Grid == null) return;

            if (!subscribedToGrid)
            {
                inventorySystem.Grid.OnChanged += RefreshBackpackPassives;
                subscribedToGrid = true;
            }

            RefreshBackpackPassives();
        }

        private void OnDisable()
        {
            if (!subscribedToGrid) return;
            if (inventorySystem != null && inventorySystem.Grid != null)
                inventorySystem.Grid.OnChanged -= RefreshBackpackPassives;

            subscribedToGrid = false;
        }

        private void Update()
        {
            if (playerTf == null) return;
            direction = playerHealth.Position - transform.position;
            direction.y = 0f;
            float sqrDistance = direction.sqrMagnitude;
            //处理磁吸范围
            float pickupMultiplier = playerStats != null ? playerStats.PickupRangeMultiplier : 1f;
            float backpackPickupRangeBonus = globalModifier != null ? globalModifier.PickupRangeBonus : 0f;
            backpackPickupRangeBonus = Mathf.Min(backpackPickupRangeBonus, maxBackpackPickupRangeBonus);
            float finalAttractRange = attractRange * pickupMultiplier * (1f + backpackPickupRangeBonus);
            switch (magentState)
            {
                case MagnetState.Idle:
                    if (sqrDistance < finalAttractRange * finalAttractRange)
                    {
                        StartAttract();
                    }
                    break;

                case MagnetState.Attracted:
                    MoveTowardsPlayer();

                    if (sqrDistance <= collectRange * collectRange)
                    {
                        collectable.Collect();
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

        private void RefreshBackpackPassives()
        {
            if (inventorySystem == null || inventorySystem.Grid == null) return;

            globalModifier = passiveCollector.Collect(inventorySystem.Grid.GetUniqueItems());
        }

        private enum MagnetState
        {
            Idle,
            Attracted,
        }
    }
}
