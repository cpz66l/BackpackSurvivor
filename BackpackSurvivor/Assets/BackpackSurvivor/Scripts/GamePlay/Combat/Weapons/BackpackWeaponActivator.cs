using BS.GamePlay.Stats;
using BS.Inventory;
using System.Collections.Generic;
using UnityEngine;
using BS.GamePlay.Inventory;

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
        [SerializeField] private PlayerRunStats stats;
        [SerializeField] private WeaponItemStatResolver weaponItemStatResolver;
        [SerializeField] private int activeWeaponLimit = 1;
        [SerializeField] private List<WeaponSlot> weaponSlots;
        [SerializeField] private float maxBackpackFireRateMultiplier = 2f;
        [SerializeField] private float maxBackpackDamageBoostMultiplier = 2f;

        //激活Item与自动武器的映射表，用于把邻接效果应用到具体自动武器
        private readonly BackpackEffectCollector effectCollector = new BackpackEffectCollector();//效果控制器
        private readonly HashSet<Item> activeWeaponItems = new HashSet<Item>();

        private void Awake()
        {
            if (inventorySystem == null) //不要无条件Find,没脱再find
                inventorySystem = FindAnyObjectByType<InventorySystem>();
            if(weaponItemStatResolver == null)
                weaponItemStatResolver = FindAnyObjectByType<WeaponItemStatResolver>();
            if(stats == null)
                stats = FindAnyObjectByType<PlayerRunStats>();
        }
        private void OnDestroy()
        {
            if (stats != null)
                stats.OnStatsChanged -= RefreshActiveWeapons;
            if (inventorySystem == null) return;
            if (inventorySystem.Grid == null) return;
            inventorySystem.Grid.OnChanged -= RefreshActiveWeapons;
        }
        private void Start()
        {
            //涉及跨对象订阅，Awake 顺序不保证，所以订阅放在start()
            inventorySystem.Grid.OnChanged += RefreshActiveWeapons;
            if (stats != null)
                stats.OnStatsChanged += RefreshActiveWeapons;
            RefreshActiveWeapons();
        }

        private void RefreshActiveWeapons()
        {
            // 1. 扫描背包邻接，解析真实有效效果。
            List<AdjacencyEffect> effects = inventorySystem.Grid.ScanAdjacency(AdjacencyRuleBook.Rules);
            List<AdjacencyEffect> validEffects = AdjacencyEffectResolver.ResolveValidEffects(effects);
            // 2. 汇总数值类邻接收益。DualWield 不在这里处理，它是激活类效果。
            effectCollector.Collect(validEffects);

            // 3. 重建所有自动武器，避免旧布局效果残留。
            DeactivateAllWeapons();

            List<Item> items = inventorySystem.Grid.GetUniqueItems();
            int activatedCount = 0;
            int bonus = stats != null ? stats.ActiveWeaponLimitBonus : 0;
            int finalActiveWeaponLimit = Mathf.Max(1, activeWeaponLimit + bonus);
            foreach (var item in items)//先遍历到的item就是位置靠前的物品
            {
                if(activatedCount >= finalActiveWeaponLimit) break;
                if (activeWeaponItems.Contains(item)) continue;// 跳过已由双持奖励激活的武器

                if (TryActivateItem(item))
                {
                    activatedCount++;

                    // 双持是“基础激活武器带出的奖励”，必须立刻结算。
                    // 否则奖励武器后面会被基础循环当成普通武器计数。
                    ActivateDualWieldWeapons(validEffects);
                }
            }

        }

        //关闭所有槽位
        private void DeactivateAllWeapons()
        {
            foreach (WeaponSlot weapon in weaponSlots)
            {
                if (weapon == null) continue;
                if (weapon.WeaponObject == null) continue;

                //刷新背包时所有武器都会先关闭，再出现判定激活，旧邻接效果不能残留到下一次布局。
                AutoWeapon autoWeapon = weapon.WeaponObject.GetComponent<AutoWeapon>();
                if (autoWeapon != null)
                {
                    autoWeapon.SetBackpackFireRateMultiplier(1f);//重置背包攻速倍率
                    autoWeapon.SetBackpackWeaponMultiplier(1f);//重置背包武器火力倍率
                    autoWeapon.SetBackpackDamageBoostMultiplier(1f);//重置背包伤害加成倍率
                }
                weapon.WeaponObject.SetActive(false);
            }

            activeWeaponItems.Clear(); //存激活Item的集合
        }

        //尝试激活某一个背包物品，并应用效果。
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

                    if (weaponItemStatResolver == null)
                        autoWeapon.SetBackpackWeaponMultiplier(1f);
                    else
                    {
                        float weaponDamageMultiplier = weaponItemStatResolver.GetDamageMultiplier(item);
                        autoWeapon.SetBackpackWeaponMultiplier(weaponDamageMultiplier); //重置背包火力倍率
                    }
                    //将物品被邻接效果修饰后的状态应用
                    ApplyModifierToAutoWeapon(item, autoWeapon);
                }
                return true;
            }

            return false;
        }
        //将物品被邻接效果修饰后的状态应用
        private void ApplyModifierToAutoWeapon(Item item, AutoWeapon autoWeapon)
        {
            if (item == null) return;
            if (autoWeapon == null) return;

            if (!effectCollector.TryGetModifier(item, out BackpackItemModifier modifier))
                return;

            float fireRateMultiplier = 1f + modifier.FireRateBonus;
            fireRateMultiplier = Mathf.Min(fireRateMultiplier, maxBackpackFireRateMultiplier);
            autoWeapon.SetBackpackFireRateMultiplier(fireRateMultiplier);

            float damageMultiplier = 1f + modifier.DamageBonus;
            damageMultiplier = Mathf.Min(damageMultiplier, maxBackpackDamageBoostMultiplier);
            autoWeapon.SetBackpackDamageBoostMultiplier(damageMultiplier);
        }

        //提供一个查询激活武器的接口
        public bool IsWeaponItemActive(Item item)
        {
            if(activeWeaponItems.Contains(item)) return true;
            return false;
        }


        // DualWield 是激活类邻接效果：
        // 它不产生数值 modifier，而是在已有基础激活武器旁边额外激活另一把武器。
        // 因此它保留在 BackpackWeaponActivator 中处理。
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

 
    }
}
