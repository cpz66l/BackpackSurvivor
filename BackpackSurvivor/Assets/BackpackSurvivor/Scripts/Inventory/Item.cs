namespace BS.Inventory
{
    public class Item
    {
        public string Id { get;}
        public Rarity Rarity { get;}
        public int Width { get;}
        public int Height { get;}


        public Item(string id, Rarity rarity, int width, int height)
        {
            Id = id;
            Width = width;
            Height = height;
            Rarity = rarity;
        }
    }
}
