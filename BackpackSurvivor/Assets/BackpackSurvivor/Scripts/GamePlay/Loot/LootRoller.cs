using BS.Data;
using BS.Inventory;
using System;
using System.Collections.Generic;
using static BS.Data.LootTableData;

namespace BS.GamePlay.Loot
{
    /// <summary>
    /// 带保底机制的掉落掷骰器。
    /// </summary>
    public class LootRoller
    {
        private int pityCount;
        private int pityThreshold;

        /// <summary>
        /// 构造保底掷骰器
        /// </summary>
        /// <param name="pityThreshold">触发保底所需的累计未中次数阈值</param>
        public LootRoller(int pityThreshold)//构造函数
        {
            this.pityThreshold = pityThreshold;
            pityCount = 0;
        }

        /// <summary>
        /// 根据掉落频道进行一次抽取
        /// </summary>
        /// <param name="bundle">掉落频道</param>
        /// <returns>选中的频道，若无有效条目则返回 null</returns>
        public List<LootEntry> RollBundle(LootTableData bundle)
        {
            List<LootEntry> entries = new List<LootEntry>();
            // 若频道为空或无效，直接返回 null
            if (bundle == null || bundle.channels == null || bundle.channels.Length == 0)
                return entries;
            foreach (var c in bundle.channels)
            {
                if(UnityEngine.Random.value < c.probability)
                {
                    LootEntry entry = Roll(c.subTable);
                    if (entry != null)
                        entries.Add(entry);
                }
            }
            return entries;
        }

        /// <summary>
        /// 根据掉落表进行一次抽取，自动处理保底逻辑。
        /// </summary>
        /// <param name="table">掉落表资产</param>
        /// <returns>选中的掉落条目，若无有效条目则返回 null</returns>
        public LootEntry Roll(LootTableData table)
        {
            // 若条目为空或无效，直接返回 null
            if (table == null || table.entries == null || table.entries.Length == 0)
                return null;

            //触发保底阶段
            if (pityCount >= pityThreshold)
            {
                //获得从稀有度≥蓝 的条目
                LootEntry[] eligible = Array.FindAll
                    (table.entries, e => e != null && e.weight > 0
                    && (int)e.rarity >= (int)Rarity.Rare);

                if (eligible.Length > 0)
                {
                    LootEntry selected = PickByWeight(eligible);
                    if (selected != null)
                    {
                        pityCount = 0;
                        return selected;
                    }
                    // 若没有高级条目（数据异常），降级为全表抽取，但依然清零保底（视为保底已用）
                    // 可在此处记录警告日志，但为稳健继续执行
                }
            }
            //全表抽取
            LootEntry result = PickByWeight(table.entries);
            if (result == null) return null;
            //中蓝以上保底也清零
            if (result.category == DropCategory.Equipment && (int)result.rarity >= (int)Rarity.Rare) pityCount = 0;
            //没中则奖池累计
            else if(result.category == DropCategory.Equipment) pityCount++;

            return result;
        }

        /// <summary>
        /// 从给定条目数组中按权重随机选择一个。
        /// </summary>
        private LootEntry PickByWeight(LootEntry[] entries)
        {
            // 计算总权重
            int total = 0;
            foreach (var e in entries)
            {
                if (e != null && e.weight > 0)
                    total += e.weight;
            }

            if (total <= 0)
                return null; // 无有效权重

            //随机掷点
            int roll = UnityEngine.Random.Range(0, total);
            int accum = 0;
            foreach (var e in entries)
            {
                if (e != null && e.weight > 0)
                {
                    accum += e.weight;
                    if (roll < accum)
                    {
                        return e;
                    }
                }
            }
            return null;//理论上不会到达
        }
    }
}
