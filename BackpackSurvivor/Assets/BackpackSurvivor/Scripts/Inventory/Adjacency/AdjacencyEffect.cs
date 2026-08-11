namespace BS.Inventory
{
    public class AdjacencyEffect
    {
        public AdjacencyEffectId EffectId { get; }
        public Item ItemA { get; }
        public ConnectableSides SideA { get; }
        public Item ItemB { get; }
        public ConnectableSides SideB { get; }

        public AdjacencyEffect(
            AdjacencyEffectId effectId,
            Item itemA,
            ConnectableSides sideA,
            Item itemB,
            ConnectableSides sideB)
        {
            //构造赋值
            EffectId = effectId;
            ItemA = itemA;
            SideA = sideA;
            ItemB = itemB;
            SideB = sideB;

        }

        public bool Involves(Item item)
        {
            // 返回 item 是否是 ItemA 或 ItemB
            if(item == null) return false;
            if(item == ItemA || item == ItemB)
                return true;
            return false;
        }
    }
}
