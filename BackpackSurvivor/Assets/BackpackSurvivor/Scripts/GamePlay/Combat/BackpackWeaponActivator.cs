using BS.Inventory;
using System.Collections.Generic;
using UnityEngine;

namespace BS.GamePlay.Combat
{
    //监听背包变化，根据背包物品激活/关闭玩家身边的自动武器对象。
    public class BackpackWeaponActivator : MonoBehaviour
    {
        //嵌套类
        [System.Serializable]
        private class WeaponSlot
        {
            public ItemTag Tag;
            public GameObject WeaponObject;
        }

        [SerializeField] private InventorySystem inventorySystem;
        [SerializeField] private int activeWeaponLimit = 1;
        [SerializeField] private List<WeaponSlot> weaponSlots;

        private readonly HashSet<Item> activeWeaponItems = new HashSet<Item>();

        private void Awake()
        {
            if (inventorySystem == null) //不要无条件Find,没脱再find
                inventorySystem = FindAnyObjectByType<InventorySystem>();
        }
        private void OnDestroy()
        {
            if(inventorySystem == null) return;
            if (inventorySystem.Grid == null) return;
            inventorySystem.Grid.OnChanged -= RefreshActiveWeapons;
        }
        private void Start()
        {
            //涉及跨对象订阅，Awake 顺序不保证，所以订阅放在start()
            inventorySystem.Grid.OnChanged += RefreshActiveWeapons;
            RefreshActiveWeapons();
        }

        private void RefreshActiveWeapons()
        {
            //获取真实双持效果，用于激活双持武器
            List<AdjacencyEffect> effects = inventorySystem.Grid.ScanAdjacency(AdjacencyRuleBook.Rules);
            List<AdjacencyEffect> validDualWieldEffects = AdjacencyEffectResolver.ResolveValidEffects(effects);

            //关闭所有槽位
            DeactivateAllWeapons();

            List<Item> items = inventorySystem.Grid.GetUniqueItems();
            int activatedCount = 0;

            foreach (var item in items)//先遍历到的item就是位置靠前的物品
            {
                if (activatedCount >= activeWeaponLimit) break;

                if (TryActivateItem(item))
                    activatedCount++;
            }

            //激活双持奖励武器，不计入激活武器上限
            ActivateDualWieldWeapons(validDualWieldEffects);
        }

        //关闭所有槽位
        private void DeactivateAllWeapons()
        {
            foreach (WeaponSlot weapon in weaponSlots)
            {
                if (weapon == null) continue;
                if (weapon.WeaponObject == null) continue;

                weapon.WeaponObject.SetActive(false);
            }

            activeWeaponItems.Clear();
        }

        //尝试激活某一个背包物品。
        private bool TryActivateItem(Item item)
        {
            if (item == null) return false;
            if (activeWeaponItems.Contains(item)) return false;

            foreach (WeaponSlot weapon in weaponSlots)
            {
                if (weapon == null) continue;
                if (weapon.WeaponObject == null) continue;
                if (weapon.WeaponObject.activeSelf) continue;
                if (weapon.Tag != item.Tag) continue;

                weapon.WeaponObject.SetActive(true);
                activeWeaponItems.Add(item);
                return true;
            }

            return false;
        }

        //提供一个查询激活武器的接口
        public bool IsWeaponItemActive(Item item)
        {
            if(activeWeaponItems.Contains(item)) return true;
            return false;
        }


        //开始激活双持效果的奖励武器
        private void ActivateDualWieldWeapons(List<AdjacencyEffect> validDualWieldEffects)
        {
            if (validDualWieldEffects == null) return;

            foreach (AdjacencyEffect effect in validDualWieldEffects)
            {
                if (effect == null) continue;
                if (effect.EffectId != AdjacencyEffectId.DualWield) continue;

                bool itemAActive = activeWeaponItems.Contains(effect.ItemA);
                bool itemBActive = activeWeaponItems.Contains(effect.ItemB);

                //确保两个激活双持效果的武器都是激活武器

                if (itemAActive && !itemBActive)//A已经是激活武器，激活B
                    TryActivateItem(effect.ItemB);
                else if (itemBActive && !itemAActive)//B已经是激活武器，激活A
                    TryActivateItem(effect.ItemA);
            }
            //注意这里不处理两个都没激活的情况。
            //为什么？因为 DualWield 是“被激活武器的邻接奖励”，不是免费从背包里随便拉出一组武器。
            //它必须依附基础激活位，否则玩家可以把两把手枪放在背包右下角，也绕过左上优先级，
            //这会破坏“背包位置决定优先级”的规则。
        }
    }
}
