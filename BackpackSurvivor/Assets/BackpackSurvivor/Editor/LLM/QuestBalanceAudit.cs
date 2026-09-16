using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BS.GamePlay.Quest;
using BS.Quest;
using UnityEditor;
using UnityEngine;

namespace BackpackSurvivor.EditorTools
{
    public static class QuestBalanceAudit
    {
        [MenuItem("Tools/Backpack Survivor/Quest/Verify Balance 2")]
        public static string Run()
        {
            var db = AssetDatabase.LoadAssetAtPath<QuestDatabase>(QuestCatalogBuilder.DatabasePath);
            Require(db != null && db.events.Count == 15, "catalog count");
            var expected = new Dictionary<string, int> {
                {"prototype-t1-2",30}, {"prototype-t2-3",80}, {"prototype-t3-3",140},
                {"prototype-t4-1",220}, {"prototype-t4-2",220},
                {"prototype-t5-1",300}, {"prototype-t5-2",220}, {"prototype-t5-3",180}
            };
            var lines = new List<string> { DateTime.UtcNow.ToString("O") };
            foreach (var definition in db.events)
            {
                Require(definition.definitionVersion == "balance-2", definition.eventId + " version");
                Require(!definition.objectives.Any(c => c.type == ObjectiveType.SurviveToSecond), "redundant survival");
                lines.Add(definition.eventId + ": " + string.Join("; ", definition.objectives.Select(ObjectiveText.Format)));
                if (!expected.TryGetValue(definition.eventId, out int target)) continue;
                var elite = definition.objectives.Single(c => c.type == ObjectiveType.KillElite);
                Require(elite.count == target, definition.eventId + " elite threshold");
                var isolated = new QuestInstance { objectives = new List<ObjectiveClause> { elite.Copy() } };
                var snapshot = new QuestRunSnapshot { outcome = RunOutcome.Survived, eliteKills = target - 1 };
                Require(!QuestEvaluator.Evaluate(isolated, snapshot).Completed, "below elite threshold");
                snapshot.eliteKills = target;
                Require(QuestEvaluator.Evaluate(isolated, snapshot).Completed, "at elite threshold");
                snapshot.outcome = RunOutcome.Died;
                Require(!QuestEvaluator.Evaluate(isolated, snapshot).Completed, "death cannot complete");
                lines.Add("PASS elite below/exact/death: " + definition.eventId);
            }
            foreach (var definition in db.events.Where(d => d.tier == 5))
            {
                var q = QuestDrawer.Accept(definition.ToCandidate(), 216, db.AllQuestOnlyIds);
                Require(q.activeQuestOnlyItemIds.SequenceEqual(db.AllQuestOnlyIds), "full questOnly pool");
                var targetIds = q.objectives.Single(c => c.type == ObjectiveType.CarryItemAnyOf).itemIds;
                Require(targetIds.Count > 0 && targetIds.All(db.AllQuestOnlyIds.Contains), "task item reachable in pool");
                var s = new QuestRunSnapshot { outcome = RunOutcome.Survived, backpackValue = 110000,
                    eliteKills = expected[q.eventId], chestsOpenedByQuality = new[] { 0, 0, 0, 4, 0 },
                    items = new List<ItemRecord> { new ItemRecord { id = targetIds[0], level = 3, questOnly = true } } };
                Require(QuestEvaluator.Evaluate(q, s).Completed, q.eventId + " combined conditions");
                if (q.eventId == "prototype-t5-2")
                {
                    s.chestsOpenedByQuality = new[] { 0, 0, 0, 0, 4 };
                    Require(!QuestEvaluator.Evaluate(q, s).Completed, "Legendary is not exact Epic quality");
                }
                // A frozen instance must retain its own version and conditions through save serialization.
                var frozen = q.Copy(); frozen.definitionVersion = "prototype-1";
                frozen.objectives.Single(c => c.type == ObjectiveType.KillElite).count = 4;
                var restored = JsonUtility.FromJson<QuestInstance>(JsonUtility.ToJson(frozen));
                Require(restored.definitionVersion == "prototype-1" && restored.objectives.Single(c => c.type == ObjectiveType.KillElite).count == 4, "frozen contract preserved");
                Require(q.objectives.Single(c => c.type == ObjectiveType.KillElite).count == expected[q.eventId], "copy isolation");
                lines.Add("PASS T5 combination/pool/frozen JSON: " + q.eventId);
            }
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Evidence/QuestBalance"));
            Directory.CreateDirectory(output);
            File.WriteAllLines(Path.Combine(output, "balance-2-audit.txt"), lines);
            return "PASS 15 assets, 8 elite boundaries, 3 T5 combinations, exact chest quality, frozen JSON";
        }
        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
