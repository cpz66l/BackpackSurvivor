using BS.Quest;

namespace BS.GamePlay.Run
{
    /// <summary>Current-process context for the short handoff from settlement back to camp.</summary>
    public static class RunSessionContext
    {
        static QuestInstance lastQuest;
        static QuestRunSnapshot lastSnapshot;

        public static QuestInstance LastQuest => lastQuest?.Copy();
        public static QuestRunSnapshot LastSnapshot => lastSnapshot?.Copy();
        public static bool HasSettlement => lastQuest != null && lastSnapshot != null;

        public static void SetSettlement(QuestInstance quest, QuestRunSnapshot snapshot)
        {
            lastQuest = quest?.Copy();
            lastSnapshot = snapshot?.Copy();
        }

        public static void ClearSettlement()
        {
            lastQuest = null;
            lastSnapshot = null;
        }
    }
}
