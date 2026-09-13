using System;
using System.Collections.Generic;
using System.Linq;
using BS.GamePlay.Save;
using BS.Core.LLM;
using BS.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading;
using BS.GamePlay.Npc;
using BS.GamePlay.Run;

namespace BS.GamePlay.Quest
{
    public class CampController : MonoBehaviour
    {
        [SerializeField] QuestDatabase database;
        [SerializeField] TMP_Text contractText;
        [SerializeField] TMP_Text factsText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Button launchButton;
        [SerializeField] Button redrawButton;
        [SerializeField] Button menuButton;
        [SerializeField] TMP_InputField dialogueInput;
        [SerializeField] TMP_Text dialogueOutput;
        [SerializeField] NpcPersona persona;
        [SerializeField] TMP_Text auditText;
        [SerializeField] GameObject auditPanel;
        [SerializeField] Button auditOpen, auditClose;
        NpcDialogueService dialogue;
        bool aiEnabled;
        string transportLabel;
        readonly Queue<string> visibleHistory = new Queue<string>();
        public string NpcStatus => factsText == null ? "" : factsText.text;
        CancellationTokenSource replyCancellation;
        int replyRevision;
        public string DialogueText => dialogueOutput == null ? "" : dialogueOutput.text;
        public bool DialogueBusy { get; private set; }
        public string DialogueFailure => dialogue?.LastFailure;
        public int DialogueTurns => dialogue?.Turns ?? 0;
        public int DialogueTools => dialogue?.ToolCount ?? 0;
        public void ConfigureDialogue(NpcPersona local, TMP_Text audit, GameObject panel, Button open, Button close)
        { persona=local; auditText=audit; auditPanel=panel; auditOpen=open; auditClose=close; }
        bool leaving;
        public QuestInstance CurrentQuest => SaveService.Instance?.CurrentData?.campaign?.pendingQuest?.Copy();

