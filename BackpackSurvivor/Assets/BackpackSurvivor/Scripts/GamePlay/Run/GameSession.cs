using BS.GamePlay.Combat;
using BS.GamePlay.Enemies;
using BS.GamePlay.Loot;
using BS.GamePlay.Player;
using BS.GamePlay.Save;
using BS.GamePlay.Stats;
using BS.GamePlay.Upgrades;
using BS.GamePlay.Quest;
using BS.Inventory;
using BS.Quest;
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
        [SerializeField] private InventorySystem inventorySystem;
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
        private int eliteKillCount;
        private bool isEnding;
        private QuestRunSnapshot lastQuestSnapshot;
        private QuestOutcome lastQuestOutcome;
        private QuestInstance currentQuest;
        public int KillCount => killCount;
        public int EliteKillCount => eliteKillCount;
        public QuestRunSnapshot LastQuestSnapshot => lastQuestSnapshot?.Copy();
        public QuestOutcome LastQuestOutcome => lastQuestOutcome;
        public QuestInstance CurrentQuest => currentQuest;
        public QuestRunSnapshot BuildLiveQuestSnapshot()
        {
            var s = new QuestRunSnapshot { outcome = RunOutcome.Survived, elapsed = Elapsed, level = Level, kills = killCount, eliteKills = eliteKillCount, gold = totalGold, chestsOpened = LootChest.RunOpenedCount, chestsOpenedByQuality = LootChest.CopyRunOpenedByQuality() };
            if (inventorySystem != null && inventorySystem.Grid != null) foreach (var item in inventorySystem.Grid.GetUniqueItems()) { s.backpackValue += item.ScoreValue; s.items.Add(new ItemRecord { id=item.Id, rarity=item.Rarity, tag=item.Tag, level=item.Level, scoreValue=item.ScoreValue, questOnly=item.QuestOnly }); }
            return s;
        }
        private int totalGold;


        //对外只读属性，给HUD
        public GameState State => state;
        public float Elapsed => timer.Elapsed;
        public float Remaining => timer.Remaining;
        public float TimeNormalized => timer.Normalized;
        public int TotalXp => levelProgress.TotalXp;
        public int Level => levelProgress.Level;
        public int CurrentXp => levelProgress.CurrentXp;
        public int XpToNextLevel => levelProgress.XpToNextLevel;
        public int TotalGold => totalGold;
        public GameState GameState => state;

        //HUD 要靠事件刷新
        public event Action<GameState> OnStateChanged;
        public event Action<float, float> OnTimeChanged; // elapsed, remaining
        public event Action<int, int, int, int> OnXpChanged; //totalXp, level, currentXp, xpToNextLevel
        public event Action<int> OnLevelUp; //升级播报
        public event Action<List<LevelUpOption>> OnLevelUpChoiceRequested;//升级能力选择
        public event Action<RunResult> OnRunEnded; //游戏结算
        public event Action<int> OnGoldChanged; //金币


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
            if(inventorySystem == null)
                inventorySystem = FindAnyObjectByType<InventorySystem>();
        }

        private void OnEnable()
        {
            if(playerHealth != null)
                playerHealth.OnDeath += HandlePlayerDeath;
            XpOrb.OnCollected += HandleXpCollected;
            if (inputReader != null)
                inputReader.OnPause += TogglePause;
            EnemyAI.OnEnemyDied += HandleEnemyDied;
            GoldOrb.OnCollected += HandleGoldCollected;
        }
        private void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.OnDeath -= HandlePlayerDeath;
            XpOrb.OnCollected -= HandleXpCollected;
            if (inputReader != null)
                inputReader.OnPause -= TogglePause;
            EnemyAI.OnEnemyDied -= HandleEnemyDied;
            GoldOrb.OnCollected -= HandleGoldCollected;
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
            TargetRegistry.Clear();
            LootChest.ResetRuntimeState();
            timer.Reset();
            levelProgress.Reset();
            levelUpOptionGenerator.ResetRuntimeState();
            SaveService.Instance?.RecordRunStarted();
            killCount = 0;
            eliteKillCount = 0;
            isEnding = false;
            lastQuestSnapshot = null;
            lastQuestOutcome = null;
            currentQuest = SaveService.Instance != null && SaveService.Instance.CurrentData != null && SaveService.Instance.CurrentData.campaign != null
                ? SaveService.Instance.CurrentData.campaign.pendingQuest : null;
            var lootManager = FindAnyObjectByType<LootManager>();
            lootManager?.SetContractRun(currentQuest != null, currentQuest == null ? null : currentQuest.activeQuestOnlyItemIds);
            totalGold = 0;
            //初始广播，对HUD进行初始化
            SetState(GameState.Running);
            BroadcastXpChanged();
            OnTimeChanged?.Invoke(timer.Elapsed,timer.Remaining);
            OnGoldChanged?.Invoke(totalGold);
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
            //处理经验加成
            int finalXp = Mathf.RoundToInt(entry.amount * playerRunStats.XpGainMultiplier);
            int upLevelCount = levelProgress.AddXp(finalXp);
            BroadcastXpChanged();
            sfx?.PlaySfx(SfxId.PickupXp);
            for (int i = 0; i < upLevelCount; i++)
            {
                int reachedLevel = levelProgress.Level - upLevelCount + i + 1;
                OnLevelUp?.Invoke(reachedLevel);
            }
            if (upLevelCount > 0)
            {
                sfx?.PlaySfx(SfxId.LevelUp);
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
            levelUpOptionGenerator.RecordPick(option);//记录选择
            sfx?.PlaySfx(SfxId.LevelUpConfirm);
            //处理最大生命值加成
            if (option.Id == LevelUpOptionId.MaxHpUp)
                playerHealth.ApplyMaxHpBonus(playerRunStats.MaxHpBonus);

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

        private void HandleGoldCollected(LootEntry entry)
        {
            if(entry == null) return;
            if (State != GameState.Running) return;
            //处理金币加成
            int finalGold = Mathf.RoundToInt(entry.amount * playerRunStats.GoldGainMultiplier);
            totalGold += finalGold;
            OnGoldChanged?.Invoke(totalGold);
            sfx?.PlaySfx(SfxId.PickupGold);
        }


        private void TogglePause()
        {
            if(state == GameState.Running)
                PauseRun();
            else if (state == GameState.Paused)
                ResumeRun();
        }
        //暂停
        public void PauseRun()
        {
            if(state != GameState.Running) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }
        //继续
        public void ResumeRun()
        {
            if (state != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Running);
        }

        //统计杀敌数目
        private void HandleEnemyDied(EnemyKind kind)
        {
            if (state != GameState.Running) return;

            killCount++;
            if (kind == EnemyKind.Elite) eliteKillCount++;
        }

        //结束设置
        private void EndRun(GameState finalState)
        {
            if (state != GameState.Running || isEnding) return;
            isEnding = true;
            // Restore the detached item BEFORE state listeners cover/hide the inventory.
            if (inventorySystem != null && inventorySystem.Grid != null)
            {
                foreach (var view in FindObjectsByType<InventoryUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    view.CancelDragForSettlement(inventorySystem.Grid);
            }

            var snapshot = new QuestRunSnapshot
            {
                outcome = finalState == GameState.Victory ? RunOutcome.Survived : RunOutcome.Died,
                elapsed = Elapsed,
                level = Level,
                kills = killCount,
                eliteKills = eliteKillCount,
                gold = totalGold,
                chestsOpened = LootChest.RunOpenedCount,
                chestsOpenedByQuality = LootChest.CopyRunOpenedByQuality(),
                unknownQualityChestsOpened = LootChest.RunUnknownQualityOpenedCount
            };
            int legendaryFoundCount = 0;
            int legendaryCollectedValue = 0;
            // Both result types are derived from this single detached item list.
            if (inventorySystem != null && inventorySystem.Grid != null)
            {
                foreach (Item item in inventorySystem.Grid.GetUniqueItems())
                {
                    snapshot.items.Add(new ItemRecord
                    {
                        id = item.Id, rarity = item.Rarity, tag = item.Tag,
                        level = item.Level, scoreValue = item.ScoreValue, questOnly = item.QuestOnly
                    });
                    snapshot.backpackValue += item.ScoreValue;
                    if (item.Rarity == Rarity.Legendary)
                    {
                        legendaryFoundCount++;
                        legendaryCollectedValue += item.ScoreValue;
                    }
                }
            }
            lastQuestSnapshot = snapshot;
            var campaign = SaveService.Instance != null && SaveService.Instance.CurrentData != null ? SaveService.Instance.CurrentData.campaign : null;
            var questAtSettlement = campaign == null ? null : campaign.pendingQuest;
            if (questAtSettlement != null)
            {
                lastQuestOutcome = QuestEvaluator.Evaluate(questAtSettlement, snapshot);
                if (lastQuestOutcome.Completed)
                {
                    if (!SaveService.Instance.TryCompleteQuest(questAtSettlement, questAtSettlement.isFinal, out string saveError))
                        Debug.LogWarning("[Quest] completion not committed: " + saveError);
                }
            }
            QuestTelemetry.Record(questAtSettlement, snapshot, lastQuestOutcome);
            int backpackValue = snapshot.backpackValue;
            int totalXpAtSettlement = TotalXp;
            SetState(finalState);
            Time.timeScale = 0f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Quest S3] frozen snapshot: " + JsonUtility.ToJson(snapshot));
#endif

            RunResult runResult = new RunResult(finalState,
                snapshot.elapsed,
                snapshot.level,
                totalXpAtSettlement,
                snapshot.kills,
                backpackValue,
                snapshot.gold,
                legendaryFoundCount,
                legendaryCollectedValue
                );

            if (finalState == GameState.Victory)
                SaveService.Instance?.ApplyVictoryResult(runResult);

            OnRunEnded?.Invoke(runResult);//带入结算参数数据包
        }
    }
}
