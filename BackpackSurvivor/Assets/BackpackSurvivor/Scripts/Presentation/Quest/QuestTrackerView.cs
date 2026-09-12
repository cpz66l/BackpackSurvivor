using BS.Quest;
using TMPro;
using UnityEngine;

namespace BS.Presentation
{
    public sealed class QuestTrackerView : MonoBehaviour
    {
        [SerializeField] TMP_Text text;
        QuestInstance quest;
        BS.GamePlay.Run.GameSession session;
        int lastSecond = -1;
        void Awake(){session=FindAnyObjectByType<BS.GamePlay.Run.GameSession>();}
        void OnEnable(){if(session==null) session=FindAnyObjectByType<BS.GamePlay.Run.GameSession>(); if(session!=null){session.OnStateChanged+=OnState;session.OnTimeChanged+=OnTime;session.OnXpChanged+=OnXp;session.OnGoldChanged+=OnGold;} BS.GamePlay.Loot.LootChest.OnOpened+=OnChest; BS.GamePlay.Loot.DropItem.OnCollected+=OnItem; BS.GamePlay.Enemies.EnemyAI.OnEnemyDied+=OnEnemy;}
        void OnDisable(){if(session!=null){session.OnStateChanged-=OnState;session.OnTimeChanged-=OnTime;session.OnXpChanged-=OnXp;session.OnGoldChanged-=OnGold;} BS.GamePlay.Loot.LootChest.OnOpened-=OnChest; BS.GamePlay.Loot.DropItem.OnCollected-=OnItem; BS.GamePlay.Enemies.EnemyAI.OnEnemyDied-=OnEnemy;}
        void Start(){if(quest==null&&session!=null) quest=session.CurrentQuest; RefreshSnapshot();}
        void OnState(BS.GamePlay.Run.GameState value){RefreshSnapshot();}
        void OnTime(float a,float b){int second=Mathf.FloorToInt(a); if(second!=lastSecond){lastSecond=second;RefreshSnapshot();}}
        void OnXp(int a,int b,int c,int d){RefreshSnapshot();}
        void OnChest(ChestQuality q){RefreshSnapshot();}
        void OnItem(BS.Data.LootTableData.LootEntry e){RefreshSnapshot();}
        void OnEnemy(BS.GamePlay.Enemies.EnemyKind kind){RefreshSnapshot();}
        void OnGold(int value){RefreshSnapshot();}
        void RefreshSnapshot(){if(session!=null) Refresh(session.State==BS.GamePlay.Run.GameState.Running?session.BuildLiveQuestSnapshot():session.LastQuestSnapshot);}
        public void SetQuest(QuestInstance value) { quest=value; Refresh(null); }
        public void Refresh(QuestRunSnapshot snapshot)
        {
            if (text == null) return;
            if (quest == null) { text.text="暂无进行中的合同"; return; }
            var result=snapshot == null ? null : QuestEvaluator.Evaluate(quest,snapshot);
            if(result == null){text.text="合同 "+quest.eventId+" · Tier "+quest.tier+"\n等待局内数据";return;}
            var lines=new System.Collections.Generic.List<string>();
            for(int i=0;i<result.Details.Count&&i<quest.objectives.Count;i++) lines.Add((result.Details[i].satisfied?"✓ ":"○ ")+ObjectiveText.Format(quest.objectives[i]));
            text.text="合同 "+quest.eventId+" · Tier "+quest.tier+"\n"+string.Join("\n",lines.ToArray())+"\n当前进度 "+result.Progress01.ToString("P0")+(snapshot.outcome==RunOutcome.Died?"\n本局已失效：死亡不计完成":"");
        }
    }
}
