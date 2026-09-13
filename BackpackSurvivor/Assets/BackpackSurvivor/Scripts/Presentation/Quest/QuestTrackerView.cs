using System.Collections;
using System.Collections.Generic;
using BS.GamePlay;
using BS.GamePlay.Enemies;
using BS.GamePlay.Loot;
using BS.GamePlay.Run;
using BS.Inventory;
using BS.Quest;
using TMPro;
using UnityEngine;

namespace BS.Presentation
{
    public sealed class QuestTrackerView : MonoBehaviour
    {
        [SerializeField] TMP_Text text;
        QuestInstance quest;
        GameSession session;
        InventoryGrid grid;
        Coroutine pendingEnemyRefresh;
        int lastSecond = -1;
        public string DisplayText => text == null ? "" : text.text;

        void Awake()
        {
            session = FindAnyObjectByType<GameSession>();
            if(text){text.enableAutoSizing=true;text.fontSizeMin=12;text.fontSizeMax=17;}
        }
        void OnEnable()
        {
            if (session == null) session = FindAnyObjectByType<GameSession>();
            if (session != null)
            {
                session.OnStateChanged += OnState;
                session.OnTimeChanged += OnTime;
                session.OnXpChanged += OnXp;
            }
            LootChest.OnOpened += OnChest;
            EnemyAI.OnEnemyDied += OnEnemy;
            RefreshSnapshot();
        }
        void Start() { RefreshSnapshot(); }
        void OnDisable()
        {
            if (session != null)
            {
                session.OnStateChanged -= OnState;
                session.OnTimeChanged -= OnTime;
                session.OnXpChanged -= OnXp;
            }
            LootChest.OnOpened -= OnChest;
            EnemyAI.OnEnemyDied -= OnEnemy;
            if (grid != null) grid.OnChanged -= RefreshSnapshot;
            grid = null;
            if (pendingEnemyRefresh != null) StopCoroutine(pendingEnemyRefresh);
            pendingEnemyRefresh = null;
        }
        void OnState(GameState value) { lastSecond = -1; RefreshSnapshot(); }
        void OnTime(float elapsed, float remaining)
        {
            int second = Mathf.FloorToInt(elapsed);
            if (second == lastSecond) return;
            lastSecond = second;
            RefreshSnapshot();
        }
        void OnXp(int total, int level, int current, int next) { RefreshSnapshot(); }
        void OnChest(ChestQuality quality) { RefreshSnapshot(); }
        void OnEnemy(EnemyKind kind)
        {
            // Read after GameSession has recorded the kill, regardless of subscriber order.
            if (pendingEnemyRefresh == null && isActiveAndEnabled)
                pendingEnemyRefresh = StartCoroutine(RefreshAfterDeathEvent());
        }
        IEnumerator RefreshAfterDeathEvent()
        {
            yield return null;
            pendingEnemyRefresh = null;
            RefreshSnapshot();
        }
        void RefreshSnapshot()
        {
            if (session == null) return;
            // Start order is unspecified. Re-read the contract on StartRun broadcasts
            // and subsequent restarts instead of retaining an early null reference.
            quest = session.CurrentQuest;
            if (grid == null)
            {
                grid = FindAnyObjectByType<InventorySystem>()?.Grid;
                if (grid != null) grid.OnChanged += RefreshSnapshot;
            }
            if (session.State == GameState.NotStarted)
            {
                if (text != null) text.text = "正在同步本局合同…";
                return;
            }
            var frozen = session.LastQuestSnapshot;
            Refresh(frozen ?? session.BuildLiveQuestSnapshot());
        }
        public void SetQuest(QuestInstance value) { quest = value; Refresh(null); }
        public void Refresh(QuestRunSnapshot snapshot)
        {
            if (text == null) return;
            if (quest == null) { text.text = "暂无进行中的合同"; return; }
            var result = snapshot == null ? null : QuestEvaluator.Evaluate(quest, snapshot);
            string heading = "合同 " + quest.eventId + " · Tier " + quest.tier;
            if (result == null) { text.text = heading + "\n等待局内数据"; return; }
            var lines = new List<string>();
            for (int i = 0; i < result.Details.Count && i < quest.objectives.Count; i++)
                lines.Add((result.Details[i].satisfied ? "✓ " : "○ ") + ObjectiveText.Format(quest.objectives[i])+"\n  "+ObjectiveText.CurrentProgress(quest.objectives[i],snapshot));
            text.text = heading + "\n" + string.Join("\n", lines) + "\n已满足 " + result.Details.FindAll(d=>d.satisfied).Count + " / " + result.Details.Count + " 条" +
                (snapshot.outcome == RunOutcome.Died ? "\n本局已失效：死亡不计完成" : "\n条件满足后仍需存活至胜利结算");
        }
    }
}
