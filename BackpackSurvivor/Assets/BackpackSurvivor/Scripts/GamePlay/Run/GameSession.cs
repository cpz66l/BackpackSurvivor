using BS.GamePlay.Combat;
using BS.GamePlay.Enemies;
using BS.GamePlay.Loot;
using BS.GamePlay.Player;
using BS.GamePlay.Stats;
using BS.GamePlay.Upgrades;
using BS.Presentation;
using System;
using System.Collections.Generic;
using UnityEngine;
using static BS.Data.LootTableData;
namespace BS.GamePlay.Run
{
    public class GameSession : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private float runDurationSeconds = 900f;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private int baseXpToNextLevel = 10;
        [SerializeField] private int xpGrowthPerLevel = 10;

        [SerializeField] private PlayerRunStats playerRunStats;

        [SerializeField] private SfxPlayer sfx;
        private LevelProgress levelProgress;
        private RunTimer timer;
        private GameState state = GameState.NotStarted;
        private LevelUpOptionGenerator levelUpOptionGenerator;
        private int killCount;


        //对外只读属性，给HUD
        public GameState State => state;
        public float Elapsed => timer.Elapsed;
        public float Remaining => timer.Remaining;
        public float TimeNormalized => timer.Normalized;
        public int TotalXp => levelProgress.TotalXp;
        public int Level => levelProgress.Level;
        public int CurrentXp => levelProgress.CurrentXp;
        public int XpToNextLevel => levelProgress.XpToNextLevel;

        //HUD 要靠事件刷新
        public event Action<GameState> OnStateChanged;
        public event Action<float, float> OnTimeChanged; // elapsed, remaining
        public event Action<int, int, int, int> OnXpChanged; //totalXp, level, currentXp, xpToNextLevel
        public event Action<int> OnLevelUp; //升级播报
        public event Action<List<LevelUpOption>> OnLevelUpChoiceRequested;//升级能力选择
        public event Action<RunResult> OnRunEnded; //游戏结算


        private void Awake()
        {
            if (playerHealth == null)
                playerHealth = FindAnyObjectByType<PlayerController>()?.GetComponent<Health>();
            timer = new RunTimer(runDurationSeconds);
            if(inputReader == null)
                inputReader = FindAnyObjectByType<InputReader>();
            levelProgress = new LevelProgress(baseXpToNextLevel, xpGrowthPerLevel);
            levelUpOptionGenerator = new LevelUpOptionGenerator();
            if(playerRunStats == null)
                playerRunStats = FindAnyObjectByType<PlayerRunStats>();
            if (sfx == null)
                sfx = FindAnyObjectByType<SfxPlayer>();
        }

        private void OnEnable()
        {
            if(playerHealth != null)
                playerHealth.OnDeath += HandlePlayerDeath;
            XpOrb.OnCollected += HandleXpCollected;
            if (inputReader != null)
                inputReader.OnPause += TogglePause;
            EnemyAI.OnEnemyDied += HandleEnemyDied;
        }
        private void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.OnDeath -= HandlePlayerDeath;
            XpOrb.OnCollected -= HandleXpCollected;
            if (inputReader != null)
                inputReader.OnPause -= TogglePause;
            EnemyAI.OnEnemyDied -= HandleEnemyDied;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            StartRun();
        }

        private void Update()
        {
            if(state != GameState.Running) return;
            timer.Tick(Time.deltaTime);
            OnTimeChanged?.Invoke(timer.Elapsed ,timer.Remaining);
            if (timer.IsFinished)
                EndRun(GameState.Victory);
        }
        //初始化
        public void StartRun()
        {
            playerRunStats.ResetToDefault();
            timer.Reset();
            levelProgress.Reset();
            killCount = 0;
            //初始广播，对HUD进行初始化
            SetState(GameState.Running);
            BroadcastXpChanged();
            OnTimeChanged?.Invoke(timer.Elapsed,timer.Remaining);
        }
        //设置状态
        private void SetState(GameState nextState)
        {
            if(state == nextState) return;
            state = nextState;
            OnStateChanged?.Invoke(state);
        }

        private void HandlePlayerDeath()
        {
            if(state != GameState.Running) return ;
            EndRun(GameState.Defeat);
        }
        private void HandleXpCollected(LootEntry entry)
        {
            if (entry == null) return;
            if (state != GameState.Running) return;
            int upLevelCount = levelProgress.AddXp(entry.amount);
            BroadcastXpChanged();
            sfx?.PlayPickupXp();
            for (int i = 0; i < upLevelCount; i++)
            {
                int reachedLevel = levelProgress.Level - upLevelCount + i + 1;
                OnLevelUp?.Invoke(reachedLevel);
            }
            if (upLevelCount > 0)
            {
                sfx?.PlayLevelUp();
                RequestLevelUpChoice(levelProgress.Level);
            }
        }
        //进入升级选择
        private void RequestLevelUpChoice(int level)
        {
            if (state != GameState.Running) return;

            Time.timeScale = 0f;
            SetState(GameState.LevelUpSelecting);
            List<LevelUpOption> options = levelUpOptionGenerator.Generate(level, 3);
            OnLevelUpChoiceRequested?.Invoke(options);
        }
        //处理升级选择
        public void ChooseLevelUpOption(LevelUpOption option)
        {
            if (state != GameState.LevelUpSelecting) return;
            if (option == null) return;
            if(playerRunStats == null) return;
            playerRunStats.Apply(option);
            CompleteLevelUpChoice();
        }

        //完成升级选择
        public void CompleteLevelUpChoice()
        {
            if (state != GameState.LevelUpSelecting) return;

            Time.timeScale = 1f;
            SetState(GameState.Running);
        }

        private void BroadcastXpChanged()
        {
            OnXpChanged?.Invoke(
                levelProgress.TotalXp,
                levelProgress.Level,
                levelProgress.CurrentXp,
                levelProgress.XpToNextLevel);
        }

        private void TogglePause()
        {
            if(state == GameState.Running)
                PauseRun();
            else if (state == GameState.Paused)
                ResumeRun();
        }
        //暂停
        private void PauseRun()
        {
            if(state != GameState.Running) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }
        //继续
        private void ResumeRun()
        {
            if (state != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Running);
        }

        //统计杀敌数目
        private void HandleEnemyDied()
        {
            if (state != GameState.Running) return;

            killCount++;
        }

        //结束设置
        private void EndRun(GameState finalState)
        {
            if (state != GameState.Running) return;
            SetState(finalState);
            Time.timeScale = 0f;
            RunResult runResult = new RunResult(finalState,Elapsed,Level,TotalXp,killCount);
            OnRunEnded?.Invoke(runResult);//带入结算参数数据包
        }
    }
}
