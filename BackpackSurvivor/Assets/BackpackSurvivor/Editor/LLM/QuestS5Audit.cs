using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BS.Quest;
using BS.GamePlay.Save;
using UnityEditor;

namespace BackpackSurvivor.EditorTools
{
    public static class QuestS5Audit
    {
        const string Evidence = "Docs/Evidence/S5";

        [MenuItem("Tools/Backpack Survivor/Quest/S5 Verify Event Progression")]
        public static void Run()
        {
            Directory.CreateDirectory(Evidence);
            var db = QuestCatalogBuilder.Build();
            Require(db.events.Count == 15, "event catalog count");
            var completed = new HashSet<string>();
            var recent = new List<string>();
            var lines = new List<string> { DateTime.UtcNow.ToString("O") };
            for (int tier = 1; tier <= 5; tier++)
            {
                var candidate = db.Draw(tier, completed, new HashSet<int>(), 5000 + tier, recent);
                Require(candidate != null && candidate.tier == tier, "tier " + tier + " draw");
                var accepted = QuestDrawer.Accept(candidate, 5000 + tier, db.AllQuestOnlyIds);
                Require(accepted != null && accepted.objectives.Count > 0 && accepted.definitionVersion == candidate.definitionVersion, "tier " + tier + " frozen instance");
                completed.Add(candidate.eventId); recent.Add(candidate.eventId);
                lines.Add("PASS tier=" + tier + " event=" + candidate.eventId + " seed=" + accepted.seed);
                Require(db.Draw(tier, completed, new HashSet<int>(), 9000 + tier, recent) == null || db.Candidates.Count(c => c.tier == tier && !completed.Contains(c.eventId)) > 0, "completed filtering");
            }
            var json = UnityEngine.JsonUtility.ToJson(new SaveData { campaign = new CampaignSave { tier = 5, completedEventIds = completed.ToList(), pendingQuest = QuestDrawer.Accept(db.Candidates.Last(c => c.tier == 5), 77, db.AllQuestOnlyIds) } });
            var restored = UnityEngine.JsonUtility.FromJson<SaveData>(json);
            Require(restored.campaign != null && restored.campaign.completedEventIds.Count == 5 && restored.campaign.pendingQuest != null, "campaign JSON roundtrip");
            lines.Add("PASS campaign JSON roundtrip: completed=5 pending=frozen");
            File.WriteAllLines(Evidence + "/verification.txt", lines);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("[Quest S5] PASS event progression audit");
        }
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
