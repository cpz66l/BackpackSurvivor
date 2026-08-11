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

        private Health playerH;
        private MapBounds mapBounds;

        private void Awake()
        {
            playerH = GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
            mapBounds = FindAnyObjectByType<MapBounds>();
        }

        private bool TryFindSpawnPoint(out Vector3 result)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 pos = mapBounds.GetRandomPoint();
                float sqrDistToPlayer = (pos - playerH.Position).sqrMagnitude;
                if (sqrDistToPlayer < minDistToPlayer * minDistToPlayer) continue;
                pos.y = 0.5f;
                result = pos;
                return true;
            }
            result = Vector3.zero;
            return false;
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


        private void AddKillsCount()
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
