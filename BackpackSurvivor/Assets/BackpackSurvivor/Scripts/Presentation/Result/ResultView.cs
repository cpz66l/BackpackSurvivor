using BS.GamePlay.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;

namespace BS.Presentation
{
    public class ResultView : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;

        [SerializeField] private Color victoryTitleColor;
        [SerializeField] private Color defeatTitleColor;

        [SerializeField] private TMP_Text statsText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private float sceneLoadDelayAfterClick = 0.08f;

        [Header("V0.4 presentation (optional)")]
        [SerializeField] private RectTransform dialog;
        [SerializeField] private TMP_Text subtitleText;
        // Elapsed, level, kills, gold, backpack value, legendary count: existing snapshot only.
        [SerializeField] private TMP_Text[] statisticValues;
        [SerializeField] private TMP_Text legendarySummaryText;
        [SerializeField] private TMP_Text questOutcomeText;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string restartSceneFallback = "01-Run";

        private bool isLeavingScene;
        private Button[] menuButtons;
        private int openedFrame;

        public void ConfigurePresentation(GameSession session, GameObject visualRoot, RectTransform content,
            TMP_Text title, TMP_Text subtitle, TMP_Text[] values, TMP_Text legendarySummary,
            Button restart, Button mainMenu, Color victoryColor, Color defeatColor, TMP_Text questOutcome = null)
        {
            gameSession = session; panel = visualRoot; dialog = content;
            titleText = title; subtitleText = subtitle; statisticValues = values;
            legendarySummaryText = legendarySummary; statsText = null;
            questOutcomeText = questOutcome;
            restartButton = restart; quitButton = mainMenu;
            victoryTitleColor = victoryColor; defeatTitleColor = defeatColor;
            menuButtons = new[] { restartButton, quitButton };
        }

        private void Awake()
        {
            if(gameSession == null) 
                gameSession = FindAnyObjectByType<GameSession>();
            if (sfx == null)
                sfx = FindAnyObjectByType<SfxPlayer>();
            menuButtons = new[] { restartButton, quitButton };
        }

        private void OnEnable()
        {
            if(gameSession != null)
                gameSession.OnRunEnded += HandleRunEnded;
            if(restartButton != null) 
                restartButton.onClick.AddListener(HandleRestartClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(HandleQuitClicked);
        }

        private void OnDisable()
        {
            if (gameSession != null)
                gameSession.OnRunEnded -= HandleRunEnded;
            if (restartButton != null)
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            if (quitButton != null)
                quitButton.onClick.RemoveListener(HandleQuitClicked);
        }

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            if (panel == null || !panel.activeInHierarchy || dialog == null) return;
            RunMenuPresentation.FitDialog(panel.transform as RectTransform, dialog);
            if (!isLeavingScene) RunMenuPresentation.Navigate(menuButtons, true, openedFrame);
        }

        private void HandleRunEnded(RunResult runResult)
        {
            if (panel != null) panel.SetActive(true);
            openedFrame = Time.frameCount;
            isLeavingScene = false;
            if (restartButton != null) restartButton.interactable = true;
            if (quitButton != null) quitButton.interactable = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            RunMenuPresentation.FitDialog(panel != null ? panel.transform as RectTransform : null, dialog);
            if(runResult.FinalState == GameState.Victory)
            {
                if (titleText != null) { titleText.text = "行动成功"; titleText.color = victoryTitleColor; }
                if (subtitleText != null) subtitleText.text = "已完成本次行动";
                sfx?.PlaySfx(SfxId.GameVictory);
            }
            else if(runResult.FinalState == GameState.Defeat)
            {
                if (titleText != null) { titleText.text = "行动失败"; titleText.color = defeatTitleColor; }
                if (subtitleText != null) subtitleText.text = "本次行动已结束";
                sfx?.PlaySfx(SfxId.GameDefeat);
            }

            if (statsText != null) statsText.text =
                $"存活时间：{FormatTime(runResult.Elapsed)}\r\n" +
                $"等级：{runResult.Level}\r\n" +
                $"总经验：{runResult.TotalXp}\r\n" +
                $"击杀数：{runResult.KillCount}\r\n" +
                $"背包价值：￥{runResult.BackpackValue}";

            string[] values =
            {
                FormatTime(runResult.Elapsed), runResult.Level.ToString(), runResult.KillCount.ToString("N0"),
                "￥" + runResult.TotalGold.ToString("N0"), "￥" + runResult.BackpackValue.ToString("N0"),
                runResult.LegendaryFoundCount.ToString("N0")
            };
            if (statisticValues != null)
                for (int i = 0; i < statisticValues.Length && i < values.Length; i++)
                    if (statisticValues[i] != null) statisticValues[i].text = values[i];
            if (legendarySummaryText != null)
                legendarySummaryText.text = $"总经验  {runResult.TotalXp:N0}    ·    传说装备价值  ￥{runResult.LegendaryCollectedValue:N0}";
            if (questOutcomeText != null)
            {
                var outcome = gameSession == null ? null : gameSession.LastQuestOutcome;
                questOutcomeText.text = outcome == null ? "当前没有进行中的合同" : (outcome.Completed ? "合同已完成" : "合同未达成，已保留") + $" · 进度 {outcome.Progress01:P0}" + FormatQuestItems();
            }
        }
        string FormatQuestItems()
        {
            var snapshot = gameSession == null ? null : gameSession.LastQuestSnapshot;
            if (snapshot == null || snapshot.items == null || snapshot.items.Count == 0) return "\n带出物品：空";
            return "\n带出物品：" + string.Join("、", snapshot.items.Select(i => (i.questOnly ? "★" : "") + i.id + " Lv" + i.level).ToArray());
        }
        //计算显示时间
        private string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);//向下取整
            int minutes = totalSeconds / 60;
            int sec = totalSeconds % 60;
            return $"{minutes:00}:{sec:00}";
        }

        private void HandleRestartClicked()
        {
            if (isLeavingScene) return;
            BeginSceneLoad("Camp");
        }

        private void HandleQuitClicked()
        {
            if (isLeavingScene) return;
            BeginSceneLoad("Camp");
        }

        private void BeginSceneLoad(string sceneName)
        {
            isLeavingScene = true;
            if (restartButton != null) restartButton.interactable = false;
            if (quitButton != null) quitButton.interactable = false;
            sfx?.PlaySfx(SfxId.ButtonClick);
            StartCoroutine(LoadSceneAfterClick(sceneName));
        }

        private IEnumerator LoadSceneAfterClick(string sceneName)
        {
            yield return new WaitForSecondsRealtime(sceneLoadDelayAfterClick);
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
