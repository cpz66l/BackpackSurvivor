using System;
using System.Collections.Generic;
using BS.GamePlay.Run;
using BS.GamePlay.Upgrades;
using BS.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackSurvivor.EditorTools
{
    /// <summary>Builds the approved, minimal illustrated upgrade cards while preserving gameplay rules.</summary>
    public static class NightUpgradeUIBuilder
    {
        const string RootName = "NightUpgradeChoice";
        const string ControllerName = "NightUpgradeController";
        const string FontPath = "Assets/BackpackSurvivor/Art/Font/SourceHanSansCN-Normal SDF.asset";
        public const string IllustrationsPath = "Assets/BackpackSurvivor/Art/UI/Upgrades/UpgradeIllustrations.png";
        static readonly Color Muted = new Color(.65f, .75f, .84f, 1);
        static readonly Color White = new Color(.94f, .96f, .98f, 1);
        static readonly Color Line = new Color(.36f, .48f, .59f, .9f);
        static TMP_FontAsset font;

        [MenuItem("Tools/Backpack Survivor/Art/Apply Night Upgrade UI")]
        public static void ApplyToActiveScene()
        {
            GameSession session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            if (session == null) throw new InvalidOperationException("Open a gameplay scene containing GameSession first.");
            LevelUpChoiceView oldView = UnityEngine.Object.FindAnyObjectByType<LevelUpChoiceView>(FindObjectsInactive.Include);
            Canvas canvas = oldView != null ? oldView.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                foreach (Canvas candidate in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (candidate.isRootCanvas && candidate.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = candidate; break; }
            if (canvas == null) throw new InvalidOperationException("A screen-space gameplay Canvas is required.");
            Build(canvas.transform, session);
            EditorSceneManager.MarkSceneDirty(session.gameObject.scene);
        }

        public static GameObject Build(Transform existingCanvas, GameSession session)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build the upgrade UI outside Play Mode.");
            if (existingCanvas == null || existingCanvas.GetComponent<Canvas>() == null || session == null)
                throw new ArgumentException("Pass the existing gameplay Canvas and GameSession.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidOperationException("Missing shared Chinese UI font: " + FontPath);
            // Validate every option illustration before changing any existing scene objects.
            UpgradeIllustrationImporter.Import();
            UpgradeChoiceCard.IllustrationBinding[] illustrations = LoadIllustrations();

            foreach (LevelUpChoiceView oldView in existingCanvas.GetComponentsInChildren<LevelUpChoiceView>(true))
            {
                SerializedObject oldData = new SerializedObject(oldView);
                SerializedProperty oldRoot = oldData.FindProperty("root");
                GameObject oldVisual = oldRoot != null ? oldRoot.objectReferenceValue as GameObject : null;
                if (oldVisual != null && oldVisual != existingCanvas.gameObject) oldVisual.SetActive(false);
                oldView.enabled = false;
                EditorUtility.SetDirty(oldView);
            }
            RemoveExisting(existingCanvas, RootName);
            RemoveExisting(existingCanvas, ControllerName);
            CanvasGroup[] coveredHud = PrepareCoveredHud(existingCanvas);

            RectTransform root = Rect(RootName, existingCanvas, 0, 0, 0, 0);
            Stretch(root, 0);
            Canvas topCanvas = root.gameObject.AddComponent<Canvas>();
            topCanvas.overrideSorting = true; topCanvas.sortingOrder = 200;
            root.gameObject.AddComponent<GraphicRaycaster>();
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            Image dimmer = root.gameObject.AddComponent<Image>();
            dimmer.color = new Color(.025f, .045f, .075f, .61f);
            dimmer.raycastTarget = true;

            // Intentionally no surrounding panel: only the heading, three cards, and one footer line.
            RectTransform dialog = Rect("TacticalUpgradeDialog", root, 0, 0, 1360, 848);
            Text("Heading", dialog, "选择一项强化", 0, 362, 1252, 88, 64, White, FontStyles.Bold);
            TextMeshProUGUI level = Text("Level", dialog, "等级提升 · LV. 02", 0, 294, 1252, 48, 29, Muted);
            UpgradeChoiceCard[] cards = new UpgradeChoiceCard[3];
            for (int i = 0; i < cards.Length; i++) cards[i] = BuildCard(dialog, i, illustrations);
            Text("FooterInstruction", dialog, "点击卡片或按 1 / 2 / 3 选择", 0, -366, 1252, 48, 28, Muted);

            GameObject controller = new GameObject(ControllerName);
            controller.transform.SetParent(existingCanvas, false);
            controller.layer = 5;
            LevelUpChoiceView view = controller.AddComponent<LevelUpChoiceView>();
            view.Configure(session, root.gameObject, cards, level, null, null, group, dialog);
            view.ConfigureLayout(426f, "等级提升 · LV. ");
            view.ConfigureCoveredHud(coveredHud);
            EditorUtility.SetDirty(view);
            root.gameObject.SetActive(false);
            root.SetAsLastSibling();
            return root.gameObject;
        }

        static UpgradeChoiceCard.IllustrationBinding[] LoadIllustrations()
        {
            Dictionary<string, Sprite> byName = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(IllustrationsPath))
            {
                Sprite sprite = asset as Sprite;
                if (sprite != null) byName[sprite.name] = sprite;
            }
            List<UpgradeChoiceCard.IllustrationBinding> bindings = new List<UpgradeChoiceCard.IllustrationBinding>();
            List<string> missing = new List<string>();
            foreach (LevelUpOptionId id in Enum.GetValues(typeof(LevelUpOptionId)))
            {
                Sprite sprite;
                if (!byName.TryGetValue(id.ToString(), out sprite)) missing.Add(id.ToString());
                else bindings.Add(new UpgradeChoiceCard.IllustrationBinding(id, sprite));
            }
            if (missing.Count > 0)
                throw new InvalidOperationException("Upgrade illustration atlas must contain one named Sprite per option. Missing: " + string.Join(", ", missing.ToArray()) + ". Atlas: " + IllustrationsPath);
            return bindings.ToArray();
        }

        static CanvasGroup[] PrepareCoveredHud(Transform canvas)
        {
            List<CanvasGroup> groups = new List<CanvasGroup>();
            foreach (Transform child in canvas)
            {
                if (child.name == RootName || child.name == ControllerName) continue;
                if (!(child is RectTransform) || child.GetComponentInChildren<Graphic>(true) == null) continue;
                CanvasGroup group = child.GetComponent<CanvasGroup>();
                if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
                groups.Add(group);
            }
            return groups.ToArray();
        }

        static UpgradeChoiceCard BuildCard(Transform parent, int index, UpgradeChoiceCard.IllustrationBinding[] illustrations)
        {
            RectTransform rect = Rect("Choice_" + (index + 1), parent, (index - 1) * 426, -27, 400, 550);
            UpgradeRoundedGraphic panel = Rounded(rect, new Color(.066f, .108f, .15f, .965f), 10, 1.5f, Line);
            panel.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = panel; button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            UpgradeChoiceCard card = rect.gameObject.AddComponent<UpgradeChoiceCard>();

            RectTransform stripeRect = Rect("CategoryAccent", rect, -146, 230, 32, 8);
            UpgradeRoundedGraphic stripe = Rounded(stripeRect, Color.white, 4, 0, Color.clear);
            TextMeshProUGUI category = Text("Category", rect, "火力", -24, 229, 180, 42, 28, Muted, FontStyles.Normal, TextAlignmentOptions.Left);
            RectTransform artRect = Rect("OptionIllustration", rect, 0, 87, 328, 224);
            Image art = artRect.gameObject.AddComponent<Image>();
            art.color = Color.white; art.preserveAspect = true; art.raycastTarget = false;
            RectTransform fallbackRect = Rect("CategoryIconFallback", rect, 0, 87, 86, 86);
            fallbackRect.gameObject.AddComponent<CanvasRenderer>();
            UpgradeCategoryGraphic fallback = fallbackRect.gameObject.AddComponent<UpgradeCategoryGraphic>();
            fallback.raycastTarget = false;

            TextMeshProUGUI title = Text("Title", rect, "火力强化", 0, -54, 344, 60, 39, White, FontStyles.Bold);
            title.enableAutoSizing = true; title.fontSizeMin = 31; title.fontSizeMax = 39;
            TextMeshProUGUI effect = Text("Effect", rect, "伤害 +15%", 0, -122, 344, 56, 35, White, FontStyles.Bold);
            effect.enableAutoSizing = true; effect.fontSizeMin = 27; effect.fontSizeMax = 35;
            Box("ActionDivider", rect, 0, -168, 328, 1, new Color(.32f, .44f, .54f, .7f));
            RectTransform keyRect = Rect("Keycap", rect, -55, -222, 52, 52);
            Rounded(keyRect, new Color(.06f, .10f, .15f, .45f), 8, 1.5f, Line);
            TextMeshProUGUI key = Text("Shortcut", rect, (index + 1).ToString(), -55, -221, 46, 46, 31, Muted);
            Box("KeySeparator", rect, 0, -222, 1, 30, new Color(.38f, .5f, .6f, .8f));
            TextMeshProUGUI action = Text("ActionLabel", rect, "选择", 60, -222, 104, 48, 29, Muted);
            card.ConfigureIllustrated(button, panel, stripe, art, title, effect, category, key, action, fallback, illustrations);

            LevelUpOptionId[] ids = { LevelUpOptionId.DamageUp, LevelUpOptionId.MaxHpUp, LevelUpOptionId.MoveSpeedUp };
            LevelUpOptionCategory[] types = { LevelUpOptionCategory.Attack, LevelUpOptionCategory.Survival, LevelUpOptionCategory.Mobility };
            string[] titles = { "火力强化", "应急装甲", "轻装移动" };
            string[] effects = { "伤害 +15%", "最大生命值 +25", "移速 +10%" };
            card.Show(new LevelUpOption(new LevelUpOptionDefinition(ids[index], types[index], titles[index], effects[index], 0, 0, 1, -1)), index + 1);
            return card;
        }

        static UpgradeRoundedGraphic Rounded(RectTransform rect, Color fill, float radius, float stroke, Color line)
        {
            rect.gameObject.AddComponent<CanvasRenderer>();
            UpgradeRoundedGraphic graphic = rect.gameObject.AddComponent<UpgradeRoundedGraphic>();
            graphic.color = fill; graphic.raycastTarget = false; graphic.Configure(radius, stroke, line);
            return graphic;
        }
        static void RemoveExisting(Transform parent, string name)
        {
            Transform old = parent.Find(name);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            RectTransform rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset; rect.offsetMax = Vector2.one * -inset;
        }
        static Image Box(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = Rect(name, parent, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }
        static TextMeshProUGUI Text(string name, Transform parent, string content, float x, float y, float width, float height,
            float size, Color color, FontStyles style = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = Rect(name, parent, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = content; text.fontSize = size; text.fontStyle = style;
            text.color = color; text.alignment = alignment; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = Vector4.zero;
            return text;
        }
    }
}
