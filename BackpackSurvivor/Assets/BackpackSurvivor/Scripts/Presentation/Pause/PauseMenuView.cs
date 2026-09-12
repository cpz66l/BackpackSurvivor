using System.Collections;
using BS.GamePlay.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BS.Presentation
{
    /// <summary>Pause presentation only. GameSession retains ownership of pause state and the Escape key.</summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform dialog;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string restartSceneFallback = "01-Run";
        [SerializeField] private float sceneLoadDelayAfterClick = .08f;
        private bool isLeavingScene;
        private int openedFrame;
        private Button[] menuButtons;

        public void Configure(GameSession session, GameObject visualRoot, RectTransform content,
            Button resume, Button restart, Button mainMenu)
        {
            gameSession = session; panel = visualRoot; dialog = content;
            continueButton = resume; restartButton = restart; mainMenuButton = mainMenu;
            menuButtons = new[] { continueButton, restartButton, mainMenuButton };
        }

        private void Awake()
        {
            if (gameSession == null) gameSession = FindAnyObjectByType<GameSession>();
            if (sfx == null) sfx = FindAnyObjectByType<SfxPlayer>();
            menuButtons = new[] { continueButton, restartButton, mainMenuButton };
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (gameSession != null) gameSession.OnStateChanged += HandleStateChanged;
            if (continueButton != null) continueButton.onClick.AddListener(HandleContinueClicked);
            if (restartButton != null) restartButton.onClick.AddListener(HandleRestartClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        }

        private void Start()
        {
            if (gameSession != null) HandleStateChanged(gameSession.State);
        }

        private void OnDisable()
        {
            if (gameSession != null) gameSession.OnStateChanged -= HandleStateChanged;
            if (continueButton != null) continueButton.onClick.RemoveListener(HandleContinueClicked);
            if (restartButton != null) restartButton.onClick.RemoveListener(HandleRestartClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
            if (panel != null) panel.SetActive(false);
        }

        private void HandleStateChanged(GameState state)
        {
            bool visible = state == GameState.Paused;
            if (panel == null || panel.activeSelf == visible) return;
            panel.SetActive(visible);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (!visible) return;
            openedFrame = Time.frameCount;
            RunMenuPresentation.FitDialog(panel.transform as RectTransform, dialog);
        }

        private void Update()
        {
            if (panel == null || !panel.activeInHierarchy) return;
            RunMenuPresentation.FitDialog(panel.transform as RectTransform, dialog);
            if (!isLeavingScene) RunMenuPresentation.Navigate(menuButtons, false, openedFrame);
        }

        private void HandleContinueClicked()
        {
            if (isLeavingScene || gameSession == null || gameSession.State != GameState.Paused) return;
            sfx?.PlaySfx(SfxId.ButtonClick);
            gameSession.ResumeRun();
        }

        private void HandleRestartClicked()
        {
            if (isLeavingScene || gameSession == null || gameSession.State != GameState.Paused) return;
            BeginSceneLoad("Camp");
        }

        private void HandleMainMenuClicked()
        {
            if (isLeavingScene || gameSession == null || gameSession.State != GameState.Paused) return;
            BeginSceneLoad(mainMenuSceneName);
        }

        private void BeginSceneLoad(string sceneName)
        {
            isLeavingScene = true;
            foreach (Button button in menuButtons) if (button != null) button.interactable = false;
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

    /// <summary>Shared display sizing and focus movement; submit stays with the existing UI input module.</summary>
    internal static class RunMenuPresentation
    {
        public static void FitDialog(RectTransform area, RectTransform dialog)
        {
            if (area == null || dialog == null) return;
            float scale = Mathf.Min(1f, Mathf.Min((area.rect.width - 64f) / Mathf.Max(1, dialog.rect.width),
                (area.rect.height - 64f) / Mathf.Max(1, dialog.rect.height)));
            scale = Mathf.Max(.25f, scale);
            if (!Mathf.Approximately(dialog.localScale.x, scale)) dialog.localScale = Vector3.one * scale;
        }

        public static void Navigate(Button[] buttons, bool horizontal, int openedFrame)
        {
#if ENABLE_INPUT_SYSTEM
            if (Time.frameCount == openedFrame || EventSystem.current == null || buttons == null || buttons.Length == 0) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            bool previous = horizontal ? keyboard.leftArrowKey.wasPressedThisFrame : keyboard.upArrowKey.wasPressedThisFrame;
            bool next = horizontal ? keyboard.rightArrowKey.wasPressedThisFrame : keyboard.downArrowKey.wasPressedThisFrame;
            if (!previous && !next) return;
            int current = -1;
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] != null && buttons[i].gameObject == EventSystem.current.currentSelectedGameObject) current = i;
            int index = current < 0 ? (previous ? buttons.Length - 1 : 0) : (current + (previous ? -1 : 1) + buttons.Length) % buttons.Length;
            Button target = buttons[index];
            if (target != null && target.isActiveAndEnabled && target.IsInteractable()) EventSystem.current.SetSelectedGameObject(target.gameObject);
#endif
        }
    }
}
