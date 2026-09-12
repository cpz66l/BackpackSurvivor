using System.Collections.Generic;
using System.Linq;
using BS.Quest;
using UnityEngine;

namespace BS.GamePlay.Quest
{
    [CreateAssetMenu(fileName="QuestDatabase", menuName="BackpackSurvivor/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        public List<QuestEventDefinition> events = new List<QuestEventDefinition>();
        public List<QuestCandidate> Candidates => events == null ? new List<QuestCandidate>() : events.Where(e => e != null).Select(e => e.ToCandidate()).ToList();
        public QuestCandidate Draw(int tier, ISet<string> completed, ISet<int> recentTags, int seed) => QuestDrawer.Pick(Candidates, tier, completed, recentTags, seed);
    }
}
