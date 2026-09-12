using System;
using System.IO;
using BS.Quest;
using UnityEngine;

namespace BS.GamePlay.Quest
{
    public static class QuestTelemetry
    {
        [Serializable] sealed class Row { public string utc; public RunOutcome outcome; public int tier; public int kills; public int chests; public int backpackValue; public bool completed; }
        public static void Record(QuestInstance quest, QuestRunSnapshot snapshot, QuestOutcome outcome)
        {
            if (snapshot == null) return;
            try { var row=new Row { utc=DateTime.UtcNow.ToString("O"), outcome=snapshot.outcome, tier=quest==null?0:quest.tier, kills=snapshot.kills, chests=snapshot.chestsOpened, backpackValue=snapshot.backpackValue, completed=outcome!=null&&outcome.Completed }; File.AppendAllText(Path.Combine(Application.persistentDataPath,"quest_telemetry.jsonl"), JsonUtility.ToJson(row)+"\n"); }
            catch (Exception e) { Debug.LogWarning("[Quest telemetry] write failed: "+e.Message); }
        }
    }
}
