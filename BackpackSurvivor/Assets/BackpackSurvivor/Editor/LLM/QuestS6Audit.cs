using System.Linq;
using BS.Data;
using BS.GamePlay.Loot;
using UnityEditor;
using UnityEngine;

namespace BackpackSurvivor.EditorTools
{
    public static class QuestS6Audit
    {
        [MenuItem("Tools/Backpack Survivor/Quest/S6 Check QuestOnly Filter")]
        public static void Check()
        {
            var table = ScriptableObject.CreateInstance<LootTableData>();
            table.entries = new[]
            {
                new LootTableData.LootEntry { id="normal", weight=1, questOnly=false },
                new LootTableData.LootEntry { id="quest", weight=1, questOnly=true }
            };
            var roller = new LootRoller(1);
            Random.InitState(20260912);
            for (int i=0;i<100;i++) QuestS3Audit.Require(roller.Roll(table, LootContext.Normal).questOnly == false, "normal context leaked questOnly");
            var allow = new LootContext(true, new[] { "quest" });
            bool sawQuest = false;
            for (int i=0;i<100;i++) { var e=roller.Roll(table, allow); if (e.questOnly) sawQuest=true; }
            QuestS3Audit.Require(sawQuest, "contract context never allowed questOnly");
            var restricted = new LootContext(true, new[] { "other" });
            for (int i=0;i<100;i++) QuestS3Audit.Require(!roller.Roll(table, restricted).questOnly, "allowed id filter leaked questOnly");
            Object.DestroyImmediate(table);
            QuestS3Audit.Record("PASS S6 questOnly filter: normal excludes; contract allows full configured id; restricted ids exclude.");
        }
    }
}
