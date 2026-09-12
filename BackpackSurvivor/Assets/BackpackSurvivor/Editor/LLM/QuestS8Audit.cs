using System;
using System.IO;
using System.Threading.Tasks;
using BS.GamePlay.Npc;
using BS.GamePlay.Quest;
using UnityEditor;
using UnityEngine;

namespace BackpackSurvivor.EditorTools
{
    public static class QuestS8Audit
    {
        [MenuItem("Tools/Backpack Survivor/LLM/S8 Verify Camp Tool Roundtrip")]
        public static async void Run()
        {
            var db = QuestCatalogBuilder.Build();
            var candidate = db.Draw(1, new System.Collections.Generic.HashSet<string>(), new System.Collections.Generic.HashSet<int>(), 8080);
            string offline = "离线简报：合同条件以本地记录为准，先检查装备再出发。";
            string result = await new NpcDialogueService().RequestCampReplyAsync("请说明当前合同要求。", BS.Quest.QuestDrawer.Accept(candidate, 8080, db.AllQuestOnlyIds), null, offline);
            string evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Evidence/S8"));
            Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence, "verification.txt"), DateTime.UtcNow.ToString("O") + "\nresult=" + result + "\n");
            Debug.Log("[Quest S8] audit finished; response=" + result);
        }
    }
}
