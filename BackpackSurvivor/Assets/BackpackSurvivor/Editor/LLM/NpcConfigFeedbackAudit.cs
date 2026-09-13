using System;
using System.IO;
using BS.Core.LLM;
using BS.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BackpackSurvivor.EditorTools
{
    [InitializeOnLoad]
    public static class NpcConfigFeedbackAudit
    {
        const string Active = "BS.Npc.ConfigFeedbackAudit";
        static int step;
        static double deadline, next;
        static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Evidence/Feedback"));
        static NpcConfigFeedbackAudit()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Active, false)) return;
                SessionState.SetBool(Active, false);
                SessionState.EraseString("BS.Npc.AuditConfigPath");
                SessionState.EraseString("BS.Quest.AuditSavePath");
                Application.runInBackground = SessionState.GetBool("BS.Npc.ConfigBackground", false);
            };
        }
        [MenuItem("Tools/Backpack Survivor/LLM/S2 Verify Config Feedback")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save scene and exit Play before audit.");
            step = 0; deadline = next = 0;
            Directory.CreateDirectory(Dir);
            File.WriteAllText(Path.Combine(Dir, "config.txt"), DateTime.UtcNow.ToString("O") + "\n");
            string temp = Path.Combine(Path.GetTempPath(), "BS-Config-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            SessionState.SetString("BS.Npc.AuditConfigPath", Path.Combine(temp, "config.json"));
            SessionState.SetString("BS.Quest.AuditSavePath", Path.Combine(temp, "save.json"));
            SessionState.SetBool("BS.Npc.ConfigBackground", Application.runInBackground);
            SessionState.SetBool(Active, true);
            EditorSceneManager.OpenScene(LlmConfigPanelBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Active, false)) return;
            Application.runInBackground = true;
            if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 100;
            if (EditorApplication.timeSinceStartup > deadline) { Finish("FAIL timeout"); return; }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
            try
            {
                var view = Object.FindAnyObjectByType<NpcConfigView>(FindObjectsInactive.Include);
                if (!view) return;
                if (step == 0)
                {
                    File.WriteAllText(LlmConfigService.ConfigPath, "{\"maxSessionTurns\":31}");
                    var migrated = LlmConfigService.LoadFile();
                    Check(migrated.npcEnabled && migrated.model == LlmModelConfig.DefaultModel && migrated.maxSessionTurns == 31, "old config retains values and receives new defaults");
                    GameObject.Find("LlmConfigButton").GetComponent<Button>().onClick.Invoke();
                    Check(view.isActiveAndEnabled, "first menu button click opens panel");
                    Input("ModelInput").text = "custom-model-test";
                    GameObject.Find("NpcEnabledToggle").GetComponent<Toggle>().isOn = false;
                    view.Apply(); view.Close(); view.Open();
                    Check(!GameObject.Find("NpcEnabledToggle").GetComponent<Toggle>().isOn && Input("ModelInput").text == "custom-model-test", "switch and model survive save/reopen");
                    Input("ModelInput").text = "bad model"; view.Apply();
                    Check(LlmConfigService.LoadFile().model == "custom-model-test", "invalid draft never overwrites saved config");
                    view.RestoreDefaults();
                    Check(Input("ModelInput").text == LlmModelConfig.DefaultModel && GameObject.Find("NpcEnabledToggle").GetComponent<Toggle>().isOn, "development defaults populate draft");
                    Check(Input("ApiKeyInput").inputType == TMP_InputField.InputType.Password && Input("ApiKeyInput").text == "", "key field masked and never prefilled");
                    Check(LlmConfigService.Resolve().KeySource == LlmKeySource.Environment, "existing environment key resolves without exposing value");
                    view.SelfTest(); step = 1; return;
                }
                if (step == 1)
                {
                    if (view.Status.StartsWith("正在")) return;
                    Check(view.Status.StartsWith("自检成功"), "actual DeepSeek draft self-test: " + view.Status);
                    Check(LlmConfigService.LoadFile().model == "custom-model-test" && !LlmConfigService.LoadFile().npcEnabled, "self-test leaves persisted draft unchanged");
                    view.Apply();
                    Check(LlmConfigService.LoadFile().npcEnabled && LlmConfigService.LoadFile().model == LlmModelConfig.DefaultModel, "save explicitly applies defaults");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Dir, "mainmenu-ai-config.png"));
                    step = 2; next = EditorApplication.timeSinceStartup + 1;
                }
                else if (step == 2) Finish("PASS config UI and live self-test; isolated config and save paths");
            }
            catch (Exception e) { Finish("FAIL " + e.GetType().Name + ": " + e.Message); }
        }
        static TMP_InputField Input(string name) => GameObject.Find(name).GetComponent<TMP_InputField>();
        static void Check(bool ok, string text) { if (!ok) throw new InvalidOperationException(text); Note("PASS " + text); }
        static void Note(string text) => File.AppendAllText(Path.Combine(Dir, "config.txt"), text + "\n");
        static void Finish(string text) { Note(text); EditorApplication.isPlaying = false; }
    }
}
