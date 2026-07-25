using System;
using UnityEngine;
using BS.Inventory;

namespace BS.Data
{
    /// <summary>
    /// 掉落表配置资产，用于定义一组掉落物品及其权重和稀有度。
    /// </summary>
    [CreateAssetMenu(fileName = "NewLootTable",menuName = "BackpackSurvivor/LootTable")]
    public class LootTableData : ScriptableObject
    {
        //一张表要么是束（channels 非空）要么是叶（entries 非空），只许一种。
        //嵌套类：掉落频道（束结构）
        [Serializable] public class DropChannel
        {
            [Range(0, 1)] public float probability = 1f;  // 1 = 必掉（经验球用这个）
            public LootTableData subTable;                 // 引用另一张表 → 递归就此成立
        }
        public DropChannel[] channels;   // 束模式：频道列表

        //嵌套类：掉落物条目
        [Serializable] public class LootEntry
        {
            [Tooltip("品类")]
            public DropCategory category = DropCategory.Equipment;

            [Tooltip("名字")]
            public string id;

            [Tooltip("掉落的预制体")]
            public GameObject dropPrefab;

            [Tooltip("物品稀有度")]
            public Rarity rarity;

            [Tooltip("权重值（越大掉落概率越高）")]
            public int weight;

            [Tooltip("面额值")]
            public int amount = 1;

            [Tooltip("宽度")]
            public int width = 1;

            [Tooltip("高度")]
            public int height = 1;
        }
        [Tooltip("所有可能的掉落条目")]
        public LootEntry[] entries ;

        /// <summary>
        /// 总权重（所有条目权重之和）。用于概率计算。
        /// </summary>
        public int TotalWeight
        {
            get
            {
                int total = 0;
                if (entries != null)
                {
                    foreach (LootEntry entry in entries)
                    {
                        if (entry != null && entry.weight > 0) // 忽略无效或零权重条目
                            total += entry.weight;
                    }
                }
                return total;
            }
        }


        public enum DropCategory
        {
            Xp,
            Gold,
            Equipment
        }
}
   
}


