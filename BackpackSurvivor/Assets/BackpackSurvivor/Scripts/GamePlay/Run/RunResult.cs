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

        public RunResult(GameState finalState , float elapsed , int level , int totalXp , int killCount ,int backpackValue)
        {
            FinalState = finalState ;
            Elapsed = elapsed ;
            Level = level ;
            TotalXp = totalXp ;
            KillCount = killCount ;
            BackpackValue = backpackValue;
        } 


    }
}

