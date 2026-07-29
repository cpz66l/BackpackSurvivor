namespace BS.GamePlay.Run
{
    public class LevelProgress 
    {
        private readonly int baseXpToNextLevel;
        private readonly int xpGrowthPerLevel;

        private int level;
        private int currentXp;
        private int totalXp;

        public int Level => level;
        public int CurrentXp => currentXp;
        public int TotalXp => totalXp;
        public int XpToNextLevel => baseXpToNextLevel + (level-1)*xpGrowthPerLevel;

        public LevelProgress(int baseXpToNextLevel, int xpGrowthPerLevel)
        {
            //构造经验成长配置
            if (baseXpToNextLevel <= 0)
                baseXpToNextLevel = 1;
            if (xpGrowthPerLevel < 0)
                xpGrowthPerLevel = 0;
            this.xpGrowthPerLevel = xpGrowthPerLevel;
            this.baseXpToNextLevel = baseXpToNextLevel;
            Reset();
        }

        public int AddXp(int amount)
        {
            if (amount <=0) return 0;
            int levelUpCount = 0;
            currentXp += amount;
            totalXp += amount;
            while(currentXp >= XpToNextLevel)
            {
                currentXp -= XpToNextLevel;
                levelUpCount++;
                level++;
            }
            return levelUpCount;
        }
        public void Reset()
        {
            level = 1;
            currentXp = 0;
            totalXp = 0;
        }
    }
}