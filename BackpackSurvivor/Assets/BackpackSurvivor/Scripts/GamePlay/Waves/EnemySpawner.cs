using BS.GamePlay.Combat;
using UnityEngine;
using BS.Core;
using BS.GamePlay.Player;

namespace BS.GamePlay.Waves
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private int maxAlive = 10;
        [SerializeField] private float spawnOutsideRadius = 15f;
        [SerializeField] private float spawnInsideRadius = 10f;
        [SerializeField] private Transform playerTf;
        [SerializeField, Min(1)] private int maxSpawnAttempts = 24;
        // Covers the current 1.5x elite's controller, with a small obstacle clearance.
        [SerializeField] private Vector3 spawnHalfExtents = new Vector3(0.85f, 1.55f, 0.85f);
        private MapBounds mapBounds;
        private float normalEnemyMaxHp = 40f;
        private float eliteEnemyMaxHp = 150f;
        private float rangedEnemyMaxHp = 80f;

        [SerializeField] private ObjectPool normalEnemyPool;
        [SerializeField] private ObjectPool eliteEnemyPool;
        [SerializeField] private ObjectPool rangedEnemyPool;
        [SerializeField, Range(0f, 1f)] private float eliteSpawnChance;
        [SerializeField, Range(0f, 1f)] private float rangedSpawnChance;

        private float spawnTimer = 0f;

        private void Start()
        {
            //如果没拖拽，直接通过找脚本获得Player
            if (playerTf == null)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null) playerTf = player.transform;
            }
            mapBounds = FindAnyObjectByType<MapBounds>();
        }

        private void Update()
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer > spawnInterval && TargetRegistry.Count < maxAlive) 
            {
                // A blocked map consumes this scheduled attempt rather than retrying every frame.
                spawnTimer = 0f;
                if (playerTf == null || !SpawnPositionSampler.TryFindInRing(
                    mapBounds, playerTf.position, spawnInsideRadius, spawnOutsideRadius,
                    1f, spawnHalfExtents, maxSpawnAttempts, out Vector3 spawnPos)) return;
                ObjectPool selectedPool = PickEnemyPool();
                if (selectedPool == null) return;
                if (selectedPool == normalEnemyPool)
                {
                    GameObject obj = selectedPool.Get(spawnPos);
                    obj.GetComponent<Health>()?.SetMaxHpAndReset(normalEnemyMaxHp);
                }
                else if(selectedPool == eliteEnemyPool) 
                {
                    GameObject obj = selectedPool.Get(spawnPos);
                    obj.GetComponent<Health>()?.SetMaxHpAndReset(eliteEnemyMaxHp);
                }
                else if (selectedPool == rangedEnemyPool)
                {
                    GameObject obj = selectedPool.Get(spawnPos);
                    obj.GetComponent<Health>()?.SetMaxHpAndReset(rangedEnemyMaxHp);
                }
            }
        }

        public void ApplyWaveSettings(
            float spawnInterval,
            int maxAlive,
            float eliteSpawnChance,
            float rangedSpawnChance,
            float normalEnemyMaxHp,
            float eliteEnemyMaxHp,
            float rangedEnemyMaxHp)
        {
            if(spawnInterval <= 0||maxAlive <=0) return;
            this.spawnInterval = spawnInterval;
            this.maxAlive = maxAlive;

            this.eliteSpawnChance = Mathf.Clamp01(eliteSpawnChance);//限制在0-1之间
            this.rangedSpawnChance = Mathf.Clamp01(rangedSpawnChance);

            this.normalEnemyMaxHp = Mathf.Max(normalEnemyMaxHp, 1f);
            this.eliteEnemyMaxHp = Mathf.Max(eliteEnemyMaxHp, 1f);
            this.rangedEnemyMaxHp = Mathf.Max(rangedEnemyMaxHp, 1f);
        }

        private ObjectPool PickEnemyPool()
        {
            float randomValue = Random.value;

            if (rangedEnemyPool != null && randomValue < rangedSpawnChance)
                return rangedEnemyPool;

            if (randomValue < eliteSpawnChance + rangedSpawnChance)
                return eliteEnemyPool;

            return normalEnemyPool;
        }
    }
}
