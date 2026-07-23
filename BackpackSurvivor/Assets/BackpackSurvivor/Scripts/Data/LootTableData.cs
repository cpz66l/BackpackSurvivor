using System;
using System.Collections.Generic;
using UnityEngine;

namespace BS.Data
{
    /// <summary>
    /// 掉落表配置资产，用于定义一组掉落物品及其权重和稀有度。
    /// </summary>
    [CreateAssetMenu(fileName = "NewLootTable",menuName = "BackpackSurvivor/LootTable")]
    public class LootTableData : ScriptableObject
    {
        //嵌套类,掉落物条目
        [Serializable] public class LootEntry
        {
            [Tooltip("掉落的预制体")]
            public GameObject dropPrefab;

            [Tooltip("物品稀有度")]
            public Rarity rarity;

            [Tooltip("权重值（越大掉落概率越高）")]
            public int weight;
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
    }
    public enum Rarity
    {
        Common,     // 白色
        Uncommon,   // 绿色
        Rare,       // 蓝色
        Epic,       // 紫色
        Legendary   // 金色
    }
}


