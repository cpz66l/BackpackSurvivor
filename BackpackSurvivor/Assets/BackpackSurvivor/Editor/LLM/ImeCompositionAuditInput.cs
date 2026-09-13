using System;
using System.IO;
using BS.GamePlay.Quest;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BackpackSurvivor.EditorTools
{
    [InitializeOnLoad]
    public static class ImeFeedbackAudit
    {
        const string Active="BS.Npc.ImeFeedbackAudit";
        static double deadline,next;
        static int step;
        static bool enteredPlay;
        static ImeCompositionAuditInput ime;
        static string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Docs/Evidence/Feedback2"));
        static ImeFeedbackAudit()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=state=>{
                if(state==PlayModeStateChange.EnteredPlayMode)enteredPlay=true;
                if(state==PlayModeStateChange.ExitingPlayMode)enteredPlay=false;
                if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Active,false))return;
                SessionState.SetBool(Active,false);SessionState.EraseString("BS.Npc.AuditConfigPath");SessionState.EraseString("BS.Quest.AuditSavePath");
                Application.runInBackground=SessionState.GetBool("BS.Ime.Background",false);
            };
        }
        [MenuItem("Tools/Backpack Survivor/LLM/S8 Verify IME Composition")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Exit Play and save scene first.");
            Directory.CreateDirectory(Dir);File.WriteAllText(Path.Combine(Dir,"ime.txt"),DateTime.UtcNow.ToString("O")+"\n");
            string temp=Path.Combine(Path.GetTempPath(),"BS-Ime-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
            File.WriteAllText(Path.Combine(temp,"config.json"),"{\"npcEnabled\":false}");
            SessionState.SetString("BS.Npc.AuditConfigPath",Path.Combine(temp,"config.json"));SessionState.SetString("BS.Quest.AuditSavePath",Path.Combine(temp,"save.json"));
            SessionState.SetBool("BS.Ime.Background",Application.runInBackground);SessionState.SetBool(Active,true);step=0;deadline=next=0;
            EditorSceneManager.OpenScene(CampSceneBuilder.ScenePath);EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Active,false))return;
            Application.runInBackground=true;
            if(deadline==0)deadline=EditorApplication.timeSinceStartup+60;
            if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL timeout");return;}
            if(!enteredPlay||!Application.isPlaying||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
            try
            {
                var camp=UnityEngine.Object.FindAnyObjectByType<CampController>();if(!camp||camp.CurrentQuest==null)return;
                var input=GameObject.Find("DialogueInput").GetComponent<TMP_InputField>();
                if(step==0)
                {
                    if(EventSystem.current==null || EventSystem.current.currentInputModule==null)return;
                    Check(!input.richText&&!input.textComponent.richText,"generated scene migrates field and text flags together");
                    var module=EventSystem.current.currentInputModule;
                    ime=new GameObject("ImeFeedbackInput").AddComponent<ImeCompositionAuditInput>();
                    module.inputOverride=ime;
                    input.interactable=true;input.Select();input.ActivateInputField();step=1;next=EditorApplication.timeSinceStartup+.2;
                }
                else if(step==1)
                {
                    EventSystem.current.currentInputModule.inputOverride=ime;
                    ime.composition="wo'xiang";
                    input.richText=true;input.textComponent.richText=false;input.ForceLabelUpdate();
                    Check(input.textComponent.text.Contains("<u>"),"control reproduces literal underline tags with old mismatched flags");
                    input.richText=false;input.ForceLabelUpdate();
                    Check(input.textComponent.text.Contains("wo'xiang")&&!input.textComponent.text.Contains("<u>"),"composition renders without markup after fix");
                    Check(input.text=="","composition is not committed into submitted text");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"ime-composition.png"));step=2;next=EditorApplication.timeSinceStartup+1;
                }
                else if(step==2)
                {
                    ime.composition="";
                    input.text="我想聊聊今天的行动";input.ForceLabelUpdate();
                    Check(input.textComponent.text.Contains(input.text)&&!input.text.Contains("<u>"),"committed Chinese text remains intact");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Dir,"ime-chinese.png"));step=3;next=EditorApplication.timeSinceStartup+1;
                }
                else Finish("PASS TMP composition pipeline simulation; physical Windows IME still needs user confirmation");
            }
            catch(Exception e){Finish("FAIL "+e);}
        }
        static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);File.AppendAllText(Path.Combine(Dir,"ime.txt"),"PASS "+text+"\n");}
        static void Finish(string text){File.AppendAllText(Path.Combine(Dir,"ime.txt"),text+"\n");EditorApplication.isPlaying=false;}
    }
}
