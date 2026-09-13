using System;
using System.Collections.Generic;

namespace BS.Quest
{
    [Serializable]
    public sealed class RunMemoryRecord
    {
        public string recordId;
        public string operatorId;
        public int runNumber;
        public string contractId;
        public string contractTitle;
        public string definitionVersion;
        public int tier;
        public RunOutcome outcome;
        public List<MemoryObjectiveResult> objectives = new List<MemoryObjectiveResult>();
        public List<ItemRecord> verifiedItems = new List<ItemRecord>();
        public List<VerifiedRunEvent> verifiedEvents = new List<VerifiedRunEvent>();
        public List<ItemRecord> lostOrUnrecoveredItems = new List<ItemRecord>();
        public List<string> campaignChanges = new List<string>();
        public string sourceVersion;

        public RunMemoryRecord Copy()
        {
            var copy=(RunMemoryRecord)MemberwiseClone();
            copy.objectives=objectives==null?new List<MemoryObjectiveResult>():new List<MemoryObjectiveResult>(objectives.ConvertAll(x=>x?.Copy()));
            copy.verifiedItems=verifiedItems==null?new List<ItemRecord>():new List<ItemRecord>(verifiedItems);
            copy.verifiedEvents=verifiedEvents==null?new List<VerifiedRunEvent>():new List<VerifiedRunEvent>(verifiedEvents.ConvertAll(x=>x?.Copy()));
            copy.lostOrUnrecoveredItems=lostOrUnrecoveredItems==null?new List<ItemRecord>():new List<ItemRecord>(lostOrUnrecoveredItems);
            copy.campaignChanges=campaignChanges==null?new List<string>():new List<string>(campaignChanges);
            return copy;
        }
    }

    [Serializable]
    public sealed class MemoryObjectiveResult
    {
        public int type;
        public bool optional;
        public bool satisfied;
        public float progress01;

        public MemoryObjectiveResult Copy() => (MemoryObjectiveResult)MemberwiseClone();
    }

    [Serializable]
    public sealed class VerifiedRunEvent
    {
        public string eventId;
        public int order;
        public string type;
        public string relatedItemId;
        public int objectiveIndex = -1;
        public int stage = -1;
        public string source;

        public VerifiedRunEvent Copy() => (VerifiedRunEvent)MemberwiseClone();
    }
}
