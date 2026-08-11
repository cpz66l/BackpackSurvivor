using BS.Inventory;
using System.Collections.Generic;
using UnityEngine;
namespace BS.GamePlay.Combat
{
    public class WeaponItemStatResolver : MonoBehaviour
    {
        [System.Serializable]
        private class WeaponStat
        {
            public Rarity rarity;
            public float damageMultiplier = 1f;
        }

        [SerializeField] private List<WeaponStat> weaponStats;
        [SerializeField] private float levelDamageMultiplier = 0.25f; // 每级增加的伤害倍数

        public float GetDamageMultiplier(Item item)
        {
            if (item == null || weaponStats == null)
                return 1f;
            foreach (var stat in weaponStats)
            {
                if (stat.rarity == item.Rarity)
                {
                    float damageMultiplier = stat.damageMultiplier * (1 + (item.Level - 1) * levelDamageMultiplier);
                    return damageMultiplier;
                }
            }
            return 1f; // 默认倍数
        }
    }
}
