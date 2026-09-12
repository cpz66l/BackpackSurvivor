using System;
using System.IO;
using System.Linq;
using BS.GamePlay;
using BS.GamePlay.Combat;
using BS.GamePlay.Loot;
using BS.GamePlay.Player;
using BS.GamePlay.Quest;
using BS.GamePlay.Run;
using BS.GamePlay.Save;
using BS.Quest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BackpackSurvivor.EditorTools
{
    [InitializeOnLoad]
    public static class QuestS7Audit
    {
        const string Active = "BS.Quest.CampAuditActive";
        const string Step = "BS.Quest.CampAuditStep";
        static double nextAction;
        static double deadline;
        static string Evidence => Path.GetFullPath(Path.Combine(Application.dataPath,"../../Docs/Evidence/S7"));
        static QuestS7Audit() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Backpack Survivor/Quest/S7 Verify Camp Roundtrip")]
        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the active scene first.");
            SessionState.SetBool("BS.Quest.AuditBackground", Application.runInBackground);
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Path.Combine(Evidence,"verification.txt"), DateTime.UtcNow.ToString("O") + "\n");
            var database = QuestCatalogBuilder.Build();
            Require(database.events.Count == 15 && database.events.Select(e=>e.eventId).Distinct().Count()==15,"15 unique local definitions");
            Require(Enumerable.Range(1,5).All(t=>database.events.Count(e=>e.tier==t)==3),"three events per tier");
            Require(database.AllQuestOnlyIds.Count==2,"full configured questOnly pool");
            var picked = database.Candidates[3];
            var accepted = QuestDrawer.Accept(picked,17,database.AllQuestOnlyIds);
            int original = picked.objectives[0].count;
            accepted.objectives[0].count=999;
            accepted.objectives[0].tags.Clear();
            Require(picked.objectives[0].count==original && picked.objectives[0].tags.Count>0,"accepted clauses deep copied");
            Note("PASS catalog: 15 events/5 tiers, full quest pool, accepted clause isolation.");
            string folder = Path.Combine(Path.GetTempPath(),"BackpackSurvivor-S7-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string blocked = Path.Combine(folder,"directory-not-a-file"); Directory.CreateDirectory(blocked);
            Require(!CampaignFile.TryWrite(blocked,"{}",out _) && Directory.Exists(blocked),"failed atomic write preserves destination");
            string path = Path.Combine(folder,"save_data.json");
            File.WriteAllText(path,"{\"totalRuns\":77,\"totalGold\":123}");
            SessionState.SetString("BS.Quest.AuditSavePath",path);
            SessionState.SetBool(Active,true); SessionState.SetInt(Step,0);
            EditorSceneManager.OpenScene(CampSceneBuilder.ScenePath);
            deadline=EditorApplication.timeSinceStartup+180;
            EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Active,false)) return;
            Application.runInBackground = true;
            if (deadline==0) deadline=EditorApplication.timeSinceStartup+180;
            if (EditorApplication.timeSinceStartup>deadline) { Finish("FAIL timed out"); return; }
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying) return;
            int step=SessionState.GetInt(Step,0);
            if (step==5 && !EditorApplication.isPlaying) { SessionState.SetInt(Step,6); EditorApplication.isPlaying=true; return; }
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup<nextAction) return;
            try
            {
                var camp=Object.FindAnyObjectByType<CampController>();
                var session=Object.FindAnyObjectByType<GameSession>();
                if (step==0 && camp && camp.CurrentQuest!=null)
                {
                    Require(SaveService.Instance.CurrentData.totalRuns==77 && SaveService.Instance.CurrentData.totalGold==123,"old-save values preserved");
                    string json=JsonUtility.ToJson(camp.CurrentQuest);
                    SessionState.SetString("BS.Quest.Accepted",json);
                    ScreenCapture.CaptureScreenshot(Path.Combine(Evidence,"camp.png"));
                    Note("PASS initial camp draw + disk save, old save preserved; "+camp.CurrentQuest.eventId);
                    SessionState.SetInt(Step,1); nextAction=EditorApplication.timeSinceStartup+1;
                }
                else if (step==1 && camp) { Click("LaunchButton"); SessionState.SetInt(Step,2); }
                else if (step==2 && session && session.State==GameState.Running)
                {
                    Require(JsonUtility.ToJson(session.CurrentQuest)==SessionState.GetString("BS.Quest.Accepted",""),"same conditions injected on launch");
                    var loot=Object.FindAnyObjectByType<LootManager>();
                    Require(loot.ContractRun && session.CurrentQuest.activeQuestOnlyItemIds.Count==2,"contract mode + full pool");
                    session.SendMessage("TogglePause");
                    var restart = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="Restart" && b.transform.parent.name=="PauseDialog");
                    restart.onClick.Invoke();
                    Note("PASS camp-to-run contract identity and loot context; pause retry returns to camp.");
                    SessionState.SetInt(Step,3);
                }
                else if (step==3 && camp && camp.CurrentQuest!=null)
                {
                    Require(JsonUtility.ToJson(camp.CurrentQuest)==SessionState.GetString("BS.Quest.Accepted",""),"pause retry retained contract");
                    int draws=SaveService.Instance.CurrentData.campaign.drawCount;
                    Click("RedrawButton");
                    Require(SaveService.Instance.CurrentData.campaign.drawCount==draws+1,"unlimited redraw updates pending");
                    SessionState.SetString("BS.Quest.Accepted",JsonUtility.ToJson(camp.CurrentQuest));
                    Click("LaunchButton"); SessionState.SetInt(Step,4);
                }
                else if (step==4 && session && session.State==GameState.Running)
                {
                    var health=Object.FindAnyObjectByType<PlayerController>().GetComponent<Health>();
                    health.TakeDamage(new DamageInfo(float.MaxValue,health.gameObject,health.Position,false,0));
                    Require(session.LastQuestSnapshot.outcome==RunOutcome.Died && !session.LastQuestOutcome.Completed,"death does not clear pending");
                    Require(JsonUtility.ToJson(SaveService.Instance.CurrentData.campaign.pendingQuest)==SessionState.GetString("BS.Quest.Accepted",""),"death pending retained");
                    var restart=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="Restart" && b.transform.parent.name=="ResultDialog");
                    restart.onClick.Invoke();
                    SessionState.SetInt(Step,7);
                }
                else if (step==7 && camp && camp.CurrentQuest!=null)
                {
                    Note("PASS redraw then second launch; real death preserved pending; result button returned to camp.");
                    SessionState.SetInt(Step,5); EditorApplication.isPlaying=false;
                }
                else if (step==6 && camp && camp.CurrentQuest!=null)
                {
                    Require(JsonUtility.ToJson(camp.CurrentQuest)==SessionState.GetString("BS.Quest.Accepted",""),"pending restored after Play restart");
                    File.Copy(SessionState.GetString("BS.Quest.AuditSavePath",""),Path.Combine(Evidence,"restored-save.json"),true);
                    Finish("PASS Play restart restored complete pending conditions; test used temporary save only.");
                }
            }
            catch(Exception e) { Finish("FAIL "+e); Debug.LogException(e); }
        }
        static void Click(string name) { GameObject.Find(name).GetComponent<Button>().onClick.Invoke(); }
        static void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
        static void Note(string message) { File.AppendAllText(Path.Combine(Evidence,"verification.txt"),DateTime.UtcNow.ToString("O")+" "+message+"\n"); Debug.Log("[Quest S7] "+message); }
        static void Finish(string message)
        {
            Note(message);
            SessionState.SetBool(Active,false);
            SessionState.EraseString("BS.Quest.AuditSavePath");
            Application.runInBackground = SessionState.GetBool("BS.Quest.AuditBackground", false);
            EditorApplication.isPlaying=false;
        }
    }
}
