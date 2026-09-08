using BS.GamePlay.Run;
using BS.GamePlay.Upgrades;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BS.Presentation
{
    /// <summary>Owns the selection transaction, independently of the hidden visual root.</summary>
    public class LevelUpChoiceView : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject root;
        // Retain original serialized fields for existing scenes and prefabs.
        [SerializeField] private Button choiceOne;
        [SerializeField] private Button choiceTwo;
        [SerializeField] private Button choiceThree;
        [SerializeField] private TextMeshProUGUI choiceOneTitle;
        [SerializeField] private TextMeshProUGUI choiceTwoTitle;
        [SerializeField] private TextMeshProUGUI choiceThreeTitle;
        [SerializeField] private TextMeshProUGUI choiceOneDescription;
        [SerializeField] private TextMeshProUGUI choiceTwoDescription;
        [SerializeField] private TextMeshProUGUI choiceThreeDescription;

        [Header("Tactical presentation (optional)")]
        [SerializeField] private UpgradeChoiceCard[] cards;
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TextMeshProUGUI progressLabel;
        [SerializeField] private Image progressFill;
        [SerializeField] private CanvasGroup modalGroup;
        [SerializeField] private RectTransform dialog;
        [SerializeField] private float cardSpacing = 420f;
        [SerializeField] private float progressBarWidth = 254f;
        [SerializeField] private string levelLabelPrefix = "LV. ";
        [SerializeField] private CanvasGroup[] coveredHudGroups;
        private readonly List<HudSnapshot> hudSnapshots = new List<HudSnapshot>();
        private bool hudIsCovered;
        private List<LevelUpOption> currentOptions;
        private GameObject previousSelection;
        private GameSession subscribedSession;
        private bool isOpen;
        private bool selectionCommitted;
        private int focusedIndex;
        private int openedFrame;
        public bool IsOpen => isOpen;
        public int VisibleOptionCount => currentOptions == null ? 0 : currentOptions.Count;

        private struct HudSnapshot
        {
            public CanvasGroup group;
            public float alpha;
            public bool interactable;
            public bool blocksRaycasts;
        }

        public void Configure(GameSession session, GameObject visualRoot, UpgradeChoiceCard[] choiceCards,
            TextMeshProUGUI level, TextMeshProUGUI progress, Image fill, CanvasGroup group, RectTransform content)
        {
            gameSession = session; root = visualRoot; cards = choiceCards;
            levelLabel = level; progressLabel = progress; progressFill = fill;
            modalGroup = group; dialog = content;
            if (Application.isPlaying) { Unsubscribe(); BindCards(); Subscribe(); Close(); }
        }
        public void ConfigureLayout(float choiceSpacing, string levelPrefix)
        {
            cardSpacing = choiceSpacing;
            levelLabelPrefix = levelPrefix;
        }
        public void ConfigureCoveredHud(CanvasGroup[] groups)
        {
            RestoreHud();
            coveredHudGroups = groups;
            if (isOpen) CoverHud();
        }
        private void Awake()
        {
            if (gameSession == null) gameSession = FindAnyObjectByType<GameSession>();
            BindCards(); Close();
        }
        private void OnEnable()
        {
            Subscribe();
            if (choiceOne != null) choiceOne.onClick.AddListener(SelectChoiceOne);
            if (choiceTwo != null) choiceTwo.onClick.AddListener(SelectChoiceTwo);
            if (choiceThree != null) choiceThree.onClick.AddListener(SelectChoiceThree);
        }
        private void OnDisable()
        {
            Unsubscribe();
            Close();
            if (choiceOne != null) choiceOne.onClick.RemoveListener(SelectChoiceOne);
            if (choiceTwo != null) choiceTwo.onClick.RemoveListener(SelectChoiceTwo);
            if (choiceThree != null) choiceThree.onClick.RemoveListener(SelectChoiceThree);
        }
        private void Subscribe()
        {
            if (gameSession == null) gameSession = FindAnyObjectByType<GameSession>();
            if (gameSession == null || subscribedSession == gameSession) return;
            subscribedSession = gameSession;
            subscribedSession.OnLevelUpChoiceRequested += HandleLevelUpChoiceRequested;
            subscribedSession.OnStateChanged += HandleStateChanged;
        }
        private void Unsubscribe()
        {
            if (subscribedSession == null) return;
            subscribedSession.OnLevelUpChoiceRequested -= HandleLevelUpChoiceRequested;
            subscribedSession.OnStateChanged -= HandleStateChanged;
            subscribedSession = null;
        }
        private void BindCards()
        {
            if (cards == null) return;
            for (int i = 0; i < cards.Length; i++) if (cards[i] != null) cards[i].Bind(this, i);
        }
        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.LevelUpSelecting && isOpen) Close();
        }
        private void HandleLevelUpChoiceRequested(List<LevelUpOption> options)
        {
            // Copy the offer so later modifications to the producer's list cannot alter a click.
            currentOptions = new List<LevelUpOption>(3);
            if (options != null)
                for (int i = 0; i < options.Count && currentOptions.Count < 3; i++)
                    if (options[i] != null) currentOptions.Add(options[i]);
            if (currentOptions.Count == 0)
            {
                Close(); gameSession?.CompleteLevelUpChoice(); return;
            }
            selectionCommitted = false;
            BindCards();
            for (int i = 0; i < 3; i++)
            {
                LevelUpOption option = i < currentOptions.Count ? currentOptions[i] : null;
                if (cards != null && i < cards.Length && cards[i] != null)
                {
                    cards[i].gameObject.SetActive(option != null);
                    if (option != null)
                    {
                        cards[i].Show(option, i + 1);
                        RectTransform cardRect = cards[i].transform as RectTransform;
                        if (cardRect != null)
                            cardRect.anchoredPosition = new Vector2((i - (currentOptions.Count - 1) * .5f) * cardSpacing, cardRect.anchoredPosition.y);
                    }
                }
                Button legacy = LegacyButton(i);
                if (legacy != null) { legacy.gameObject.SetActive(option != null); legacy.interactable = option != null; }
                TextMeshProUGUI title = i == 0 ? choiceOneTitle : i == 1 ? choiceTwoTitle : choiceThreeTitle;
                TextMeshProUGUI description = i == 0 ? choiceOneDescription : i == 1 ? choiceTwoDescription : choiceThreeDescription;
                if (title != null) title.text = option == null ? "" : option.Title;
                if (description != null) description.text = option == null ? "" : option.Description;
            }
            if (gameSession != null)
            {
                if (levelLabel != null) levelLabel.text = levelLabelPrefix + gameSession.Level.ToString("00");
                if (progressLabel != null) progressLabel.text = "下一级经验  " + gameSession.CurrentXp + " / " + gameSession.XpToNextLevel;
                if (progressFill != null)
                {
                    float normalized = Mathf.Clamp01((float)gameSession.CurrentXp / Mathf.Max(1, gameSession.XpToNextLevel));
                    progressFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progressBarWidth * normalized);
                }
            }
            Open();
        }
        /// <summary>Shared button and keyboard entry point; GameSession alone applies stats and resumes time.</summary>
        public void SelectChoice(int index)
        {
            if (!isOpen || selectionCommitted || currentOptions == null || index < 0 || index >= currentOptions.Count) return;
            if (gameSession == null || gameSession.State != GameState.LevelUpSelecting) return;
            LevelUpOption option = currentOptions[index];
            selectionCommitted = true;
            gameSession.ChooseLevelUpOption(option);
            if (gameSession.State == GameState.LevelUpSelecting) { selectionCommitted = false; return; }
            Close();
        }
        private void SelectChoiceOne() { SelectChoice(0); }
        private void SelectChoiceTwo() { SelectChoice(1); }
        private void SelectChoiceThree() { SelectChoice(2); }
        private Button LegacyButton(int index) { return index == 0 ? choiceOne : index == 1 ? choiceTwo : choiceThree; }
        private Button ChoiceButton(int index)
        {
            return cards != null && index < cards.Length && cards[index] != null ? cards[index].Button : LegacyButton(index);
        }
        private void Open()
        {
            if (!isOpen && EventSystem.current != null) previousSelection = EventSystem.current.currentSelectedGameObject;
            CoverHud();
            isOpen = true; openedFrame = Time.frameCount; focusedIndex = -1;
            SetRootVisible(true);
            if (modalGroup != null) modalGroup.alpha = 0;
            FitDialog();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        private void Close()
        {
            bool restoreFocus = isOpen;
            isOpen = false; currentOptions = null;
            SetRootVisible(false);
            RestoreHud();
            if (restoreFocus && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            previousSelection = null;
        }
        private void CoverHud()
        {
            if (hudIsCovered) return;
            hudSnapshots.Clear();
            if (coveredHudGroups != null)
                foreach (CanvasGroup group in coveredHudGroups)
                {
                    if (group == null || group == modalGroup) continue;
                    // Do not hide the modal or its always-active controller if a binding is edited incorrectly.
                    if (transform == group.transform || transform.IsChildOf(group.transform)) continue;
                    if (root != null && (root.transform == group.transform || root.transform.IsChildOf(group.transform))) continue;
                    hudSnapshots.Add(new HudSnapshot
                    {
                        group = group, alpha = group.alpha,
                        interactable = group.interactable, blocksRaycasts = group.blocksRaycasts
                    });
                    group.interactable = false;
                    group.blocksRaycasts = false;
                }
            hudIsCovered = true;
        }
        private void FadeCoveredHud()
        {
            if (!hudIsCovered) return;
            for (int i = 0; i < hudSnapshots.Count; i++)
            {
                CanvasGroup group = hudSnapshots[i].group;
                if (group == null) continue;
                group.alpha = Mathf.MoveTowards(group.alpha, 0, Time.unscaledDeltaTime * 10f);
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }
        private void RestoreHud()
        {
            if (!hudIsCovered) return;
            for (int i = 0; i < hudSnapshots.Count; i++)
            {
                HudSnapshot snapshot = hudSnapshots[i];
                if (snapshot.group == null) continue;
                snapshot.group.alpha = snapshot.alpha;
                snapshot.group.interactable = snapshot.interactable;
                snapshot.group.blocksRaycasts = snapshot.blocksRaycasts;
            }
            hudSnapshots.Clear();
            hudIsCovered = false;
        }
        private void SetRootVisible(bool visible)
        {
            if (root == null) return;
            // Legacy prefabs may put this component on the root itself. Keep it active to receive future events.
            bool rootContainsController = transform == root.transform || transform.IsChildOf(root.transform);
            if (rootContainsController)
            {
                if (modalGroup == null) modalGroup = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();
            }
            else root.SetActive(visible);
            if (modalGroup != null)
            {
                modalGroup.alpha = visible ? 1 : 0;
                modalGroup.interactable = visible; modalGroup.blocksRaycasts = visible;
            }
        }
        private void FocusChoice(int index)
        {
            if (currentOptions == null || currentOptions.Count == 0) return;
            focusedIndex = (index + currentOptions.Count) % currentOptions.Count;
            Button button = ChoiceButton(focusedIndex);
            if (button != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
        private void FitDialog()
        {
            if (dialog == null || root == null) return;
            RectTransform area = root.transform as RectTransform;
            if (area == null) return;
            float scale = Mathf.Min(1f, Mathf.Min((area.rect.width - 56f) / Mathf.Max(1, dialog.rect.width), (area.rect.height - 56f) / Mathf.Max(1, dialog.rect.height)));
            scale = Mathf.Max(.25f, scale);
            if (!Mathf.Approximately(dialog.localScale.x, scale)) dialog.localScale = Vector3.one * scale;
        }
        private void Update()
        {
            if (!isOpen) return;
            FadeCoveredHud();
            FitDialog();
            if (modalGroup != null) modalGroup.alpha = Mathf.MoveTowards(modalGroup.alpha, 1f, Time.unscaledDeltaTime * 9f);
            if (Time.frameCount == openedFrame) return;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            // Background clicks can clear EventSystem selection. Never confirm a stale cached focus.
            focusedIndex = -1;
            if (EventSystem.current != null)
                for (int i = 0; i < VisibleOptionCount; i++)
                    if (ChoiceButton(i) != null && EventSystem.current.currentSelectedGameObject == ChoiceButton(i).gameObject)
                        focusedIndex = i;
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) SelectChoice(0);
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) SelectChoice(1);
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) SelectChoice(2);
            else if (keyboard.leftArrowKey.wasPressedThisFrame) FocusChoice(focusedIndex < 0 ? VisibleOptionCount - 1 : focusedIndex - 1);
            else if (keyboard.rightArrowKey.wasPressedThisFrame) FocusChoice(focusedIndex < 0 ? 0 : focusedIndex + 1);
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                if (focusedIndex >= 0) SelectChoice(focusedIndex);
            }
#endif
        }
    }
}
