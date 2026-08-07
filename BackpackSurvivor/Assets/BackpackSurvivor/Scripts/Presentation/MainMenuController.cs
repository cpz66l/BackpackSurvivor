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


        private void Awake()
        {
            if(aboutPanel != null)
                aboutPanel.SetActive(false);
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
        }

        private void StartButton()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("01-Run");
        }

        private void QuitButton() => Application.Quit();

        private void AboutButton() => aboutPanel.SetActive(true);

        private void CloseAboutButton() => aboutPanel.SetActive(false);
    }
}
