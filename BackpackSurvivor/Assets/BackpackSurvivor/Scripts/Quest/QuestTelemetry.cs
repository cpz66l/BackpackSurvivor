using System;
using System.IO;
using System.Linq;
using BS.Quest;
using UnityEngine;

namespace BS.GamePlay.Quest
{
    public static class QuestTelemetry
    {
        [Serializable] sealed class Row { public string utc; public string eventId; public int tier; public int seed; public RunOutcome outcome; public int kills; public int eliteKills; public int chests; public int[] chestsByQuality; public int backpackValue; public int questOnlyItems; public bool completed; }
        public static void Record(QuestInstance quest, QuestRunSnapshot snapshot, QuestOutcome outcome)
        {
            if (snapshot == null) return;
            try { var row=new Row { utc=DateTime.UtcNow.ToString("O"), eventId=quest==null?string.Empty:quest.eventId, tier=quest==null?0:quest.tier, seed=quest==null?0:quest.seed, outcome=snapshot.outcome, kills=snapshot.kills, eliteKills=snapshot.eliteKills, chests=snapshot.chestsOpened, chestsByQuality=snapshot.chestsOpenedByQuality, backpackValue=snapshot.backpackValue, questOnlyItems=snapshot.items==null?0:snapshot.items.Count(i=>i.questOnly), completed=outcome!=null&&outcome.Completed }; File.AppendAllText(Path.Combine(Application.persistentDataPath,"quest_telemetry.jsonl"), JsonUtility.ToJson(row)+"\n"); }
            catch (Exception e) { Debug.LogWarning("[Quest telemetry] write failed: "+e.Message); }
        }
    }
}
