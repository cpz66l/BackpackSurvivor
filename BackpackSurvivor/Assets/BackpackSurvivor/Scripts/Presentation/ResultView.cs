using BS.GamePlay.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        

        private void Awake()
        {
            if(gameSession == null) 
                gameSession = FindAnyObjectByType<GameSession>();

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
            panel.SetActive(false);
        }

        private void HandleRunEnded(RunResult runResult)
        {
            panel.SetActive(true);
            if(runResult.FinalState == GameState.Victory)
            {
                titleText.text = "游戏胜利";
                titleText.color = victoryTitleColor;
            }
            else if(runResult.FinalState == GameState.Defeat)
            {
                titleText.text = "游戏失败";
                titleText.color = defeatTitleColor;
            }

            statsText.text =
                $"存活时间：{FormatTime(runResult.Elapsed)}\r\n" +
                $"等级：{runResult.Level}\r\n" +
                $"总经验：{runResult.TotalXp}\r\n" +
                $"击杀数：{runResult.KillCount}";
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
            Time.timeScale = 1f;
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(buildIndex);
        }

        private void HandleQuitClicked()
        {
            Application.Quit();
        }
    }
}
