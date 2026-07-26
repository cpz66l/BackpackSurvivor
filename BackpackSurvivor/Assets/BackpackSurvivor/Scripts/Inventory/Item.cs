namespace BS.Inventory
{
    public class Item
    {
        public string Id { get;}
        public Rarity Rarity { get;}
        public bool Rotated { get; private set; }

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
        }

        public void Rotate() => Rotated = !Rotated;
    }
}
