namespace BS.GamePlay.Save
{
    [System.Serializable]
    public class SaveData
    {
        public int totalRuns;   //总局数
        public int totalWins;   //总胜利局数
        public int bestBackpackValue;   //最高背包价值
        public int totalGold;           //总金币数
        public int legendaryFoundCount; //传说物品胜利带出数
        public int legendaryCollectedValue; //传说物品胜利带出总价值
        public string lastPlayedVersion;    //上次游玩版本

        public static SaveData CreateDefault()
        {
            SaveData defaultData = new SaveData();
            defaultData.totalRuns = 0;
            defaultData.totalWins = 0;
            defaultData.bestBackpackValue = 0;
            defaultData.totalGold = 0;
            defaultData.legendaryCollectedValue = 0;
            defaultData.legendaryFoundCount = 0;
            defaultData.lastPlayedVersion = "v0.3.10";
            return defaultData;
        }
    }
}
