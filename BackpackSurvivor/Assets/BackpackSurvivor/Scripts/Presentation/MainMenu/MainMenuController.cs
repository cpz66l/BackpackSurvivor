using BS.Core;
using System.Collections;
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
        [SerializeField] private Button settingsButton;
        [SerializeField] private SettingsPanelView settingsPanel;

        [SerializeField] private TMP_Text[] preloadTexts;
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private float sceneLoadDelayAfterClick = 0.08f;

        private bool isLeavingScene;

        private void Awake()
        {
            if(aboutPanel != null)
                aboutPanel.SetActive(false);
            if (gameplayGuidePanel != null)
                gameplayGuidePanel.SetActive(false);
            if (sfx == null)
                sfx = FindAnyObjectByType<SfxPlayer>();
            if (uiAudioSource == null)
                uiAudioSource = GetComponent<AudioSource>();
            SettingsService.Apply(SettingsService.Load());
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
            if (settingsButton != null)
                settingsButton.onClick.AddListener(SettingsButton);
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
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(SettingsButton);
        }

        private void StartButton()
        {
            if (isLeavingScene) return;
            isLeavingScene = true;
            PlayButtonClickAcrossScene();
            Time.timeScale = 1f;
            SceneManager.LoadScene("01-Run");
        }

        private void QuitButton()
        {
            if (isLeavingScene) return;
            PlayButtonClick();
            StartCoroutine(QuitAfterClick());
        }

        private void AboutButton()
        {
            PlayButtonClick();
            aboutPanel.SetActive(true);
        }

        private void CloseAboutButton()
        {
            PlayButtonClick();
            aboutPanel.SetActive(false);
        }

        private void GameplayGuideButton()
        {
            PlayButtonClick();
            gameplayGuidePanel.SetActive(true);
        }

        private void CloseGuideButton()
        {
            PlayButtonClick();
            gameplayGuidePanel.SetActive(false);
        }

        public void PlayButtonClick()
        {
            if (sfx != null)
            {
                sfx.PlaySfx(SfxId.ButtonClick);
                return;
            }

            if (buttonClickClip == null) return;
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
                uiAudioSource.playOnAwake = false;
                uiAudioSource.spatialBlend = 0f;
            }
            GameSettings settings = SettingsService.Load();
            float volume = 0.65f * SettingsService.GetEffectiveSfxVolume(settings);
            uiAudioSource.PlayOneShot(buttonClickClip, volume);
        }

        private void PlayButtonClickAcrossScene()
        {
            if (buttonClickClip == null)
            {
                PlayButtonClick();
                return;
            }

            GameObject audioObject = new GameObject("OneShotUIButtonAudio");
            DontDestroyOnLoad(audioObject);

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            GameSettings settings = SettingsService.Load();
            float volume = 0.65f * SettingsService.GetEffectiveSfxVolume(settings);
            source.PlayOneShot(buttonClickClip, volume);

            Destroy(audioObject, buttonClickClip.length + 0.1f);
        }

        private IEnumerator QuitAfterClick()
        {
            isLeavingScene = true;
            yield return new WaitForSecondsRealtime(sceneLoadDelayAfterClick);
            Application.Quit();
        }

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


        private void SettingsButton()
        {
            PlayButtonClick();
            settingsPanel.Open();
        }
    }
}
