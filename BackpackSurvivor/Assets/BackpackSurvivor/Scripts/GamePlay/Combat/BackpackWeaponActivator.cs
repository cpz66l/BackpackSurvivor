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
            foreach (var weapon in weaponSlots)
            {
                if(weapon == null) continue;
                if (weapon.WeaponObject == null) continue;
                weapon.WeaponObject.SetActive(false);
            }
            activeWeaponItems.Clear();//清空激活武器
            List<Item> items = inventorySystem.Grid.GetUniqueItems();
            int activatedCount = 0;

            foreach (var item in items)//先遍历到的item就是位置靠前的物品
            {
                foreach(var weapon in weaponSlots)
                {
                    if(weapon == null) continue ;
                    if (activatedCount >= activeWeaponLimit) break;
                    if (item.Tag != weapon.Tag) continue;
                    if (weapon.WeaponObject == null) continue;
                    weapon.WeaponObject.SetActive(true);
                    activatedCount++;
                    activeWeaponItems.Add(item);
                    break;//激活一个武器后要跳出内层循环，不然会出现一个item,激活两个weapon
                }
            }
        }

        //提供一个查询激活武器的接口
        public bool IsWeaponItemActive(Item item)
        {
            if(activeWeaponItems.Contains(item)) return true;
            return false;
        }

    }
}
