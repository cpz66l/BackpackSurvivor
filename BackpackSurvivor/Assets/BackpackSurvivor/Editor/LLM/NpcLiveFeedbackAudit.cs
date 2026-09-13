using System;
using System.IO;
using BS.Core.LLM;
using BS.GamePlay.Npc;
using BS.GamePlay.Quest;
using BS.GamePlay.Run;
using BS.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace BackpackSurvivor.EditorTools
{
    [InitializeOnLoad]
    public static class NpcLiveFeedbackAudit
    {
        const string Active="BS.Npc.LiveFeedbackAudit";
        static int step;
        static double deadline,next;
        static string frozen;
        static string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Docs/Evidence/Feedback"));
        static NpcLiveFeedbackAudit()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=state=>{
                if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Active,false))return;
                SessionState.SetBool(Active,false);
                SessionState.EraseString("BS.Npc.AuditConfigPath");SessionState.EraseString("BS.Quest.AuditSavePath");
                Application.runInBackground=SessionState.GetBool("BS.Npc.LiveFeedbackBackground",false);
            };
        }
        [MenuItem("Tools/Backpack Survivor/LLM/S8 Verify Default Live Feedback")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save scene and exit Play first.");
            if(SessionState.GetBool("BS.Npc.UseLiveAudit",false))throw new InvalidOperationException("Remove live override; this audit verifies the real default.");
            var persona=AssetDatabase.LoadAssetAtPath<NpcPersona>("Assets/BackpackSurvivor/Data/Quest/NpcPersona.asset");
            if(!persona || persona.useMockInEditor)throw new InvalidOperationException("Default persona must use live transport.");
            step=0;deadline=next=0;
            Directory.CreateDirectory(Dir);File.WriteAllText(Path.Combine(Dir,"npc-live.txt"),DateTime.UtcNow.ToString("O")+"\n");
            string temp=Path.Combine(Path.GetTempPath(),"BS-LiveFeedback-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
            SessionState.SetString("BS.Quest.AuditSavePath",Path.Combine(temp,"save.json"));
            SessionState.SetString("BS.Npc.AuditConfigPath",Path.Combine(temp,"config.json"));
            SessionState.SetBool("BS.Npc.LiveFeedbackBackground",Application.runInBackground);SessionState.SetBool(Active,true);
            EditorSceneManager.OpenScene(CampSceneBuilder.ScenePath);EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Active,false))return;
            Application.runInBackground=true;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+210;
            if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL timeout");return;}
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
            try
            {
                var camp=Object.FindAnyObjectByType<CampController>();
                if(step==0 && camp && camp.CurrentQuest!=null && !camp.DialogueBusy)
                {
                    Check(camp.DialogueFailure==null && camp.DialogueTools>0 && camp.NpcStatus.Contains("DeepSeek 在线"),"default live opening succeeds without test override");
                    frozen=JsonUtility.ToJson(camp.CurrentQuest);
                    Submit("今天有点紧张，你会怎么安慰我？");step=1;
                }
                else if(step==1 && camp && !camp.DialogueBusy)
                {
                    Check(camp.DialogueFailure==null && camp.DialogueTools==0,"casual emotional reply is live with no forced tool round");
                    Note(camp.DialogueText);
                    Submit("你喜欢喝咖啡还是茶？为什么？");step=2;
                }
                else if(step==2 && camp && !camp.DialogueBusy)
                {
                    Check(camp.DialogueFailure==null && camp.DialogueTools==0,"different casual topic gets a live reply");
                    Check(camp.DialogueTurns==3 && camp.DialogueText.Contains("今天有点紧张") && camp.DialogueText.Contains("咖啡"),"visible transcript retains current session conversation");
                    Note(camp.DialogueText);ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"camp-default-live.png"));Set(3,1);
                }
                else if(step==3 && camp){Submit("当前合同有什么目标？");step=4;}
                else if(step==4 && camp && !camp.DialogueBusy)
                {
                    Check(camp.DialogueFailure==null && camp.DialogueTools>0,"factual question still uses audited read-only tools");
                    Check(JsonUtility.ToJson(camp.CurrentQuest)==frozen,"all conversations preserve contract");
                    GameObject.Find("AuditOpen").GetComponent<Button>().onClick.Invoke();
                    var audit=GameObject.Find("AuditText").GetComponent<TMP_Text>();
                    Check(audit.text.Contains("response status=200") && audit.text.Contains("tool["),"development panel includes raw response and actual tool execution");
                    File.WriteAllText(Path.Combine(Dir,"npc-tool-audit.txt"),audit.text);Set(5,1);
                }
                else if(step==5 && camp)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"camp-feedback-audit.png"));Set(6,1);
                }
                else if(step==6 && camp)
                {
                    var config=LlmConfigService.LoadFile();config.npcEnabled=false;string error;
                    Check(LlmConfigService.SaveFile(config,out error),"save disabled setting to isolated config");
                    SceneManager.LoadScene("Camp");step=7;
                }
                else if(step==7 && camp && !camp.DialogueBusy && camp.CurrentQuest!=null)
                {
                    Check(camp.NpcStatus.Contains("AI NPC 已关闭") && camp.DialogueTurns==0 && camp.DialogueTools==0 && camp.DialogueFailure=="npc_disabled","disabled Camp shows local briefing without a request");
                    Check(!GameObject.Find("DialogueInput").GetComponent<TMP_InputField>().interactable,"disabled mode disables free chat input");
                    Check(JsonUtility.ToJson(camp.CurrentQuest)==frozen,"switch preserves pending contract");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"camp-ai-disabled.png"));Set(8,1);
                }
                else if(step==8 && camp){camp.Launch();step=9;}
                else if(step==9)
                {
                    var session=Object.FindAnyObjectByType<GameSession>();var tracker=Object.FindAnyObjectByType<QuestTrackerView>();
                    if(!session || !tracker || session.State!=GameState.Running)return;
                    Check(JsonUtility.ToJson(session.CurrentQuest)==frozen && tracker.DisplayText.Contains(session.CurrentQuest.eventId),"AI disabled still launches same contract and displays tracker");
                    Finish("PASS default live chat, factual audit, disabled Camp and departure; isolated config/save");
                }
            }
            catch(Exception e)
            {
                var failedCamp=Object.FindAnyObjectByType<CampController>();
                if(failedCamp)
                {
                    var service=typeof(CampController).GetField("dialogue",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(failedCamp) as NpcDialogueService;
                    Note("failure="+failedCamp.DialogueFailure);
                    if(service!=null)File.WriteAllText(Path.Combine(Dir,"npc-failure-audit.txt"),service.Audit);
                }
                Finish("FAIL "+e.GetType().Name+": "+e.Message);
            }
        }
        static void Submit(string text){var input=GameObject.Find("DialogueInput").GetComponent<TMP_InputField>();input.text=text;input.onSubmit.Invoke(text);}
        static void Set(int value,double delay){step=value;next=EditorApplication.timeSinceStartup+delay;}
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);Note("PASS "+text);}
        static void Note(string text)=>File.AppendAllText(Path.Combine(Dir,"npc-live.txt"),text+"\n");
        static void Finish(string text){Note(text);EditorApplication.isPlaying=false;}
    }
}
