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
        void Awake(){session=FindAnyObjectByType<BS.GamePlay.Run.GameSession>();}
        void Update(){if(session!=null){if(quest==null) quest=session.CurrentQuest; Refresh(session.State==BS.GamePlay.Run.GameState.Running?session.BuildLiveQuestSnapshot():session.LastQuestSnapshot);}}
        public void SetQuest(QuestInstance value) { quest=value; Refresh(null); }
        public void Refresh(QuestRunSnapshot snapshot)
        {
            if (text == null) return;
            if (quest == null) { text.text="暂无进行中的合同"; return; }
            var result=snapshot == null ? null : QuestEvaluator.Evaluate(quest,snapshot);
            text.text=$"合同 {quest.eventId} · Tier {quest.tier}\n" + (result == null ? "等待局内数据" : $"进度 {result.Progress01:P0}");
        }
    }
}
