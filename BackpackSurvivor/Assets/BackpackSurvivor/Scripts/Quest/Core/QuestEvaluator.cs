using System;
using System.Linq;
using BS.Inventory;

namespace BS.Quest
{
    public static class QuestEvaluator
    {
        public static QuestOutcome Evaluate(QuestInstance quest, QuestRunSnapshot snapshot)
        {
            var result = new QuestOutcome();
            if (quest == null || snapshot == null) return result;
            var objectives = quest.objectives ?? new System.Collections.Generic.List<ObjectiveClause>();
            bool all = true; int satisfied = 0;
            foreach (var clause in objectives)
            {
                bool ok = EvaluateClause(clause, snapshot);
                if (ok) satisfied++; else if (clause != null && !clause.optional) all = false;
                result.Details.Add(new ClauseResult { type = clause == null ? default : clause.type, optional = clause != null && clause.optional, satisfied = ok, progress01 = Progress(clause, snapshot, ok) });
            }
            result.Progress01 = objectives.Count == 0 ? 0f : (float)satisfied / objectives.Count;
            if (objectives.Count == 0) all = false;
            result.ConditionsSatisfiedBeforeDeath = all;
            result.Completed = snapshot.outcome == RunOutcome.Survived && all;
            return result;
        }

        static bool EvaluateClause(ObjectiveClause c, QuestRunSnapshot s)
        {
            if (c == null) return false;
            var items = s.items ?? new System.Collections.Generic.List<ItemRecord>();
            int count = Math.Max(0, c.count);
            switch (c.type)
            {
                case ObjectiveType.CarryTag: return items.Count(i => i.tag == c.tag) >= count;
                case ObjectiveType.CarryTagSet: return items.Count(i => c.tags != null && c.tags.Contains(i.tag)) >= count;
                case ObjectiveType.CarryRarity: return items.Count(i => i.rarity >= c.rarity) >= count;
                case ObjectiveType.CarryItem: return items.Count(i => i.id == c.itemId) >= count;
                case ObjectiveType.CarryItemAtLevel: return items.Count(i => i.id == c.itemId && i.level >= c.itemLevel) >= count;
                case ObjectiveType.CarryAnyItemAtLevel: return items.Count(i => i.level >= c.itemLevel) >= count;
                case ObjectiveType.CarryItemAnyOf: return items.Count(i => c.itemIds != null && c.itemIds.Contains(i.id)) >= count;
                case ObjectiveType.BackpackValueAtLeast: return s.backpackValue >= c.minValue;
                case ObjectiveType.BackpackValueAtMost: return s.backpackValue <= c.maxValue;
                case ObjectiveType.ExcludeTag: return !items.Any(i => i.tag == c.tag);
                case ObjectiveType.ExcludeRarity: return !items.Any(i => i.rarity >= c.rarity);
                case ObjectiveType.KillTotal: return s.kills >= count;
                case ObjectiveType.KillElite: return s.eliteKills >= count;
                case ObjectiveType.OpenChestAtLeast: { int q = (int)c.minChestQuality; return q >= 0 && q < 5 && s.chestsOpenedByQuality != null && s.chestsOpenedByQuality.Length > q && s.chestsOpenedByQuality[q] >= count; }
                case ObjectiveType.ReachLevel: return s.level >= count;
                case ObjectiveType.SurviveToSecond: return s.elapsed >= count;
                default: return false;
            }
        }

        static float Progress(ObjectiveClause c, QuestRunSnapshot s, bool ok)
        {
            if (ok) return 1f;
            if (c == null) return 0f;
            int target = Math.Max(1, c.count); int actual = 0;
            var items = s.items ?? new System.Collections.Generic.List<ItemRecord>();
            switch (c.type) { case ObjectiveType.KillTotal: actual=s.kills; break; case ObjectiveType.KillElite: actual=s.eliteKills; break; case ObjectiveType.ReachLevel: actual=s.level; break; case ObjectiveType.SurviveToSecond: actual=(int)s.elapsed; break; case ObjectiveType.OpenChestAtLeast: int q=(int)c.minChestQuality; if(q>=0&&q<5&&s.chestsOpenedByQuality!=null&&s.chestsOpenedByQuality.Length>q) actual=s.chestsOpenedByQuality[q]; break; default: return 0f; }
            return Math.Min(1f, (float)Math.Max(0, actual) / target);
        }
    }
}
