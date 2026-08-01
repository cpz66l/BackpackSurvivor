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

        //激活Item与自动武器的映射表，用于把邻接效果应用到具体自动武器
        private readonly Dictionary<Item, AutoWeapon> activeWeaponsByItem = new Dictionary<Item, AutoWeapon>();
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
            List<AdjacencyEffect> validEffects = AdjacencyEffectResolver.ResolveValidEffects(effects);

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
            ActivateDualWieldWeapons(validEffects);
            ActivateFireRateBoost(validEffects);
        }

        //关闭所有槽位
        private void DeactivateAllWeapons()
        {
            foreach (WeaponSlot weapon in weaponSlots)
            {
                if (weapon == null) continue;
                if (weapon.WeaponObject == null) continue;

                //刷新背包时所有武器都会先关闭，再出现判定激活，旧邻接效果不能残留到下一次布局。
                weapon.WeaponObject.GetComponent<AutoWeapon>()?.SetBackpackFireRateMultiplier(1f);//重置背包火力倍率
                weapon.WeaponObject.SetActive(false);
            }

            activeWeaponItems.Clear(); //存激活Item的集合
            activeWeaponsByItem.Clear();//存激活Item与自动武器的映射表
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
                AutoWeapon autoWeapon = weapon.WeaponObject.GetComponent<AutoWeapon>();
                if (autoWeapon != null)
                {
                    activeWeaponsByItem[item] = autoWeapon;
                }
                return true;
            }

            return false;
        }

        //尝试获取激活的自动武器，用于把邻接效果应用到具体自动武器
        private bool TryGetActiveAutoWeapon(Item item, out AutoWeapon autoWeapon)
        {
            return activeWeaponsByItem.TryGetValue(item, out autoWeapon);
        }

        //提供一个查询激活武器的接口
        public bool IsWeaponItemActive(Item item)
        {
            if(activeWeaponItems.Contains(item)) return true;
            return false;
        }


        //开始激活双持效果的奖励武器
        private void ActivateDualWieldWeapons(List<AdjacencyEffect> validEffects)
        {
            if (validEffects == null) return;

            foreach (AdjacencyEffect effect in validEffects)
            {
                if (effect == null) continue;
                if (effect.EffectId != AdjacencyEffectId.DualWield) continue; //筛选双持效果

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

        //开始激活攻速加成效果
        private void ActivateFireRateBoost(List<AdjacencyEffect> validEffects)
        {
            if (validEffects == null) return;

            //给激活武器计算攻速加成叠加效果
            Dictionary<AutoWeapon, float> fireRateBonusByWeapon = new Dictionary<AutoWeapon, float>();

            foreach (AdjacencyEffect effect in validEffects)
            {
                if (effect == null) continue;
                if (effect.EffectId != AdjacencyEffectId.FireRateBoost) continue; //筛选攻速效果

                if (TryGetActiveAutoWeapon(effect.ItemA, out AutoWeapon autoWeaponA))
                {
                    float effectValue = effect.ItemB.EffectValue;
                    if (fireRateBonusByWeapon.ContainsKey(autoWeaponA))
                        fireRateBonusByWeapon[autoWeaponA] +=effectValue;
                    else fireRateBonusByWeapon[autoWeaponA] = effectValue;
                }
                else if (TryGetActiveAutoWeapon(effect.ItemB, out AutoWeapon autoWeaponB))
                {
                    float effectValue = effect.ItemA.EffectValue;
                    if (fireRateBonusByWeapon.ContainsKey(autoWeaponB))
                        fireRateBonusByWeapon[autoWeaponB] += effectValue;
                    else fireRateBonusByWeapon[autoWeaponB] = effectValue;
                }
            }

            //给每个激活且有攻速效果的武器计算攻速倍率，并应用到武器上
            foreach (var kvp in fireRateBonusByWeapon)
            {
                AutoWeapon autoWeapon = kvp.Key;
                float effectValues = kvp.Value;

                float fireRateMultiplier = 1f + effectValues;
                fireRateMultiplier = Mathf.Min(fireRateMultiplier, 1.75f);
                autoWeapon.SetBackpackFireRateMultiplier(fireRateMultiplier);
            }
        }
    }
}
