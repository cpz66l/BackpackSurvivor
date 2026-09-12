using System.Collections.Generic;
using BS.Quest;
using UnityEngine;

namespace BS.GamePlay.Quest
{
    [CreateAssetMenu(fileName="QuestEvent", menuName="BackpackSurvivor/Quest Event")]
    public class QuestEventDefinition : ScriptableObject
    {
        public string eventId;
        public int tier = 1;
        public int tag;
        public string definitionVersion = "1";
        public string codename;
        [TextArea] public string briefingSeed;
        [TextArea] public string offlineBriefing;
        public List<ObjectiveClause> objectives = new List<ObjectiveClause>();
        [Range(0f,1f)] public float baseWeight = 1f;
        public string[] unlockAfter;
        public bool isFinal;
        public QuestCandidate ToCandidate(int seed = 0) => new QuestCandidate { eventId=eventId, tier=tier, tag=tag, seed=seed,
            definitionVersion=definitionVersion, baseWeight=baseWeight, unlockAfter=unlockAfter, isFinal=isFinal, objectives=objectives,
            briefingTitle=codename, briefingBody=offlineBriefing, briefingSource="offline" };
    }
}
