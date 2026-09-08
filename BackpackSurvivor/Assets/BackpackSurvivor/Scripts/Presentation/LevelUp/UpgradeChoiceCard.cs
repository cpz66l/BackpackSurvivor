using BS.GamePlay.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BS.Presentation
{
    /// <summary>Illustrated UGUI card with shared sprites and unscaled-time hover feedback.</summary>
    public sealed class UpgradeChoiceCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image surface;
        [SerializeField] private Image border;
        [SerializeField] private Image accentStrip;
        [SerializeField] private Image actionSurface;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI effect;
        [SerializeField] private TextMeshProUGUI category;
        [SerializeField] private TextMeshProUGUI number;
        [SerializeField] private TextMeshProUGUI shortcut;
        [SerializeField] private TextMeshProUGUI actionLabel;
        [SerializeField] private UpgradeCategoryGraphic icon;
        [SerializeField] private UpgradeRoundedGraphic roundedPanel;
        [SerializeField] private Graphic shortAccent;
        [SerializeField] private Image illustration;
        [SerializeField] private IllustrationBinding[] illustrations;
        private LevelUpChoiceView owner;
        private int index;
        private bool hovered, focused;
        private Color accent = new Color(.46f, .77f, .86f);
        private float emphasis;
        public Button Button => button;

        [System.Serializable]
        public struct IllustrationBinding
        {
            public LevelUpOptionId id;
            public Sprite sprite;
            public IllustrationBinding(LevelUpOptionId optionId, Sprite optionSprite) { id = optionId; sprite = optionSprite; }
        }

        public void Configure(Button click, Image background, Image outline, Image stripe, Image action,
            TextMeshProUGUI heading, TextMeshProUGUI detail, TextMeshProUGUI type, TextMeshProUGUI ordinal,
            TextMeshProUGUI key, TextMeshProUGUI selectLabel, UpgradeCategoryGraphic categoryIcon)
        {
            button = click; surface = background; border = outline; accentStrip = stripe; actionSurface = action;
            title = heading; effect = detail; category = type; number = ordinal; shortcut = key;
            actionLabel = selectLabel; icon = categoryIcon;
        }
        public void ConfigureIllustrated(Button click, UpgradeRoundedGraphic panel, Graphic stripe, Image artwork,
            TextMeshProUGUI heading, TextMeshProUGUI detail, TextMeshProUGUI type, TextMeshProUGUI key,
            TextMeshProUGUI selectLabel, UpgradeCategoryGraphic fallback, IllustrationBinding[] artworkSet)
        {
            button = click; roundedPanel = panel; shortAccent = stripe; illustration = artwork;
            title = heading; effect = detail; category = type; shortcut = key; actionLabel = selectLabel;
            icon = fallback; illustrations = artworkSet;
            surface = border = accentStrip = actionSurface = null;
            number = null;
        }
        public void Bind(LevelUpChoiceView view, int optionIndex)
        {
            owner = view; index = optionIndex;
            if (button == null) return;
            button.onClick.RemoveListener(Select); button.onClick.AddListener(Select);
        }
        public void Show(LevelUpOption option, int ordinal)
        {
            hovered = focused = false; emphasis = 0;
            accent = CategoryColor(option.Category);
            if (title != null) title.text = option.Title;
            if (effect != null) { effect.text = option.Description; effect.color = new Color(.94f, .96f, .98f, 1); }
            if (category != null) { category.text = CategoryName(option.Category); category.color = new Color(.70f, .78f, .85f, 1); }
            if (number != null) number.text = ordinal.ToString("00");
            if (shortcut != null) shortcut.text = ordinal.ToString();
            Sprite artwork = FindIllustration(option.Id);
            if (illustration != null)
            {
                illustration.sprite = artwork;
                illustration.preserveAspect = true;
                illustration.enabled = artwork != null;
            }
            if (icon != null)
            {
                icon.Category = option.Category; icon.color = accent;
                icon.gameObject.SetActive(artwork == null);
            }
            if (button != null) button.interactable = true;
            Paint();
        }
        private void Select() { if (owner != null) owner.SelectChoice(index); }
        private Sprite FindIllustration(LevelUpOptionId id)
        {
            if (illustrations == null) return null;
            for (int i = 0; i < illustrations.Length; i++) if (illustrations[i].id == id) return illustrations[i].sprite;
            return null;
        }
        public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; }
        public void OnSelect(BaseEventData eventData) { focused = true; }
        public void OnDeselect(BaseEventData eventData) { focused = false; }
        private void OnDisable() { hovered = focused = false; }
        private void Update()
        {
            float next = Mathf.MoveTowards(emphasis, hovered || focused ? 1f : 0f, Time.unscaledDeltaTime * 7f);
            if (Mathf.Approximately(next, emphasis)) return;
            emphasis = next; Paint();
        }
        private void Paint()
        {
            Color fill = Color.Lerp(new Color(.066f, .108f, .15f, .965f), new Color(.089f, .14f, .19f, .985f), emphasis);
            Color stroke = Color.Lerp(new Color(.36f, .48f, .59f, .9f), new Color(.55f, .70f, .81f, 1), emphasis);
            if (roundedPanel != null) roundedPanel.SetColors(fill, stroke);
            if (surface != null) surface.color = fill;
            if (border != null) border.color = stroke;
            if (accentStrip != null) accentStrip.color = accent;
            if (shortAccent != null) shortAccent.color = accent;
            if (actionSurface != null) actionSurface.color = new Color(.075f, .12f, .16f, .65f);
            if (actionLabel != null) actionLabel.color = Color.Lerp(new Color(.65f, .75f, .84f, 1), new Color(.82f, .90f, .96f, 1), emphasis);
        }
        public static string CategoryName(LevelUpOptionCategory type)
        {
            switch (type)
            {
                case LevelUpOptionCategory.Attack: return "火力";
                case LevelUpOptionCategory.Survival: return "生存";
                case LevelUpOptionCategory.Mobility: return "机动";
                case LevelUpOptionCategory.Loot: return "回收";
                default: return "构筑";
            }
        }
        public static Color CategoryColor(LevelUpOptionCategory type)
        {
            switch (type)
            {
                case LevelUpOptionCategory.Attack: return new Color(.98f, .75f, .29f, 1);
                case LevelUpOptionCategory.Survival: return new Color(.37f, .85f, .56f, 1);
                case LevelUpOptionCategory.Mobility: return new Color(.35f, .72f, .98f, 1);
                case LevelUpOptionCategory.Loot: return new Color(.72f, .65f, .92f, 1);
                default: return new Color(.56f, .79f, .84f, 1);
            }
        }
    }
}
