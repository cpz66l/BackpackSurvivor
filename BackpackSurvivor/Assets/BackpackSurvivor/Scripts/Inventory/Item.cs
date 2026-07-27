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

        // 宽高变成"按需换算"：标志说了算
        public int Width => Rotated ? baseHeight : baseWidth;
        public int Height => Rotated ? baseWidth : baseHeight;

        public Item(string id, Rarity rarity, int width, int height)
        {
            Id = id;
            baseWidth = width;
            baseHeight = height;
            Rarity = rarity;
            Rotated = false;
            Level = 1;
            MaxLevel = 3;
            Tag = ItemTag.None;
            LocalConnectableSides = ConnectableSides.None;
        }

        //新构造函数
        public Item(string id,
            Rarity rarity, int width, int height 
            , ItemTag itemTag ,
            ConnectableSides connectableSides)
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
        }


        public void Rotate() => Rotated = !Rotated;
        public void IncreaseLevel() => Level++;

        public ConnectableSides GetWorldConnectableSides()
        {
            return LocalConnectableSides;
        }

    }
}
