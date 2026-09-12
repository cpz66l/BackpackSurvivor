using BS.GamePlay.Save;
using BS.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BS.GamePlay.Quest
{
    public class CampController : MonoBehaviour
    {
        [SerializeField] TMP_Text contractText;
        [SerializeField] TMP_Text factsText;
        [SerializeField] Button launchButton;
        public void Configure(TMP_Text contract, TMP_Text facts, Button launch) { contractText=contract; factsText=facts; launchButton=launch; }
        void Start()
        {
            var save=SaveService.Instance;
            var q=save != null && save.CurrentData != null && save.CurrentData.campaign != null ? save.CurrentData.campaign.pendingQuest : null;
            contractText.text = q == null ? "暂无进行中的合同\n调度台已准备就绪" : $"合同：{q.eventId}\nTier {q.tier}\n{q.briefingBody}";
            factsText.text = "局外事实\n背包：空\n当前合同判定仅在存活带出后生效";
            launchButton.onClick.AddListener(Launch);
        }
        void OnDestroy() { if (launchButton != null) launchButton.onClick.RemoveListener(Launch); }
        void Launch() { SceneManager.LoadScene("01-Run_ArtFull"); }
    }
}
