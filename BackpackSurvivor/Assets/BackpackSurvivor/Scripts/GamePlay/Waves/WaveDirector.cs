using BS.GamePlay.Loot;
using BS.GamePlay.Run;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace BS.GamePlay.Waves
{
    public class WaveDirector : MonoBehaviour
    {
        public event Action<int, string, Color> OnWaveStageChanged; //stageIndex, stageName,displayColor
        //嵌套类，波次管理
        [Serializable]
        public class WaveStage {
            public float startTimeSeconds;
            public float spawnInterval;
            public int maxAlive;
            public string stageName;
            public Color displayColor;
            [Range(0f, 1f)] public float eliteSpawnChance;
            [Range(0f, 1f)] public float rangedSpawnChance;
            public int chestKillsToSpawn;
            public int chestMaxFieldCount;
            public ChestSpawner.ChestTierWeight[] chestTierWeights;
            public float normalEnemyMaxHp = 1f;
            public float eliteEnemyMaxHp = 1f;
            public float rangedEnemyMaxHp = 1f;
        }
        [SerializeField] private List<WaveStage> waveStages;

        [SerializeField] private GameSession gameSession;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private ChestSpawner chestSpawner;

        private int currentStageIndex = -1;

        private void Awake()
        {
            if(gameSession == null)
                gameSession = FindAnyObjectByType<GameSession>();
            if(enemySpawner == null)
                enemySpawner = GetComponent<EnemySpawner>();
            if (chestSpawner == null)
                chestSpawner = FindAnyObjectByType<ChestSpawner>();
        }
        

        private void Update()
        {
            if (gameSession == null || enemySpawner == null) return;
            if (gameSession.State != GameState.Running) return;
            if (waveStages == null || waveStages.Count == 0) return;

            //倒序遍历，时间越往后阶段越优先
            for (int i = waveStages.Count - 1; i >= 0; i--)
            {
                if(gameSession.Elapsed >= waveStages[i].startTimeSeconds)
                {
                    int stageIndex = currentStageIndex;
                    currentStageIndex = i;
                    if (stageIndex != currentStageIndex)
                    {
                        //改变敌人生成器的参数
                        if (enemySpawner != null)
                            enemySpawner.ApplyWaveSettings 
                            (waveStages[i].spawnInterval,
                            waveStages[i].maxAlive,
                            waveStages[i].eliteSpawnChance,
                            waveStages[i].rangedSpawnChance,
                            waveStages[i].normalEnemyMaxHp,
                            waveStages[i].eliteEnemyMaxHp,
                            waveStages[i].rangedEnemyMaxHp);
                        //改变宝箱生成器的参数
                        if (chestSpawner != null)
                            chestSpawner.ApplyWaveSettings 
                            (waveStages[i].chestKillsToSpawn,
                            waveStages[i].chestMaxFieldCount,
                            waveStages[i].chestTierWeights);

                        OnWaveStageChanged?.Invoke(currentStageIndex,
                            waveStages[i].stageName, waveStages[i].displayColor);
                    }
                    break;  
                }
            }
        }


    }
}
