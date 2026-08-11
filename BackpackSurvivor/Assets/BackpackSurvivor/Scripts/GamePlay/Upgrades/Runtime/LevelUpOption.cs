namespace BS.GamePlay.Upgrades
{
    public class LevelUpOption
    {

        private LevelUpOptionId id;
        private string title;
        private string description;
        private float value;
        private LevelUpOptionCategory category;

        public LevelUpOptionId Id => id;
        public string Title => title;
        public string Description => description;
        public float Value => value;
        public LevelUpOptionCategory Category => category;

        public LevelUpOption(LevelUpOptionDefinition definition)
        {
            id = definition.Id;
            title = definition.Title;
            description = definition.Description;
            value = definition.Value;
            category = definition.Category;
        }
    }
}
