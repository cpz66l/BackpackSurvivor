using System;
using System.Collections.Generic;

namespace BS.Quest
{
    public static class QuestDrawer
    {
        public static QuestCandidate Pick(IList<QuestCandidate> candidates, int tier, ISet<string> completed, ISet<int> recentTags, int seed)
        {
            if (candidates == null) return null;
            var available = new List<QuestCandidate>();
            foreach (var c in candidates)
            {
                if (c == null || c.tier != tier || string.IsNullOrEmpty(c.eventId) || c.baseWeight <= 0) continue;
                if (completed != null && completed.Contains(c.eventId)) continue;
                bool unlocked = true;
                if (c.unlockAfter != null) foreach (var id in c.unlockAfter) if (completed == null || !completed.Contains(id)) { unlocked = false; break; }
                if (unlocked) available.Add(c);
            }
            if (available.Count == 0) return null;
            var rng = new Random(seed);
            float total = 0;
            foreach (var c in available) total += c.baseWeight * (recentTags != null && recentTags.Contains(c.tag) ? .25f : 1f);
            float roll = (float)rng.NextDouble() * total;
            foreach (var c in available) { roll -= c.baseWeight * (recentTags != null && recentTags.Contains(c.tag) ? .25f : 1f); if (roll < 0) return c; }
            return available[available.Count - 1];
        }

        public static QuestInstance Accept(QuestCandidate c, int seed, IList<string> allQuestOnlyIds)
        {
            if (c == null) return null;
            var q = new QuestInstance { eventId=c.eventId, tier=c.tier, seed=seed, definitionVersion=c.definitionVersion,
                briefingTitle=c.briefingTitle, briefingBody=c.briefingBody, briefingHint=c.briefingHint, briefingSource=c.briefingSource,
                objectives = c.objectives == null ? new List<ObjectiveClause>() : new List<ObjectiveClause>(c.objectives) };
            if (allQuestOnlyIds != null) q.activeQuestOnlyItemIds = new List<string>(allQuestOnlyIds);
            return q;
        }
    }
}
