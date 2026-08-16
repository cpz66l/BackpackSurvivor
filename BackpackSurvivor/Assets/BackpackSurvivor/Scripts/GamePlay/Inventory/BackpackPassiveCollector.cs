using BS.Inventory;
using System.Collections.Generic;

namespace BS.GamePlay.Inventory
{
    public class BackpackPassiveCollector
    {
        private readonly BackpackGlobalModifier globalModifier = new BackpackGlobalModifier();

        public BackpackGlobalModifier Collect(List<Item> items)
        {
            globalModifier.Clear();
            if (items == null) return globalModifier;

            bool mechanicalArmApplied = false;
            foreach (Item item in items)
            {
                if (item == null) continue;
                if (item.Tag == ItemTag.MechanicalArm && !mechanicalArmApplied)
                {
                    globalModifier.AddActiveWeaponLimitBonus(1);
                    mechanicalArmApplied = true;
                    continue;
                }

                if (item.Tag == ItemTag.Armor)
                {
                    globalModifier.AddDamageReductionBonus(item.EffectValue);
                    continue;
                }

                if (item.Tag == ItemTag.MagnetCore)
                {
                    globalModifier.AddPickupRangeBonus(item.EffectValue);
                }
            }
            return globalModifier;
        }
    }
}
