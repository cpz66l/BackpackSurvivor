using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace BS.Presentation
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject aboutPanel;
        [SerializeField] private Button aboutButton;
        [SerializeField] private Button closeAboutButton;
        [SerializeField] private Button gameplayGuideButton;
        [SerializeField] private GameObject gameplayGuidePanel;
        [SerializeField] private Button closeGuideButton;

        [SerializeField] private TMP_Text[] preloadTexts;

        private void Awake()
        {
            if(aboutPanel != null)
                aboutPanel.SetActive(false);
            if (gameplayGuidePanel != null)
                gameplayGuidePanel.SetActive(false);
        }

        private void Start()
        {
            PrewarmPanels();
        }
        private void OnEnable()
        {
            if(startButton != null)
                startButton.onClick.AddListener(StartButton);
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitButton);
            if (aboutButton != null)
                aboutButton.onClick.AddListener(AboutButton);
            if (closeAboutButton != null)
                closeAboutButton.onClick.AddListener(CloseAboutButton);
            if (gameplayGuideButton != null)
                gameplayGuideButton.onClick.AddListener(GameplayGuideButton);
            if (closeGuideButton != null)
                closeGuideButton.onClick.AddListener(CloseGuideButton);
        }

        private void OnDisable()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(StartButton);
            if (quitButton != null)
                quitButton.onClick.RemoveListener(QuitButton);
            if (aboutButton != null)
                aboutButton.onClick.RemoveListener(AboutButton);
            if (closeAboutButton != null)
                closeAboutButton.onClick.RemoveListener(CloseAboutButton);
            if (gameplayGuideButton != null)
                gameplayGuideButton.onClick.RemoveListener(GameplayGuideButton);
            if (closeGuideButton != null)
                closeGuideButton.onClick.RemoveListener(CloseGuideButton);
        }

        private void StartButton()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("01-Run");
        }

        private void QuitButton() => Application.Quit();

        private void AboutButton() => aboutPanel.SetActive(true);

        private void CloseAboutButton() => aboutPanel.SetActive(false);

        private void GameplayGuideButton() => gameplayGuidePanel.SetActive(true);

        private void CloseGuideButton() => gameplayGuidePanel.SetActive(false);
        private void PrewarmPanels()
        {
            bool aboutWasActive = aboutPanel != null && aboutPanel.activeSelf;
            bool guideWasActive = gameplayGuidePanel != null && gameplayGuidePanel.activeSelf;

            if (aboutPanel != null)
                aboutPanel.SetActive(true);

            if (gameplayGuidePanel != null)
                gameplayGuidePanel.SetActive(true);

            foreach (TMP_Text text in preloadTexts)
            {
                if (text == null) continue;
                text.ForceMeshUpdate();
            }

            Canvas.ForceUpdateCanvases();

            if (aboutPanel != null)
                aboutPanel.SetActive(aboutWasActive);

            if (gameplayGuidePanel != null)
                gameplayGuidePanel.SetActive(guideWasActive);
        }
    }
}
