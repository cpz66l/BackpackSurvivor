using System.Collections.Generic;
using System.Linq;

namespace BS.GamePlay.Loot
{
    public sealed class LootContext
    {
        public bool AllowQuestOnly { get; }
        public IReadOnlyCollection<string> AllowedQuestOnlyItemIds { get; }
        public LootContext(bool allowQuestOnly, IReadOnlyCollection<string> allowedQuestOnlyItemIds = null)
        { AllowQuestOnly = allowQuestOnly; AllowedQuestOnlyItemIds = allowedQuestOnlyItemIds; }
        public bool Allows(string id, bool questOnly)
        { return !questOnly || (AllowQuestOnly && (AllowedQuestOnlyItemIds == null || AllowedQuestOnlyItemIds.Count == 0 || AllowedQuestOnlyItemIds.Contains(id))); }
        public static LootContext Normal => new LootContext(false);
    }
}
