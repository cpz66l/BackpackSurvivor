namespace BS.Inventory
{
    public class AdjacencyRule
    {
        public ItemTag TagA { get; }
        public ConnectableSides SideA { get; }
        public ItemTag TagB { get; }
        public ConnectableSides SideB { get; }
        public AdjacencyEffectId EffectId { get; }

        public AdjacencyRule(
            ItemTag tagA,
            ConnectableSides sideA,
            ItemTag tagB,
            ConnectableSides sideB,
            AdjacencyEffectId effectId)
        {
            TagA = tagA;
            SideA = sideA;
            TagB = tagB;
            SideB = sideB;
            EffectId = effectId;
        }
    }
}