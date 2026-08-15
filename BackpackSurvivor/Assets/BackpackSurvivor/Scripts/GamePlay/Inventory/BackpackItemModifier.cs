using BS.Inventory;

namespace BS.GamePlay.Inventory
{
    public class BackpackItemModifier
    {
        public Item Item { get; }
        public float FireRateBonus { get; private set; }
        public float DamageBonus { get; private set; }

        public bool HasAnyBonus => FireRateBonus > 0f || DamageBonus > 0f;

        public BackpackItemModifier(Item item)
        {
            Item = item;
        }

        public void AddFireRateBonus(float value)
        {
            if (value <= 0f) return;

            FireRateBonus += value;
        }

        public void AddDamageBonus(float value)
        {
            if (value <= 0f) return;

            DamageBonus += value;
        }
    }
}