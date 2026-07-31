using BS.GamePlay.Combat;
using BS.GamePlay.Player;
using BS.GamePlay.Run;
using BS.GamePlay.Waves;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BS.Presentation
{
    public class RunHudView : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private WaveDirector waveDirector;

        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Image xpLoop;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private TextMeshProUGUI waveText;

        //血量HUD
        [SerializeField] private Health playerHealth;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;

        private void Awake()
        {
            if (gameSession == null)
                gameSession = FindAnyObjectByType<GameSession>();
            if(playerHealth == null)
                playerHealth =  FindAnyObjectByType<PlayerController>()?.GetComponent<Health>();
            if(waveDirector == null)
                waveDirector = FindAnyObjectByType<WaveDirector>();
            //关闭血条Slider可操控
            if (hpSlider != null)
            {
                hpSlider.interactable = false;

                Navigation navigation = hpSlider.navigation;
                navigation.mode = Navigation.Mode.None;
                hpSlider.navigation = navigation;
            }
        }
        private void OnEnable()
        {
            if (gameSession != null) 
                {
                gameSession.OnTimeChanged += HandleTimeChanged;
                gameSession.OnXpChanged += HandleXpChanged;
                gameSession.OnStateChanged += HandleStateChanged;
            }
            if (playerHealth != null)
                playerHealth.OnHealthChanged += HandleHealthChanged;
            if (waveDirector != null)
                waveDirector.OnWaveStageChanged += HandleWaveStageChanged;
        }
        private void OnDisable()
        {
            if (gameSession != null)
            {
                gameSession.OnTimeChanged -= HandleTimeChanged;
                gameSession.OnXpChanged -= HandleXpChanged;
                gameSession.OnStateChanged -= HandleStateChanged;
            }
            if (playerHealth != null)
                playerHealth.OnHealthChanged -= HandleHealthChanged;
            if (waveDirector != null)
                waveDirector.OnWaveStageChanged -= HandleWaveStageChanged;
           
        }

        private void Start()
        {
            if (gameSession != null) 
            {
            //主动刷新一次当前值，防止 HUD 比 GameSession.StartRun() 更晚订阅，漏掉初始广播。
            HandleTimeChanged(gameSession.Elapsed, gameSession.Remaining);
            HandleXpChanged(gameSession.TotalXp, gameSession.Level ,
                gameSession.CurrentXp,gameSession.XpToNextLevel);
            HandleStateChanged(gameSession.State);
            }
            if (playerHealth != null)
                HandleHealthChanged(playerHealth.CurrentHp, playerHealth.MaxHp);
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
                case GameState.LevelUpSelecting:
                    stateText.text = "LEVEL UP";
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

        private void HandleXpChanged(int totalXp, int level,int currentXp ,int xpToNextLevel)
        {
            float ratio = 0f;
            if (xpToNextLevel > 0)
                ratio = Mathf.Clamp01((float)currentXp / xpToNextLevel);
            if(xpLoop != null)
                xpLoop.fillAmount = ratio;
            levelText.text = level.ToString();
        }

        private void HandleHealthChanged(float currentHp ,float maxHp)
        {
            if (maxHp <= 0f) return;

            float ratio = Mathf.Clamp01(currentHp / maxHp);

            if (hpSlider != null)
                hpSlider.normalizedValue = ratio;

            if (hpText != null)
                hpText.text = $"HP {Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";
        }

        private void HandleWaveStageChanged(int stageIndex ,string stageName,Color displayColor)
        {
            if(waveText == null) return;
            waveText.text = $"WAVE {stageIndex + 1} · {stageName}";
            waveText.color = displayColor;
        }
    }
}