        public void Configure(QuestDatabase data, TMP_Text contract, TMP_Text facts, TMP_Text status, Button launch, Button redraw, Button menu, TMP_InputField input = null, TMP_Text output = null)
        {
            database=data; contractText=contract; factsText=facts; statusText=status;
            launchButton=launch; redrawButton=redraw; menuButton=menu;
            dialogueInput=input; dialogueOutput=output;
        }
        void Start()
        {
            Time.timeScale = 1f;
            if (SaveService.Instance == null) new GameObject("SaveService").AddComponent<SaveService>();
            launchButton.onClick.AddListener(Launch);
            redrawButton.onClick.AddListener(Redraw);
            menuButton.onClick.AddListener(ReturnToMenu);
            if (dialogueInput != null)
            {
                // TMP inserts IME underline tags according to the field flag, not just the text component.
                // Also migrate already-generated Camp scenes when they start.
                dialogueInput.richText = false;
                dialogueInput.onSubmit.AddListener(AskNpc);
            }
            if(auditOpen) auditOpen.onClick.AddListener(OpenAudit);
            if(auditClose) auditClose.onClick.AddListener(CloseAudit);
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if(auditOpen) auditOpen.gameObject.SetActive(false);
#endif
            if (CurrentQuest == null && !SaveService.Instance.CurrentData.campaign.finalCompleted) Redraw();
            else Refresh();
            ResetDialogue();
            AskGreeting();
        }
        void OnDestroy()
        {
            if (launchButton) launchButton.onClick.RemoveListener(Launch);
            if (redrawButton) redrawButton.onClick.RemoveListener(Redraw);
            if (menuButton) menuButton.onClick.RemoveListener(ReturnToMenu);
            if (dialogueInput != null) dialogueInput.onSubmit.RemoveListener(AskNpc);
            if(auditOpen) auditOpen.onClick.RemoveListener(OpenAudit);
            if(auditClose) auditClose.onClick.RemoveListener(CloseAudit);
            CancelReply();
            if(dialogue!=null) dialogue.AuditChanged-=UpdateAudit;
        }
        async void AskGreeting()
        {
            if (leaving || DialogueBusy || dialogue==null) return;
            CancelReply(); replyCancellation=new CancellationTokenSource(); int revision=++replyRevision;
            DialogueBusy=true;
            if(dialogueInput) dialogueInput.interactable=false;
            string shown="";
            if(dialogueOutput) dialogueOutput.text="";
            try
            {
                string prompt=persona?.greetingPrompt??NpcPersonaDefaults.GreetingPrompt;
                string reply=await dialogue.StreamCampGreetingAsync(CurrentQuest,prompt,sentence=>{ if(this!=null&&!leaving&&revision==replyRevision&&dialogueOutput){shown+=sentence;PresentDialogue(shown);} },replyCancellation.Token);
                if(this!=null&&!leaving&&revision==replyRevision&&!string.IsNullOrWhiteSpace(reply))
                    visibleHistory.Enqueue((persona?.displayName??NpcPersonaDefaults.DisplayName)+"："+reply);
            }
            catch(OperationCanceledException) { }
            finally { if(this!=null&&revision==replyRevision){DialogueBusy=false;if(dialogueInput)dialogueInput.interactable=!leaving&&aiEnabled;UpdateAudit();} }
        }
        public async void AskNpc(string question)
        {
            if (leaving || DialogueBusy || string.IsNullOrWhiteSpace(question)) return;
            if(dialogue==null) ResetDialogue();
            CancelReply(); replyCancellation=new CancellationTokenSource(); int revision=++replyRevision;
            DialogueBusy=true;
            if(dialogueInput) dialogueInput.interactable=false;
            string shown="";
            string prefix=string.Join("\n\n",visibleHistory);
            string turn="你："+question+"\n"+(persona?.displayName??NpcPersonaDefaults.DisplayName)+"：";
            if(dialogueOutput)dialogueOutput.text=(prefix.Length>0?prefix+"\n\n":"")+turn+"正在回复…";
            UpdateAudit();
            try
            {
                string local=CurrentQuest?.briefingBody ?? persona?.offlineBriefing;
                string reply=await dialogue.StreamCampReplyAsync(question,CurrentQuest,local,sentence=>{
                    if(this!=null && !leaving && revision==replyRevision && dialogueOutput)
                    {
                        shown+=sentence;
                        PresentDialogue((prefix.Length>0?prefix+"\n\n":"")+turn+shown);
                    }
                },replyCancellation.Token);
                if(this!=null && !leaving && revision==replyRevision && dialogueOutput)
                {
                    if(aiEnabled)
                    {
                        visibleHistory.Enqueue(turn+reply);
                        while(visibleHistory.Count>4)visibleHistory.Dequeue();
                        PresentDialogue(string.Join("\n\n",visibleHistory));
                    }
                    else PresentDialogue(reply);
                }
            }
            catch(OperationCanceledException) { }
            finally
            {
                if(this!=null && revision==replyRevision)
                {
                    DialogueBusy=false;
                    if(dialogueInput){dialogueInput.interactable=!leaving&&aiEnabled;dialogueInput.text="";}
                    UpdateAudit();
                }
            }
        }
        void PresentDialogue(string text)
        {
            dialogueOutput.text=text;
            var scroll=dialogueOutput.GetComponentInParent<ScrollRect>();
            if(scroll){Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=0;}
        }
        void CancelReply(){replyRevision++;replyCancellation?.Cancel();replyCancellation?.Dispose();replyCancellation=null;DialogueBusy=false;}
        void ResetDialogue()
        {
            CancelReply();if(dialogue!=null)dialogue.AuditChanged-=UpdateAudit;
            INpcTransport transport=new DeepSeekNpcDialogue();
#if UNITY_EDITOR
            if(persona && persona.useMockInEditor)transport=new MockNpcDialogue(persona);
            if(UnityEditor.SessionState.GetBool("BS.Npc.UseLiveAudit",false))transport=new DeepSeekNpcDialogue();
#endif
            aiEnabled=LlmConfigService.LoadFile().npcEnabled;
            transportLabel=transport is MockNpcDialogue ? "Mock 测试" : "DeepSeek 在线";
            visibleHistory.Clear();
            dialogue=new NpcDialogueService(transport,null,persona?.restrictedReply,persona?.closingReply,persona);
            dialogue.ItemDefinitions=database?.ItemDefinitions;
            dialogue.SetCampHistoricalContext(RunSessionContext.LastQuest,RunSessionContext.LastSnapshot,SaveService.Instance?.CurrentData?.campaign?.runMemoryRecords);
            dialogue.AuditChanged+=UpdateAudit;
            if(dialogueInput)dialogueInput.interactable=aiEnabled;
            if(dialogueOutput)dialogueOutput.text=CurrentQuest?.briefingBody??persona?.offlineBriefing;
            UpdateAudit();
        }
        void OpenAudit(){if(auditPanel)auditPanel.SetActive(true);UpdateAudit();}
        void CloseAudit(){if(auditPanel)auditPanel.SetActive(false);}
        void UpdateAudit()
        {
            string mode=!aiEnabled?"AI NPC 已关闭 · 本地简报":dialogue?.UsedFallback==true?"本地回退 · "+dialogue.LastFailure:transportLabel+(DialogueBusy?" · 正在回复…":"");
            if(factsText)factsText.text=(persona?.displayName??NpcPersonaDefaults.DisplayName)+" · "+mode;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(auditText && dialogue!=null) auditText.text=mode+" · 轮次 "+dialogue.Turns+" · tokens "+dialogue.Tokens+" · "+(dialogue.UsedFallback?"本地回退 "+dialogue.LastFailure:"回复处理中/已验证")+"\n"+dialogue.Audit;
#endif
        }
        public void Redraw()
        {
            if (leaving || DialogueBusy) return;
            var campaign = SaveService.Instance.CurrentData.campaign;
            if (database == null || campaign.finalCompleted) { Refresh(); return; }
            int seed = Guid.NewGuid().GetHashCode();
            var picked = database.Draw(campaign.tier, new HashSet<string>(campaign.completedEventIds ?? new List<string>()),
                new HashSet<int> { campaign.lastTag }, seed, campaign.recentEventIds);
            if (picked == null) { statusText.text="当前层级没有可用合同。已保留原合同。"; Refresh(); return; }
            var quest = QuestDrawer.Accept(picked, seed, database.AllQuestOnlyIds);
            if (!SaveService.Instance.TrySetPendingQuest(quest, out string error)) statusText.text="合同保存失败：" + error;
            else
            {
                statusText.text="合同已保存。出击或下次返回营地均使用这份条件。";
                dialogue?.NotifyContractChanged();
            }
            Refresh();
        }
        public void Refresh()
        {
            var q = CurrentQuest;
            bool final = SaveService.Instance.CurrentData.campaign.finalCompleted;
            contractText.text = final ? "战役已通关\n全部终局要求已完成。" : q == null ? "暂无可用合同" :
                q.briefingTitle + " · " + new string('★', Math.Max(1,Math.Min(5,q.tier))) + "\n\n" + q.briefingBody +
                "\n\n必须存活带出，并满足以下全部必选条件：\n" + string.Join("\n", q.objectives.Select(c => "• " + ObjectiveText.Format(c))) +
                "\n\n任务局专属池：" + string.Join(" / ", q.activeQuestOnlyItemIds);
            factsText.text = (persona?.displayName??NpcPersonaDefaults.DisplayName)+"\n"+(aiEnabled?"在营地等你。":"暂时休息中。") ;
            launchButton.interactable = q != null && !final && !leaving;
            redrawButton.interactable = database != null && !final && !leaving;
        }
        public void Launch()
        {
            if (leaving || CurrentQuest == null || SaveService.Instance.CurrentData.campaign.finalCompleted) return;
            leaving=true;
            CancelReply();
            Debug.Log("[Quest Camp] depart: " + CurrentQuest.eventId + ", seed=" + CurrentQuest.seed + ", tier=" + CurrentQuest.tier);
            Time.timeScale=1f;
            SceneManager.LoadScene("01-Run_ArtFull");
        }
        public void ReturnToMenu() { if (leaving) return; leaving=true; CancelReply(); RunSessionContext.ClearSettlement(); SceneManager.LoadScene("MainMenu"); }
    }
}
