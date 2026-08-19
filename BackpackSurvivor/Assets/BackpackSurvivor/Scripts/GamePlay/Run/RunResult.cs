namespace BS.GamePlay.Run
{
    //一个事件传递的数据包
    public class RunResult
    {
        public GameState FinalState { get; } //胜利or死亡
        public float Elapsed { get; }
        public int Level { get; }
        public int TotalXp { get; }
        public int KillCount { get; }
        public int BackpackValue { get; }
        public int TotalGold { get; }
        public int LegendaryFoundCount { get; }
        public int LegendaryCollectedValue { get; }

        public RunResult(GameState finalState ,
            float elapsed ,
            int level ,
            int totalXp ,
            int killCount ,
            int backpackValue,
            int totalGold,
            int legendaryFoundCount,
            int legendaryCollectedValue
            )
        {
            FinalState = finalState ;
            Elapsed = elapsed ;
            Level = level ;
            TotalXp = totalXp ;
            KillCount = killCount ;
            BackpackValue = backpackValue;
            TotalGold = totalGold ;
            LegendaryCollectedValue = legendaryCollectedValue;
            LegendaryFoundCount = legendaryFoundCount ;
        } 


    }
}

