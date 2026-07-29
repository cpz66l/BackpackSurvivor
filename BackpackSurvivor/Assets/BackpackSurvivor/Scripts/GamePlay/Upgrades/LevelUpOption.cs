namespace BS.GamePlay.Upgrades
{
    public class LevelUpOption
    {

        private LevelUpOptionId id;
        private string title;
        private string description;
        private float value;

        public LevelUpOptionId Id => id;
        public string Title => title;
        public string Description => description;
        public float Value => value;

        public LevelUpOption(LevelUpOptionId id, string title, string description, float value)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.value = value;
        }
    }
}
