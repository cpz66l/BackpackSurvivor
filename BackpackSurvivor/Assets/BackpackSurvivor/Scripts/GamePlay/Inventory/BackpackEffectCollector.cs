using BS.Inventory;
using System.Collections.Generic;

namespace BS.GamePlay.Inventory
{
    public class BackpackEffectCollector
    {
        private readonly Dictionary<Item, BackpackItemModifier> modifiersByItem = new Dictionary<Item, BackpackItemModifier>();

        public void Collect(List<AdjacencyEffect> validEffects)
        {
            Clear();

            if (validEffects == null) return;

            foreach (AdjacencyEffect effect in validEffects)
            {
                if (effect == null) continue;

                switch (effect.EffectId)
                {
                    case AdjacencyEffectId.FireRateBoost:
                        ApplyFireRateBoost(effect);
                        break;

                    case AdjacencyEffectId.DamageBoost:
                        ApplyDamageBoost(effect);
                        break;
                    case AdjacencyEffectId.CritBoost:
                        ApplyCritBoost(effect);
                        break;
                }
            }
        }

        public bool TryGetModifier(Item item, out BackpackItemModifier modifier)
        {
            if (item == null)
            {
                modifier = null;
                return false;
            }

            return modifiersByItem.TryGetValue(item, out modifier);//哈希表，通过输入物品，获得物品修饰后效果
        }

        public void Clear()
        {
            modifiersByItem.Clear();
        }

        private BackpackItemModifier GetOrCreateModifier(Item item)
        {
            if (item == null) return null;

            if (!modifiersByItem.TryGetValue(item, out BackpackItemModifier modifier))
            {
                modifier = new BackpackItemModifier(item);
                modifiersByItem[item] = modifier;
            }

            return modifier;
        }

        private void ApplyFireRateBoost(AdjacencyEffect effect)
        {
            ApplyWeaponSideBonus(effect, BonusType.FireRate);
        }

        private void ApplyDamageBoost(AdjacencyEffect effect)
        {
            ApplyWeaponSideBonus(effect, BonusType.Damage);
        }

        private void ApplyCritBoost(AdjacencyEffect effect)
        {
            ApplyWeaponSideBonus(effect, BonusType.Crit);
        }

        private void ApplyWeaponSideBonus(AdjacencyEffect effect, BonusType bonusType)
        {
            if (effect == null) return;
            //区别武器与插件
            bool itemAIsWeapon = IsWeapon(effect.ItemA);
            bool itemBIsWeapon = IsWeapon(effect.ItemB);

            if (itemAIsWeapon && !itemBIsWeapon)
            {
                AddBonus(effect.ItemA, effect.ItemB, bonusType);
                return;
            }

            if (itemBIsWeapon && !itemAIsWeapon)
            {
                AddBonus(effect.ItemB, effect.ItemA, bonusType);
                return;
            }
        }

        private void AddBonus(Item targetItem, Item sourceItem, BonusType bonusType)
        {
            if (targetItem == null) return;
            if (sourceItem == null) return;

            BackpackItemModifier modifier = GetOrCreateModifier(targetItem);
            if (modifier == null) return;

            switch (bonusType)
            {
                case BonusType.FireRate:
                    modifier.AddFireRateBonus(sourceItem.EffectValue);
                    break;

                case BonusType.Damage:
                    modifier.AddDamageBonus(sourceItem.EffectValue);
                    break;
                case BonusType.Crit:
                    modifier.AddCritChanceBonus(sourceItem.EffectValue);
                    break;
            }
        }

        //判断是否是武器
        private bool IsWeapon(Item item)
        {
            if (item == null) return false;

            return item.Tag == ItemTag.Pistol
                || item.Tag == ItemTag.Rifle
                || item.Tag == ItemTag.Shotgun;
        }

        private enum BonusType
        {
            FireRate,
            Damage,
            Crit,
        }
    }
}