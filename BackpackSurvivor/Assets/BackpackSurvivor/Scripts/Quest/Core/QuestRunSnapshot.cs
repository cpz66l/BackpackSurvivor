using System;
using System.Collections.Generic;
using BS.Inventory;

namespace BS.Quest
{
    public enum RunOutcome { Survived, Died }
    // Never reorder these persisted quality indices. Unknown is not a quality bucket.
    public enum ChestQuality { Unknown = -1, Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4 }

    [Serializable]
    public struct ItemRecord
    {
        public string id;
        public Rarity rarity;
        public ItemTag tag;
        public int level;
        public int scoreValue;
        public bool questOnly;
    }

    [Serializable]
    public sealed class QuestRunSnapshot
    {
        public RunOutcome outcome;
        public float elapsed;
        public int level;
        public int kills;
        public int eliteKills;
        public int chestsOpened;
        public int[] chestsOpenedByQuality = new int[5];
        public int unknownQualityChestsOpened;
        public int backpackValue;
        public int gold;
        public List<ItemRecord> items = new List<ItemRecord>();

        // Detached copies prevent event consumers from mutating the stored settlement.
        public QuestRunSnapshot Copy()
        {
            var copy = (QuestRunSnapshot)MemberwiseClone();
            copy.chestsOpenedByQuality = (int[])chestsOpenedByQuality.Clone();
            copy.items = new List<ItemRecord>(items);
            return copy;
        }
    }
}
