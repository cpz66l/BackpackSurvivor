namespace BS.GamePlay.Run
{
    public enum GameState
    {
        NotStarted,    //避免场景刚加载时系统误以为已经开局。
        Running,
        LevelUpSelecting,
        Paused,
        Victory,
        Defeat
    }
}