using System;
using System.Collections.Generic;
using BS.Quest;

namespace BS.Npc
{
    public static class FactBlockBuilder
    {
        public static string Build(QuestInstance quest, QuestRunSnapshot snapshot)
        {
            if (quest == null || snapshot == null) return "事实：当前没有进行中的合同。背包：空。";
            return $"事实：合同 {quest.eventId}，Tier {quest.tier}；局面 {snapshot.outcome}；等级 {snapshot.level}；击杀 {snapshot.kills}；背包价值 {snapshot.backpackValue}；金币 {snapshot.gold}；背包物品 {snapshot.items.Count} 件。";
        }
    }

    public static class NpcResponseValidator
    {
        public static bool IsSafe(string response, int maxChars = 600)
        { return !string.IsNullOrWhiteSpace(response) && response.Length <= maxChars && response.IndexOf("完成合同", StringComparison.Ordinal) < 0 && response.IndexOf("保证", StringComparison.Ordinal) < 0; }
    }

    public enum DialogueSurface { Camp, Pulse, Settlement }
    public static class DialogueRouter
    {
        public static bool TryRoute(DialogueSurface surface, out int maxChars)
        { maxChars = surface == DialogueSurface.Pulse ? 60 : 600; return surface == DialogueSurface.Camp || surface == DialogueSurface.Pulse || surface == DialogueSurface.Settlement; }
    }
}
