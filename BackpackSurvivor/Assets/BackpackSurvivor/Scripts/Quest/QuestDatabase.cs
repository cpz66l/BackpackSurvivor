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
        public List<LootTableData> itemCatalog = new List<LootTableData>();
        public List<ItemRecord> ItemDefinitions => itemCatalog.Where(t=>t!=null&&t.entries!=null).SelectMany(t=>t.entries)
            .Where(e=>e!=null&&!string.IsNullOrWhiteSpace(e.id)).GroupBy(e=>e.id).Select(g=>g.First())
            .Select(e=>new ItemRecord{id=e.id,tag=e.itemTag,rarity=e.rarity,level=e.level,scoreValue=e.scoreValue,questOnly=e.questOnly}).ToList();
        public List<string> AllQuestOnlyIds => questOnlyPool == null || questOnlyPool.entries == null
            ? new List<string>() : questOnlyPool.entries.Where(e => e != null && e.questOnly).Select(e => e.id).Distinct().ToList();
        public List<QuestCandidate> Candidates => events == null ? new List<QuestCandidate>() : events.Where(e => e != null).Select(e => e.ToCandidate()).ToList();
        public QuestCandidate Draw(int tier, ISet<string> completed, ISet<int> recentTags, int seed, ICollection<string> recentEvents = null) => QuestDrawer.Pick(Candidates, tier, completed, recentTags, seed, recentEvents);
    }
}
