
namespace BS.GamePlay.Inventory
{
    public class BackpackGlobalModifier
    {
        public int ActiveWeaponLimitBonus { get; private set; }
        public float DamageReductionBonus { get; private set; }
        public float PickupRangeBonus { get; private set; }

        public bool HasAnyBonus => ActiveWeaponLimitBonus > 0 || DamageReductionBonus > 0f || PickupRangeBonus > 0f;

        public void AddActiveWeaponLimitBonus(int value)
        {
            if (value <= 0) return;

            ActiveWeaponLimitBonus += value;
        }

        public void AddDamageReductionBonus(float value)
        {
            if (value <= 0f) return;

            DamageReductionBonus += value;
        }

        public void AddPickupRangeBonus(float value)
        {
            if (value <= 0f) return;

            PickupRangeBonus += value;
        }

        public void Clear()
        {
            ActiveWeaponLimitBonus = 0;
            DamageReductionBonus = 0f;
            PickupRangeBonus = 0f;
        }
    }
}
