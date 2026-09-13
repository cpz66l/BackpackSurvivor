using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BS.Core.LLM;
using BS.GamePlay.Npc;
using BS.GamePlay.Run;
using BS.GamePlay.Quest;
using BS.GamePlay.Waves;
using BS.Quest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace BackpackSurvivor.EditorTools
{
    [InitializeOnLoad]
    public static class RunRadioFeedbackAudit
    {
        const string Active="BS.Npc.RadioFeedbackAudit";
        static int step,index;
        static bool enteredPlay;
        static double deadline,next;
        static string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Docs/Evidence/Feedback2"));
        static RunRadioFeedbackAudit()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=state=>{
                if(state==PlayModeStateChange.EnteredPlayMode)enteredPlay=true;
                if(state==PlayModeStateChange.ExitingPlayMode)enteredPlay=false;
                if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Active,false))return;
                SessionState.SetBool(Active,false);SessionState.EraseString("BS.Npc.AuditConfigPath");SessionState.EraseString("BS.Quest.AuditSavePath");
                Application.runInBackground=SessionState.GetBool("BS.Radio.Background",false);
            };
        }
        [MenuItem("Tools/Backpack Survivor/Quest/S10 Verify Run Radio Feedback")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Exit Play and save scene first.");
            Directory.CreateDirectory(Dir);File.WriteAllText(Path.Combine(Dir,"radio.txt"),DateTime.UtcNow.ToString("O")+"\n");
            string temp=Path.Combine(Path.GetTempPath(),"BS-Radio-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
            File.WriteAllText(Path.Combine(temp,"config.json"),"{\"npcEnabled\":false}");
            SessionState.SetString("BS.Npc.AuditConfigPath",Path.Combine(temp,"config.json"));SessionState.SetString("BS.Quest.AuditSavePath",Path.Combine(temp,"save.json"));
            SessionState.SetBool("BS.Radio.Background",Application.runInBackground);SessionState.SetBool(Active,true);step=index=0;deadline=next=0;enteredPlay=false;
            EditorSceneManager.OpenScene(CampSceneBuilder.ScenePath);EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Active,false))return;
            Application.runInBackground=true;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
            if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL timeout");return;}
            if(!enteredPlay||!Application.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
            try
            {
                if(step==0)
                {
                    var camp=Object.FindAnyObjectByType<CampController>();if(!camp||camp.CurrentQuest==null)return;
                    var config=LlmConfigService.LoadFile();config.npcEnabled=true;string error;
                    Check(LlmConfigService.SaveFile(config,out error),"enable real radio in isolated configuration");
                    camp.Launch();step=1;return;
                }
                var session=Object.FindAnyObjectByType<GameSession>();var service=Object.FindAnyObjectByType<WavePulseService>();var view=Object.FindAnyObjectByType<RadioPulseReplyView>();var wave=Object.FindAnyObjectByType<WaveDirector>();
                if(!session||!service||!view||!wave||session.State!=GameState.Running)return;
                Time.timeScale=0; // Real WaveDirector transitions with injected timer values; no enemy damage during inspection.
                if(step==1){Check(service && view,"Run scene has attached service and subtitle view");step=2;}
                if(step==2)
                {
                    if(service.StageIndex!=index || string.IsNullOrEmpty(service.LastStatus)||service.LastStatus=="waiting"||service.LastStatus=="requesting")return;
                    File.WriteAllText(Path.Combine(Dir,"radio-stage-"+index+".txt"),service.LastAudit??"");
                    Check(service.LastStatus=="shown" && view.DisplayText.Length>0,"real phase "+index+" radio displayed; status="+service.LastStatus);
                    Note(view.DisplayText);ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"radio-stage-"+index+".png"));step=3;next=EditorApplication.timeSinceStartup+1;
                }
                else if(step==3)
                {
                    var stages=(List<WaveDirector.WaveStage>)Field(wave,"waveStages");
                    if(++index<stages.Count)
                    {
                        var timer=(RunTimer)Field(session,"timer");timer.Tick(stages[index].startTimeSeconds+.05f-timer.Elapsed);step=2;
                    }
                    else{step=99;_=Boundaries(session,service,view,wave);}
                }
            }
            catch(Exception e){Finish("FAIL "+e);}
        }
        static async Task Boundaries(GameSession session,WavePulseService service,RadioPulseReplyView view,WaveDirector wave)
        {
            try
            {
                wave.enabled=false;Set(service,"delaySeconds",0f);Set(service,"ttlSeconds",8f);
                int count=service.DisplayedCount;
                var old=new Reply("旧阶段回复",250);service.DialogueOverride=old;wave.EmitStageForAudit(90,"old");
                await Task.Delay(30);
                var fresh=new Reply("新阶段回复",10);service.DialogueOverride=fresh;wave.EmitStageForAudit(91,"new");
                await Task.Delay(350);
                Check(old.Calls==1&&old.Cancelled&&service.DisplayedCount==count+1&&view.DisplayText.Contains("新阶段回复"),"new phase cancels old token; late old response never replaces subtitle");
                count=service.DisplayedCount;Set(service,"ttlSeconds",.05f);service.DialogueOverride=new Reply("过期回复",100);wave.EmitStageForAudit(92,"late");
                await Task.Delay(180);
                Check(service.DisplayedCount==count&&view.DisplayText==""&&service.LastStatus=="expired_or_inactive","TTL discards slow response");
                Set(service,"ttlSeconds",8f);service.DialogueOverride=new Reply("旧合同回复",100);wave.EmitStageForAudit(93,"previous run");
                session.StartRun();Time.timeScale=0;await Task.Delay(180);
                Check(service.DisplayedCount==count&&view.DisplayText=="","same scene StartRun rejects previous contract response");
                var cfg=LlmConfigService.LoadFile();cfg.npcEnabled=false;string error;LlmConfigService.SaveFile(cfg,out error);
                var disabled=new Reply("不应请求",0);service.DialogueOverride=disabled;wave.EmitStageForAudit(94,"off");await Task.Delay(30);
                Check(disabled.Calls==0&&view.DisplayText==""&&service.LastStatus=="disabled","AI switch prevents radio request");
                cfg.npcEnabled=true;LlmConfigService.SaveFile(cfg,out error);var exiting=new Reply("离场回复",100);service.DialogueOverride=exiting;wave.EmitStageForAudit(95,"exit");
                service.enabled=false;await Task.Delay(180);
                Check(exiting.Cancelled&&view.DisplayText==""&&service.DisplayedCount==count,"disable cancels request and clears subtitle");
                Finish("PASS five real configured phase transitions and injected lifecycle boundaries; isolated config/save; timer accelerated");
            }
            catch(Exception e){Finish("FAIL "+e);}
        }
        sealed class Reply:INpcDialogue
        {
            readonly string value;readonly int delay;public int Calls;public bool Cancelled;
            public Reply(string value,int delay){this.value=value;this.delay=delay;}
            public async Task<string> RequestPulseReplyAsync(string stage,QuestInstance q,QuestRunSnapshot s,string offline,CancellationToken ct=default)
            {Calls++;using(ct.Register(()=>Cancelled=true)){await Task.Delay(delay);return value;}}
            public Task<string> StreamCampReplyAsync(string input,QuestInstance q,string offline,Action<string> sentence,CancellationToken ct)=>throw new NotSupportedException();
            public Task<string> RequestDebriefAsync(QuestInstance q,QuestRunSnapshot s,CancellationToken ct=default)=>throw new NotSupportedException();
        }
        static object Field(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);Note("PASS "+text);}
        static void Note(string text)=>File.AppendAllText(Path.Combine(Dir,"radio.txt"),text+"\n");
        static void Finish(string text){Note(text);EditorApplication.isPlaying=false;}
    }
}
