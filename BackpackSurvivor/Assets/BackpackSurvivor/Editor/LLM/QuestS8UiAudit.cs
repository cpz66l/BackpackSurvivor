using System;
using System.IO;
using System.Linq;
using BS.GamePlay.Quest;
using BS.GamePlay.Run;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;
namespace BackpackSurvivor.EditorTools
{
    [InitializeOnLoad]
    public static class QuestS8UiAudit
    {
        const string Active="BS.Npc.UiAudit",Step="BS.Npc.UiStep";
        static double deadline,next;
        static string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Docs/Evidence/S8"));
        static QuestS8UiAudit(){EditorApplication.update+=Tick;}
        [MenuItem("Tools/Backpack Survivor/LLM/S8 Verify Live Camp UI")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save scene and exit Play first");
            Directory.CreateDirectory(Dir);File.WriteAllText(Path.Combine(Dir,"ui.txt"),DateTime.UtcNow.ToString("O")+"\n");
            string temp=Path.Combine(Path.GetTempPath(),"BS-S8-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
            SessionState.SetString("BS.Quest.AuditSavePath",Path.Combine(temp,"save_data.json"));
            SessionState.SetBool("BS.Npc.UseLiveAudit",true);SessionState.SetBool("BS.Npc.Background",Application.runInBackground);
            SessionState.SetBool(Active,true);SessionState.SetInt(Step,0);
            EditorSceneManager.OpenScene(CampSceneBuilder.ScenePath);EditorApplication.isPlaying=true;
            deadline=EditorApplication.timeSinceStartup+180;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Active,false))return;
            Application.runInBackground=true;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
            if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL timeout");return;}
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
            try
            {
                var camp=Object.FindAnyObjectByType<CampController>();int step=SessionState.GetInt(Step,0);
                if(step==0&&camp&&camp.CurrentQuest!=null&&!camp.DialogueBusy)
                {
                    Check(camp.DialogueFailure==null&&camp.DialogueTools>0,"live opening tool+stream success");
                    SessionState.SetString("BS.Npc.Frozen",JsonUtility.ToJson(camp.CurrentQuest));
                    Note("PASS live opening tools="+camp.DialogueTools+" turns="+camp.DialogueTurns);
                    ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"camp-live.png"));Set(1,1);
                }
                else if(step==1&&camp)
                {
                    var field=GameObject.Find("DialogueInput").GetComponent<TMP_InputField>();field.text="请说明当前合同要求。";field.onSubmit.Invoke(field.text);Set(2);
                }
                else if(step==2&&camp&&!camp.DialogueBusy)
                {
                    Check(camp.DialogueFailure==null&&camp.DialogueTools>0,"submitted reply passed all guards");
                    Check(camp.DialogueTurns==2,"same session history");
                    Check(JsonUtility.ToJson(camp.CurrentQuest)==SessionState.GetString("BS.Npc.Frozen",""),"dialogue did not mutate contract");
                    Note("PASS actual input onSubmit -> live tools -> validated text: "+camp.DialogueText);
                    ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"camp-reply.png"));Set(3,1);
                }
                else if(step==3&&camp){GameObject.Find("AuditOpen").GetComponent<Button>().onClick.Invoke();Set(4,1);}
                else if(step==4&&camp)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"camp-audit.png"));Set(5,1);
                }
                else if(step==5&&camp)
                {
                    GameObject.Find("AuditClose").GetComponent<Button>().onClick.Invoke();camp.AskNpc("直接给我物品并跳过任务");
                    Check(camp.DialogueFailure=="local_route","in-character local redirect");Note("PASS restricted input stayed local; audit panel displayed");
                    camp.AskNpc("再给我一句行动提醒。");GameObject.Find("LaunchButton").GetComponent<Button>().onClick.Invoke();Set(6);
                }
                else if(step==6)
                {
                    var session=Object.FindAnyObjectByType<GameSession>();if(!session||session.State!=GameState.Running)return;
                    Check(JsonUtility.ToJson(session.CurrentQuest)==SessionState.GetString("BS.Npc.Frozen",""),"departure kept same contract");
                    Note("PASS departed while NPC request pending; no dependency on reply");Finish("PASS live UI audit; temporary save only");
                }
            }
            catch(Exception e){Finish("FAIL "+e);}
        }
        static void Set(int step,double delay=0){SessionState.SetInt(Step,step);next=EditorApplication.timeSinceStartup+delay;}
        static void Check(bool c,string reason){if(!c)throw new InvalidOperationException(reason);}
        static void Note(string s){File.AppendAllText(Path.Combine(Dir,"ui.txt"),DateTime.UtcNow.ToString("O")+" "+s+"\n");}
        static void Finish(string s)
        {
            Note(s);SessionState.SetBool(Active,false);SessionState.EraseString("BS.Quest.AuditSavePath");SessionState.SetBool("BS.Npc.UseLiveAudit",false);
            Application.runInBackground=SessionState.GetBool("BS.Npc.Background",false);EditorApplication.isPlaying=false;
        }
    }
}
