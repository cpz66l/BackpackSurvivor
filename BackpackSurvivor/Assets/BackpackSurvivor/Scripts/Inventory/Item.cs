namespace BS.Inventory
{
    public class Item
    {
        public string Id { get;}
        public Rarity Rarity { get;}
        public int Level { get; private set; }
        public int MaxLevel { get; private set; }
        public ItemTag Tag { get;}
        public ConnectableSides LocalConnectableSides { get; }
        public Rotation RotationState { get; private set; }
        public int BaseWidth => baseWidth;
        public int BaseHeight => baseHeight;

        private readonly int baseWidth;    // 原始朝向的尺寸，存一份不动
        private readonly int baseHeight;
        private readonly int baseScoreValue;
        private readonly float baseEffectValue;

        // 宽高变成"按需换算"：标志说了算
        public int Width
        {
            get
            {
                if(RotationState == Rotation.None || RotationState == Rotation.Clockwise180)
                    return baseWidth;
                else
                    return baseHeight;
            }
        }
        public int Height
        {
            get
            {
                if (RotationState == Rotation.None || RotationState == Rotation.Clockwise180)
                    return baseHeight;
                else
                    return baseWidth;
            }
        }
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
            RotationState = Rotation.None;
            Level = 1;
            MaxLevel = 3;
            Tag = itemTag;
            LocalConnectableSides = connectableSides;
            baseScoreValue = scoreValue;
            baseEffectValue = effectValue;
        }


        public void Rotate()
        { 
            if(RotationState == Rotation.Clockwise270)
                RotationState = Rotation.None;
            else
                RotationState++;
        }
        public void IncreaseLevel() => Level++;

        public ConnectableSides GetWorldConnectableSides()
        {
            return GetWorldSides(LocalConnectableSides);
        }
        //把本地方向按当前 RotationState 转成世界方向
        public ConnectableSides GetWorldSides(ConnectableSides localSides)
        {
            ConnectableSides worldSides = ConnectableSides.None;
            if (RotationState == Rotation.None)
                worldSides = localSides;
            else if (RotationState == Rotation.Clockwise90)
            {
                if ((localSides & ConnectableSides.Up) != 0)
                    worldSides |= ConnectableSides.Right;
                if ((localSides & ConnectableSides.Right) != 0)
                    worldSides |= ConnectableSides.Down;
                if ((localSides & ConnectableSides.Down) != 0)
                    worldSides |= ConnectableSides.Left;
                if ((localSides & ConnectableSides.Left) != 0)
                    worldSides |= ConnectableSides.Up;
            }
            else if (RotationState == Rotation.Clockwise180)
            {
                if ((localSides & ConnectableSides.Up) != 0)
                    worldSides |= ConnectableSides.Down;
                if ((localSides & ConnectableSides.Right) != 0)
                    worldSides |= ConnectableSides.Left;
                if ((localSides & ConnectableSides.Down) != 0)
                    worldSides |= ConnectableSides.Up;
                if ((localSides & ConnectableSides.Left) != 0)
                    worldSides |= ConnectableSides.Right;
            }
            else if (RotationState == Rotation.Clockwise270)
            {
                if ((localSides & ConnectableSides.Up) != 0)
                    worldSides |= ConnectableSides.Left;
                if ((localSides & ConnectableSides.Right) != 0)
                    worldSides |= ConnectableSides.Up;
                if ((localSides & ConnectableSides.Down) != 0)
                    worldSides |= ConnectableSides.Right;
                if ((localSides & ConnectableSides.Left) != 0)
                    worldSides |= ConnectableSides.Down;
            }

            return worldSides;
        }

        private float GetLevelEffectMultiplier()
        {
            if (Level == 1) return 1f;
            if (Level == 2) return 1.5f;
            if (Level == 3) return 2f;
            return 1f;
        }

    }

    public enum Rotation
    {
        None,
        Clockwise90,
        Clockwise180,
        Clockwise270
    }
}
