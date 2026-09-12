using BS.Core;
using BS.Data;
using BS.GamePlay.Combat;
using BS.GamePlay.Enemies;
using System;
using UnityEngine;

namespace BS.GamePlay.Loot
{
    public class ChestSpawner : MonoBehaviour
    {
        [Serializable]
        public class ChestTier
        {
            public string chestName;      // "普通宝箱" / "稀有宝箱"
            public Color color;           // 箱身颜色
            public LootTableData bundle;  // 持有的束
            public int weight;            // 出现权重
        }
        [SerializeField] private ChestTier[] tiers;

        //宝箱权重类
        [Serializable]
        public class ChestTierWeight
        {
            public string chestName;
            public int weight;
        }
        private ChestTierWeight[] currentTierWeights;


        [SerializeField] private ObjectPool chestPool;
        [SerializeField] private int killsToSpawn = 20;
        private int killsCount = 0;

        //生成
        [SerializeField] private float minDistToPlayer = 12f;   // 别刷脸
        [SerializeField] private int maxAttempts = 10;         // 重试预算
        [SerializeField] private int maxFieldCount = 5;        // 场上上限
        // Covers the current chest's 1 x 1 x 1.9216 footprint at any horizontal rotation.
        [SerializeField] private Vector3 spawnHalfExtents = new Vector3(1.15f, 0.55f, 1.15f);

        private Health playerH;
        private MapBounds mapBounds;

        private void Awake()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerH = player.GetComponent<Health>();
            mapBounds = FindAnyObjectByType<MapBounds>();
        }

        private bool TryFindSpawnPoint(out Vector3 result)
        {
            result = Vector3.zero;
            return playerH != null && SpawnPositionSampler.TryFindInMap(
                mapBounds, playerH.Position, minDistToPlayer, 0.5f,
                spawnHalfExtents, maxAttempts, out result);
        }

        private bool TrySpawnChest()
        {
            //判空
            if (LootChest.ActiveCount >= maxFieldCount) return false;// 宝箱数量是否超过上限
            if (!TryFindSpawnPoint(out Vector3 pos)) return false;//是否有合适的位置
            if (tiers == null) return false; 

            ChestTier result = PickByWeight(tiers);

            if (result == null) return false;//宝箱信息是否齐全
            LootChest chest = chestPool.Get(pos).GetComponent<LootChest>();
            chest.Initialize(result.chestName, result.color, result.bundle);
            return true;
        }


        private void AddKillsCount(EnemyKind kind)
        {
            killsCount++;
            if(killsCount >= killsToSpawn)
            {
                if (TrySpawnChest())
                    killsCount = 0;
            }
        }

        private ChestTier PickByWeight(ChestTier[] tiers)
        {
            // 计算总权重
            int total = 0;
            foreach (var t in tiers)
            {
                int weight = GetWeightForTier(t);
                if (weight <= 0) continue;
                total += weight;
            }

            if (total <= 0)
                return null; // 无有效权重

            //随机掷点
            int roll = UnityEngine.Random.Range(0, total);
            int accum = 0;
            foreach (var t in tiers)
            {
                int weight = GetWeightForTier(t);
                if (weight <= 0) continue;

                accum += weight;
                if (roll < accum)
                    return t;
            }
            return null;//理论上不会到达
        }

        private int GetWeightForTier(ChestTier tier)
        {
            if (tier == null) return 0;

            if (currentTierWeights != null)
            {
                foreach (var tierWeight in currentTierWeights)
                {
                    if (tierWeight == null) continue;
                    if (tierWeight.chestName == tier.chestName)
                        return Mathf.Max(0, tierWeight.weight);
                }
            }

            return Mathf.Max(0, tier.weight);
        }


        private void OnEnable()
        {
            EnemyAI.OnEnemyDied += AddKillsCount;
        }
        private void OnDisable()
        {
            EnemyAI.OnEnemyDied -= AddKillsCount;
        }

        public void ApplyWaveSettings(int killsToSpawn, int maxFieldCount, ChestTierWeight[] currentTierWeights)
        {
            this.killsToSpawn = Mathf.Max(1, killsToSpawn);
            this.maxFieldCount = Mathf.Max(1, maxFieldCount);
            this.currentTierWeights = currentTierWeights;
        }
    }
}
