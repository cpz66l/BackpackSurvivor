namespace BS.GamePlay.Upgrades
{
    public class LevelUpOptionDefinition
    {
        public LevelUpOptionId Id { get; }
        public LevelUpOptionCategory Category { get; }
        public string Title { get; }
        public string Description { get; }
        public float Value { get; }
        public int Weight { get; }
        public int MinLevel { get; }
        public int MaxPickCount { get; }

        public LevelUpOptionDefinition(
            LevelUpOptionId id,
            LevelUpOptionCategory category,
            string title,
            string description,
            float value,
            int weight,
            int minLevel,
            int maxPickCount)
        {
            Id = id;
            Category = category;
            Title = title;
            Description = description;
            Value = value;
            Weight = weight;
            MinLevel = minLevel;
            MaxPickCount = maxPickCount;
        }
    }
}
