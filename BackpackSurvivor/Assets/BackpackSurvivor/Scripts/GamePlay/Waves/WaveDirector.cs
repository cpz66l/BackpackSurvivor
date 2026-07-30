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
        }
        [SerializeField] private List<WaveStage> waveStages;

        [SerializeField] private GameSession gameSession;
        [SerializeField] private EnemySpawner enemySpawner;
        private int currentStageIndex = -1;

        private void Awake()
        {
            if(gameSession == null)
                gameSession = FindAnyObjectByType<GameSession>();
            if(enemySpawner == null)
                enemySpawner = GetComponent<EnemySpawner>();
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
                        enemySpawner.ApplyWaveSettings
                            (waveStages[i].spawnInterval,
                            waveStages[i].maxAlive);
                        OnWaveStageChanged?.Invoke(currentStageIndex,
                            waveStages[i].stageName, waveStages[i].displayColor);
                    }
                    break;  
                }
            }
        }


    }
}