using System;
using System.Collections.Generic;
using System.Linq;
using BS.GamePlay.Save;
using BS.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;
using BS.GamePlay.Npc;

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
            if (dialogueInput != null) dialogueInput.onSubmit.AddListener(AskNpc);
            if (CurrentQuest == null && !SaveService.Instance.CurrentData.campaign.finalCompleted) Redraw();
            else Refresh();
        }
        void OnDestroy()
        {
            if (launchButton) launchButton.onClick.RemoveListener(Launch);
            if (redrawButton) redrawButton.onClick.RemoveListener(Redraw);
            if (menuButton) menuButton.onClick.RemoveListener(ReturnToMenu);
            if (dialogueInput != null) dialogueInput.onSubmit.RemoveListener(AskNpc);
        }
        public async void AskNpc(string question)
        {
            if (string.IsNullOrWhiteSpace(question)) return;
            if (dialogueOutput != null) dialogueOutput.text = "调度员正在回复…";
            string reply = await new NpcDialogueService().RequestCampReplyAsync(question, CurrentQuest, null, "离线简报：合同条件以本地记录为准，先检查装备再出发。");
            if (dialogueOutput != null) dialogueOutput.text = reply;
            if (dialogueInput != null) dialogueInput.text = string.Empty;
        }
        public void Redraw()
        {
            if (leaving) return;
            var campaign = SaveService.Instance.CurrentData.campaign;
            if (database == null || campaign.finalCompleted) { Refresh(); return; }
            int seed = Guid.NewGuid().GetHashCode();
            var picked = database.Draw(campaign.tier, new HashSet<string>(campaign.completedEventIds ?? new List<string>()),
                new HashSet<int> { campaign.lastTag }, seed, campaign.recentEventIds);
            if (picked == null) { statusText.text="当前层级没有可用合同。已保留原合同。"; Refresh(); return; }
            var quest = QuestDrawer.Accept(picked, seed, database.AllQuestOnlyIds);
            if (!SaveService.Instance.TrySetPendingQuest(quest, out string error)) statusText.text="合同保存失败：" + error;
            else statusText.text="合同已保存。出击或下次返回营地均使用这份条件。";
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
            factsText.text = "调度员\n\n" + (q?.briefingBody ?? "检查合同与装备，准备好再出发。") + "\n\n当前背包：空\n尚未开始本局。\n死亡或未达成会保留合同，可重试或重抽。";
            launchButton.interactable = q != null && !final && !leaving;
            redrawButton.interactable = database != null && !final && !leaving;
        }
        public void Launch()
        {
            if (leaving || CurrentQuest == null || SaveService.Instance.CurrentData.campaign.finalCompleted) return;
            leaving=true;
            Debug.Log("[Quest Camp] depart: " + CurrentQuest.eventId + ", seed=" + CurrentQuest.seed + ", tier=" + CurrentQuest.tier);
            Time.timeScale=1f;
            SceneManager.LoadScene("01-Run_ArtFull");
        }
        public void ReturnToMenu() { if (leaving) return; leaving=true; SceneManager.LoadScene("MainMenu"); }
    }
}
