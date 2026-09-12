using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BS.GamePlay.Loot
{
    // A run owns an immutable allow-list. Missing configuration never grants access.
    public sealed class LootContext
    {
        private readonly HashSet<string> allowedIds;
        public bool AllowQuestOnly { get; }
        public IReadOnlyCollection<string> AllowedQuestOnlyItemIds { get; }
        public static LootContext Normal { get; } = new LootContext(false);

        public LootContext(bool allowQuestOnly, IReadOnlyCollection<string> allowedQuestOnlyItemIds = null)
        {
            AllowQuestOnly = allowQuestOnly;
            allowedIds = new HashSet<string>(StringComparer.Ordinal);
            if (allowQuestOnly && allowedQuestOnlyItemIds != null)
                foreach (string id in allowedQuestOnlyItemIds)
                    if (!string.IsNullOrWhiteSpace(id)) allowedIds.Add(id);
            AllowedQuestOnlyItemIds = new ReadOnlyCollection<string>(new List<string>(allowedIds));
        }

        public bool Allows(string id, bool questOnly) =>
            !questOnly || (AllowQuestOnly && id != null && allowedIds.Contains(id));
    }
}
