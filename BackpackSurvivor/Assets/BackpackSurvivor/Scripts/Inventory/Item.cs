namespace BS.Inventory
{
    public class Item
    {
        public string Id { get;}
        public Rarity Rarity { get;}
        public int Level { get; private set; }
        public int MaxLevel { get; private set; }
        public bool Rotated { get; private set; }
        public ItemTag Tag { get;}
        public ConnectableSides LocalConnectableSides { get; }
       
        private readonly int baseWidth;    // 原始朝向的尺寸，存一份不动
        private readonly int baseHeight;
        private readonly int baseScoreValue;
        private readonly float baseEffectValue;

        // 宽高变成"按需换算"：标志说了算
        public int Width => Rotated ? baseHeight : baseWidth;
        public int Height => Rotated ? baseWidth : baseHeight;
        public int ScoreValue => baseScoreValue * Level; // 分数价值
        public float EffectValue => baseEffectValue * GetLevelEffectMultiplier(); // 战斗效果数值
        public int BaseScoreValue => baseScoreValue; // 基础分数价值
        public float BaseEffectValue => baseEffectValue; // 基础战斗效果数值    

        public Item(string id,
            Rarity rarity, int width, int height 
            , ItemTag itemTag ,
            ConnectableSides connectableSides,
            int scoreValue,float effectValue)
        {
            Id = id;
            baseWidth = width;
            baseHeight = height;
            Rarity = rarity;
            Rotated = false;
            Level = 1;
            MaxLevel = 3;
            Tag = itemTag;
            LocalConnectableSides = connectableSides;
            baseScoreValue = scoreValue;
            baseEffectValue = effectValue;
        }


        public void Rotate() => Rotated = !Rotated;
        public void IncreaseLevel() => Level++;

        public ConnectableSides GetWorldConnectableSides()
        {
            return LocalConnectableSides;
        }

        private float GetLevelEffectMultiplier()
        {
            if (Level == 1) return 1f;
            if (Level == 2) return 1.5f;
            if (Level == 3) return 2f;
            return 1f;
        }

    }
}
