using System;
using System.IO;
using System.Threading;
using BS.GamePlay.Npc;
using UnityEditor;
using UnityEngine;

namespace BackpackSurvivor.EditorTools
{
    public static class QuestS8Audit
    {
        public static bool Running { get; private set; }
        public static string LastResult { get; private set; }
        [MenuItem("Tools/Backpack Survivor/LLM/S8 Verify Camp Tool Roundtrip")]
        public static async void Run()
        {
            if(Running)throw new InvalidOperationException("S8 audit already running");
            Running=true;
            string evidence=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Docs/Evidence/S8"));
            Directory.CreateDirectory(evidence);
            var service=new NpcDialogueService();
            try
            {
                var db=QuestCatalogBuilder.Build();
                var q=BS.Quest.QuestDrawer.Accept(db.Candidates[0],8080,db.AllQuestOnlyIds);
                File.WriteAllText(Path.Combine(evidence,"sentences.txt"),DateTime.UtcNow.ToString("O")+"\n");
                var timer=System.Diagnostics.Stopwatch.StartNew();
                string result=await service.StreamCampReplyAsync("请说明当前合同要求。",q,"先核对合同，准备好再出发。",sentence=>File.AppendAllText(Path.Combine(evidence,"sentences.txt"),timer.ElapsedMilliseconds+"ms "+sentence+"\n"),CancellationToken.None);
                LastResult=(service.UsedFallback?"FAIL "+service.LastFailure:"PASS")+" tools="+service.ToolCount+" chunks="+service.StreamChunks+" firstSentenceMs="+service.FirstSentenceMilliseconds+" turns="+service.Turns+" tokens="+service.Tokens+"\nresult="+result;
                File.WriteAllText(Path.Combine(evidence,"verification.txt"),DateTime.UtcNow.ToString("O")+"\n"+LastResult);
            }
            catch(Exception e){LastResult="FAIL "+e.GetType().Name;File.WriteAllText(Path.Combine(evidence,"verification.txt"),LastResult);}
            finally {File.WriteAllText(Path.Combine(evidence,"audit.txt"),service.Audit);Running=false;}
        }
    }
}
