using System;
using System.Collections.Generic;
using BS.Quest;

namespace BS.Npc
{
    public static class FactBlockBuilder
    {
        public static string Build(QuestInstance quest, QuestRunSnapshot snapshot)
        {
            if (quest == null) return "事实区：当前没有进行中的合同。当前背包：空。尚未开始本局。";
            if (snapshot == null) return "事实区：当前合同=" + quest.eventId + "，Tier=" + quest.tier + "。当前背包：空。尚未开始本局。";
            var items = snapshot.items ?? new List<ItemRecord>();
            return "事实区（只读）：合同=" + quest.eventId + "；Tier=" + quest.tier + "；局面=" + snapshot.outcome + "；等级=" + snapshot.level + "；击杀=" + snapshot.kills + "；精英击杀=" + snapshot.eliteKills + "；背包价值=" + snapshot.backpackValue + "；金币=" + snapshot.gold + "；物品件数=" + items.Count + "。合同条件由本地判定器决定，NPC 不得承诺完成或改变游戏状态。";
        }
    }

    public static class NpcResponseValidator
    {
        public static bool IsSafe(string response, int maxChars = 600)
        {
            if (string.IsNullOrWhiteSpace(response) || response.Length > maxChars) return false;
            string[] forbidden = { "保证完成", "已完成合同", "直接给你", "跳过任务", "修改存档", "改变掉落", "必出", "一定掉落" };
            return !Array.Exists(forbidden, word => response.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }

    public enum DialogueSurface { Camp, Pulse, Settlement }
    public static class DialogueRouter
    {
        public static bool TryRoute(DialogueSurface surface, out int maxChars)
        { maxChars = surface == DialogueSurface.Pulse ? 60 : 600; return surface == DialogueSurface.Camp || surface == DialogueSurface.Pulse || surface == DialogueSurface.Settlement; }
    }
}
