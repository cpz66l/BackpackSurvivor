using BS.GamePlay.Run;
using TMPro;
using UnityEngine;

namespace BS.Presentation
{
    public class RunHudView : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;

        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI xpText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI stateText;

        private void Awake()
        {
            if (gameSession == null)
                gameSession = FindAnyObjectByType<GameSession>();
        }
        private void OnEnable()
        {
            if (gameSession == null) return;
            gameSession.OnTimeChanged += HandleTimeChanged;
            gameSession.OnXpChanged += HandleXpChanged;
            gameSession.OnStateChanged += HandleStateChanged;

        }
        private void OnDisable()
        {
            if (gameSession == null) return;
            gameSession.OnTimeChanged -= HandleTimeChanged;
            gameSession.OnXpChanged -= HandleXpChanged;
            gameSession.OnStateChanged -= HandleStateChanged;
        }

        private void Start()
        {
            if (gameSession == null) return;
            //主动刷新一次当前值，防止 HUD 比 GameSession.StartRun() 更晚订阅，漏掉初始广播。
            HandleTimeChanged(gameSession.Elapsed, gameSession.Remaining);
            HandleXpChanged(gameSession.TotalXp, gameSession.Level);
            HandleStateChanged(gameSession.State);
        }

        private void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.NotStarted:
                    stateText.text = "";
                    break;
                case GameState.Running:
                    stateText.text = "";
                    break;
                case GameState.Victory:
                    stateText.text = "VICTORY";
                    break;
                case GameState.Defeat:
                    stateText.text = "DEFEAT";
                    break;
                case GameState.Paused:
                    stateText.text = "PAUSED";
                    break;
            }
        }

        private void HandleTimeChanged(float elapsed ,float remaining)
        {
            int seconds = Mathf.CeilToInt(remaining);
            int minutes = seconds / 60;
            int sec = seconds % 60;
            timeText.text = $"{minutes:00}:{sec:00}";
        }
        private void HandleXpChanged(int totalXp, int level)
        {
            xpText.text = $"XP {totalXp}";
            levelText.text = $"Lv.{level}";
        }
    }
}
