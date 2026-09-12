using System.Collections.Generic;
using System.Linq;
using BS.Quest;
using BS.Data;
using UnityEngine;

namespace BS.GamePlay.Quest
{
    [CreateAssetMenu(fileName="QuestDatabase", menuName="BackpackSurvivor/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        public List<QuestEventDefinition> events = new List<QuestEventDefinition>();
        public LootTableData questOnlyPool;
        public List<string> AllQuestOnlyIds => questOnlyPool == null || questOnlyPool.entries == null
            ? new List<string>() : questOnlyPool.entries.Where(e => e != null && e.questOnly).Select(e => e.id).Distinct().ToList();
        public List<QuestCandidate> Candidates => events == null ? new List<QuestCandidate>() : events.Where(e => e != null).Select(e => e.ToCandidate()).ToList();
        public QuestCandidate Draw(int tier, ISet<string> completed, ISet<int> recentTags, int seed, ICollection<string> recentEvents = null) => QuestDrawer.Pick(Candidates, tier, completed, recentTags, seed, recentEvents);
    }
}
