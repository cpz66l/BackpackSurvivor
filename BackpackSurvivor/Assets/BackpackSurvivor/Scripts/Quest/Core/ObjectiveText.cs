using System.Linq;
using BS.Inventory;

namespace BS.Quest
{
    public static class ObjectiveText
    {
        public static string Format(ObjectiveClause c)
        {
            if (c == null) return "无效条件";
            string text;
            switch (c.type)
            {
                case ObjectiveType.CarryTag: text=$"携带{Tag(c.tag)}至少 {c.count} 件"; break;
                case ObjectiveType.CarryTagSet: text=$"携带{string.Join(" / ", (c.tags ?? new System.Collections.Generic.List<ItemTag>()).Select(Tag))}合计至少 {c.count} 件"; break;
                case ObjectiveType.CarryRarity: text=$"携带{Quality((int)c.rarity)}及以上物品至少 {c.count} 件"; break;
                case ObjectiveType.CarryItem: text=$"携带「{c.itemId}」至少 {c.count} 件"; break;
                case ObjectiveType.CarryItemAtLevel: text=$"携带 Lv{c.itemLevel} 及以上「{c.itemId}」至少 {c.count} 件"; break;
                case ObjectiveType.CarryAnyItemAtLevel: text=$"携带任意 Lv{c.itemLevel} 及以上装备至少 {c.count} 件"; break;
                case ObjectiveType.CarryItemAnyOf: text=$"携带{string.Join(" / ", c.itemIds ?? new System.Collections.Generic.List<string>())}合计至少 {c.count} 件"; break;
                case ObjectiveType.BackpackValueAtLeast: text=$"背包价值至少 {c.minValue:N0}"; break;
                case ObjectiveType.BackpackValueAtMost: text=$"背包价值至多 {c.maxValue:N0}"; break;
                case ObjectiveType.ExcludeTag: text=$"不携带{Tag(c.tag)}"; break;
                case ObjectiveType.ExcludeRarity: text=$"不携带{Quality((int)c.rarity)}及以上物品"; break;
                case ObjectiveType.KillTotal: text=$"累计击杀至少 {c.count}"; break;
                case ObjectiveType.KillElite: text=$"击杀精英至少 {c.count}"; break;
                case ObjectiveType.OpenChestAtLeast: text=$"开启{Quality((int)c.minChestQuality)}宝箱至少 {c.count} 个（仅计该品质）"; break;
                case ObjectiveType.ReachLevel: text=$"达到等级 {c.count}"; break;
                case ObjectiveType.SurviveToSecond: text=$"存活至少 {c.count} 秒"; break;
                default: text="未知条件，无法判定"; break;
            }
            return (c.optional ? "加分项：" : "") + text;
        }
        public static string Quality(int value) => value >= 0 && value < 5 ? new[] { "普通", "不普通", "稀有", "史诗", "传说" }[value] : "未知品质";
        public static string Tag(ItemTag tag)
        {
            string[] names = { "未分类物品", "手枪", "步枪", "霰弹枪", "狙击枪", "攻击芯片", "攻速芯片", "弹鼓", "瞄准镜", "火焰核心", "护甲", "医疗物品", "收集品", "机械臂", "磁吸核心" };
            int i = (int)tag; return i >= 0 && i < names.Length ? names[i] : "未知标签";
        }
    }
}
