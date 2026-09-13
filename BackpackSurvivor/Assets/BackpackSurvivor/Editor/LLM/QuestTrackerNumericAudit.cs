using System;
using System.IO;
using System.Reflection;
using BS.GamePlay;
using BS.GamePlay.Quest;
using BS.GamePlay.Run;
using BS.GamePlay.Save;
using BS.Inventory;
using BS.Presentation;
using BS.Quest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BackpackSurvivor.EditorTools
{
    [InitializeOnLoad]
    public static class QuestTrackerNumericAudit
    {
        const string Active = "BS.Quest.TrackerNumericAudit";
        static int step;
        static double next, deadline;
        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Evidence/Feedback2"));
        static QuestTrackerNumericAudit()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Active, false)) return;
                SessionState.SetBool(Active, false);
                SessionState.EraseString("BS.Quest.AuditSavePath");
                SessionState.EraseString("BS.Npc.AuditConfigPath");
                Application.runInBackground = SessionState.GetBool("BS.Quest.TrackerBackground", false);
            };
        }
        [MenuItem("Tools/Backpack Survivor/Quest/S9 Verify Numeric Tracker")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save scene and exit Play before audit.");
            Directory.CreateDirectory(Dir);
            File.WriteAllText(Path.Combine(Dir,"tracker-numeric.txt"), DateTime.UtcNow.ToString("O")+"\n");
            string path = Path.Combine(Path.GetTempPath(), "BS-Tracker-" + Guid.NewGuid().ToString("N"), "save.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string configPath=Path.Combine(Path.GetDirectoryName(path),"config.json");
            File.WriteAllText(configPath,"{\"npcEnabled\":false}");
            SessionState.SetString("BS.Npc.AuditConfigPath",configPath);
            SessionState.SetString("BS.Quest.AuditSavePath", path);
            SessionState.SetBool("BS.Quest.TrackerBackground", Application.runInBackground);
            SessionState.SetBool(Active, true);
            EditorSceneManager.OpenScene(CampSceneBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Active, false)) return;
            if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 90;
            Application.runInBackground = true;
            if (EditorApplication.timeSinceStartup > deadline) { Finish("FAIL timeout"); return; }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (step == 0)
                {
                    var camp = Object.FindAnyObjectByType<CampController>();
                    if (!camp || camp.CurrentQuest == null) return;
                    camp.Launch(); step = 1; return;
                }
                var session = Object.FindAnyObjectByType<GameSession>();
                var tracker = Object.FindAnyObjectByType<QuestTrackerView>();
                if (!session || !tracker || session.State == GameState.NotStarted) return;
                if (step == 1)
                {
                    Time.timeScale = 0; // keep runtime inspection independent of enemy damage
                    Check(tracker.DisplayText.Contains(session.CurrentQuest.eventId), "actual Camp launch binds contract");
                    typeof(GameSession).GetField("currentQuest", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(session, null);
                    typeof(GameSession).GetField("state", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(session, GameState.NotStarted);
                    tracker.SendMessage("Start"); // deliberately reproduce the previously broken callback order
                    var q = new QuestInstance { eventId="tracker-feedback", tier=2, briefingTitle="追踪验证",
                        objectives=new System.Collections.Generic.List<ObjectiveClause> { new ObjectiveClause { type=ObjectiveType.CarryItem, itemId="测试物品", count=2 }, new ObjectiveClause{type=ObjectiveType.BackpackValueAtLeast,minValue=8000} } };
                    string error;
                    Check(SaveService.Instance.TrySetPendingQuest(q,out error), "isolated pending write");
                    session.StartRun();
                    Check(tracker.DisplayText.Contains(q.eventId) && tracker.DisplayText.Contains("Tier 2"), "tracker Start before StartRun recovers");
                    tracker.enabled = false; session.StartRun(); tracker.enabled = true;
                    Check(tracker.DisplayText.Contains(q.eventId), "tracker enabled after StartRun recovers");
                    var grid = Object.FindAnyObjectByType<InventorySystem>().Grid;
                    var item = new Item("测试物品", Rarity.Common, 1, 1, ItemTag.Medical, ConnectableSides.None, 5200, 0, 1);
                    session.PauseRun();
                    Check(grid.Place(0,0,item) && tracker.DisplayText.Contains("当前 1 / 2") && tracker.DisplayText.Contains("5,200 / 8,000"), "paused add immediately shows partial count and value");
                    grid.Remove(item);
                    Check(tracker.DisplayText.Contains("当前 0 / 2"), "paused removal immediately regresses numeric progress");
                    grid.Place(0,0,item); session.ResumeRun(); Time.timeScale = 0;
                    Note("HUD="+tracker.DisplayText.Replace("\n"," | "));
                    ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"tracker-numeric.png"));
                    step=2; next=EditorApplication.timeSinceStartup+1;
                }
                else if (step == 2)
                {
                    typeof(GameSession).GetMethod("EndRun",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(session,new object[]{GameState.Defeat});
                    Check(tracker.DisplayText.Contains("本局已失效"), "death preserves explicit invalidation text");
                    Check(SaveService.Instance.CurrentData.campaign.pendingQuest != null, "death keeps pending");
                    Note("DEATH="+tracker.DisplayText.Replace("\n"," | "));
                    ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"tracker-numeric-death.png"));
                    step=3; next=EditorApplication.timeSinceStartup+1;
                }
                else Finish("PASS numeric tracker lifecycle; partial inventory and death injected; isolated save/config");
            }
            catch (Exception e) { Finish("FAIL "+e); }
        }
        static void Check(bool pass,string why) { if(!pass)throw new InvalidOperationException(why);Note("PASS "+why); }
        static void Note(string s) { File.AppendAllText(Path.Combine(Dir,"tracker-numeric.txt"),s+"\n"); }
        static void Finish(string s) { Note(s); EditorApplication.isPlaying=false; }
    }
}
