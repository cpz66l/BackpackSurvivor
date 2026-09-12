using System;
using System.Collections.Generic;
using BS.Inventory;

namespace BS.Quest
{
    public enum ObjectiveType { CarryTag, CarryTagSet, CarryRarity, CarryItem, CarryItemAtLevel, CarryAnyItemAtLevel, CarryItemAnyOf, BackpackValueAtLeast, BackpackValueAtMost, ExcludeTag, ExcludeRarity, KillTotal, KillElite, OpenChestAtLeast, ReachLevel, SurviveToSecond }

    [Serializable]
    public sealed class ObjectiveClause
    {
        public ObjectiveType type;
        public ItemTag tag;
        public List<ItemTag> tags = new List<ItemTag>();
        public string itemId;
        public List<string> itemIds = new List<string>();
        public Rarity rarity;
        public int itemLevel = 1;
        public int count = 1;
        public int minValue, maxValue;
        public ChestQuality minChestQuality = ChestQuality.Unknown;
        public bool optional;
    }

    [Serializable]
    public sealed class QuestInstance
    {
        public string eventId;
        public int tier;
        public int seed;
        public string definitionVersion;
        public List<ObjectiveClause> objectives = new List<ObjectiveClause>();
        public List<string> activeQuestOnlyItemIds = new List<string>();
        public string briefingTitle;
        public string briefingBody;
        public string briefingHint;
        public string briefingSource;
        public bool isFinal;
    }

    public sealed class QuestCandidate
    {
        public string eventId;
        public int tier;
        public int seed;
        public string definitionVersion;
        public int tag;
        public float baseWeight = 1f;
        public string[] unlockAfter;
        public bool isFinal;
        public List<ObjectiveClause> objectives = new List<ObjectiveClause>();
        public string briefingTitle, briefingBody, briefingHint, briefingSource;
    }

    [Serializable]
    public sealed class ClauseResult
    {
        public bool satisfied;
        public bool optional;
        public float progress01;
        public ObjectiveType type;
    }

    [Serializable]
    public sealed class QuestOutcome
    {
        public bool Completed;
        public List<ClauseResult> Details = new List<ClauseResult>();
        public float Progress01;
        public bool ConditionsSatisfiedBeforeDeath;
    }
}
