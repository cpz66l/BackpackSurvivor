using System;
using System.Collections.Generic;
using BS.GamePlay.Run;
using BS.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackSurvivor.EditorTools
{
    /// <summary>Rebinds the existing HUD data view to the image-designed v0.4 layout.</summary>
    public static class V04HudArtBuilder
    {
        public static readonly Color PanelColor = new Color(.066f, .108f, .15f, .94f);
        public static readonly Color BorderColor = new Color(.36f, .48f, .59f, .9f);
        public static readonly Color TextColor = new Color(.94f, .96f, .98f);
        public static readonly Color Muted = new Color(.65f, .75f, .84f);
        public static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BackpackSurvivor/Art/Font/SourceHanSansCN-Normal SDF.asset");

        public static void ApplyToActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
            var hud = UnityEngine.Object.FindAnyObjectByType<RunHudView>(FindObjectsInactive.Include);
            var session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            if (!hud || !session || !Font) throw new InvalidOperationException("Existing HUD, session and shared font required.");
            RectTransform root = (RectTransform)hud.transform;
            Transform canvas = hud.GetComponentInParent<Canvas>().transform;
            canvas.GetComponent<Canvas>().vertexColorAlwaysGammaSpace = false;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler) scaler.matchWidthOrHeight = .5f;
            ClearChildren(root);
            var oldImage = root.GetComponent<Image>(); if (oldImage) oldImage.enabled = false;
            root.gameObject.SetActive(true);
            SerializedObject bindings = new SerializedObject(hud);

            RectTransform vitals = Box("V04Vitals", root, 0, 0, 350, 108);
            Pin(vitals, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -26));
            Label("HealthMark", vitals, "+", -143, 21, 32, 40, 30, TextColor);
            RectTransform hpArea = Rect("Health", vitals, 0, 22, 260, 16);
            Image hpBack = hpArea.gameObject.AddComponent<Image>(); hpBack.color = new Color(.18f, .25f, .29f); hpBack.raycastTarget = false;
            Image hpFill = Fill("Fill", hpArea, new Color(.39f, .75f, .58f));
            Slider hp = hpArea.gameObject.AddComponent<Slider>(); hp.fillRect = hpFill.rectTransform;
            hp.minValue = 0; hp.maxValue = 1; hp.value = 1; hp.interactable = false; hp.transition = Selectable.Transition.None;
            hp.navigation = new Navigation { mode = Navigation.Mode.None };
            var hpText = Label("HealthText", vitals, "HP 100/100", 58, 0, 180, 25, 19, TextColor, TextAlignmentOptions.Right);
            Label("LevelCaption", vitals, "LV.", -135, -28, 42, 26, 21, Muted, TextAlignmentOptions.Left);
            var level = Label("Level", vitals, "1", -94, -28, 44, 27, 23, TextColor, TextAlignmentOptions.Left);
            RectTransform xpBar = Rect("Experience", vitals, 42, -28, 220, 7);
            var xpBack = xpBar.gameObject.AddComponent<Image>(); xpBack.color = new Color(.18f, .25f, .29f); xpBack.raycastTarget = false;
            Image xp = Fill("Fill", xpBar, new Color(.79f, .58f, .30f)); xp.sprite = SolidSprite(); xp.type = Image.Type.Filled; xp.fillMethod = Image.FillMethod.Horizontal; xp.fillAmount = 0;
            Bind(bindings, "hpSlider", hp); Bind(bindings, "hpText", hpText); Bind(bindings, "xpLoop", xp); Bind(bindings, "levelText", level);

            RectTransform timer = Rect("V04Timer", root, 0, 0, 460, 110);
            Pin(timer, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -22));
            var time = Label("Remaining", timer, "15:00", 0, 23, 200, 54, 43, TextColor);
            var wave = Label("Wave", timer, "WAVE 1", 0, -25, 440, 28, 22, Muted);
            var state = Label("StateBinding", timer, "", 0, 0, 0, 0, 18, Muted); state.gameObject.SetActive(false);
            Bind(bindings, "timeText", time); Bind(bindings, "waveText", wave); Bind(bindings, "stateText", state);

            RectTransform supplies = Box("V04Supplies", root, 0, 0, 215, 108);
            Pin(supplies, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -26));
            Label("GoldCaption", supplies, "金币", -51, 24, 72, 30, 20, Muted, TextAlignmentOptions.Left);
            var gold = Label("Gold", supplies, "0", 46, 24, 112, 32, 28, new Color(.89f, .73f, .42f), TextAlignmentOptions.Right);
            var distance = Label("SupplyDistance", supplies, "", 0, -24, 181, 29, 21, Muted, TextAlignmentOptions.Right);
            Bind(bindings, "goldText", gold); bindings.ApplyModifiedPropertiesWithoutUndo();
            var chest = canvas.GetComponent<ChestDistanceView>(); if (chest) { var data = new SerializedObject(chest); Bind(data, "chestDistanceText", distance); data.ApplyModifiedPropertiesWithoutUndo(); }

            // Contract tracker is part of the generated HUD so runtime scenes always
            // expose the live local progress, including an explicit empty state.
            Remove(root, "V04QuestTracker");
            RectTransform tracker = Box("V04QuestTracker", root, 0, 0, 330, 190);
            Pin(tracker, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(28, 10));
            var trackerText = Label("Text", tracker, "暂无进行中的合同", 12, 0, 306, 172, 17, TextColor, TextAlignmentOptions.TopLeft);
            trackerText.textWrappingMode = TextWrappingModes.Normal;
            trackerText.enableAutoSizing=true;trackerText.fontSizeMin=12;trackerText.fontSizeMax=17;
            var trackerView = tracker.gameObject.AddComponent<QuestTrackerView>();
            var trackerData = new SerializedObject(trackerView); Bind(trackerData, "text", trackerText); trackerData.ApplyModifiedPropertiesWithoutUndo();

            Button bag = Button("V04BagButton", root, "TAB  背包", 190, 56);
            Pin((RectTransform)bag.transform, Vector2.zero, Vector2.zero, new Vector2(28, 25));
            var inventory = UnityEngine.Object.FindAnyObjectByType<InventoryUIController>();
            if (inventory) UnityEventTools.AddPersistentListener(bag.onClick, inventory.HandleOpenBag);
            Button pause = Button("V04PauseButton", root, "ESC  暂停", 160, 56);
            Pin((RectTransform)pause.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 25));
            UnityEventTools.AddPersistentListener(pause.onClick, session.PauseRun);
            var keys = Label("V04Controls", root, "WASD 移动  ·  鼠标瞄准  ·  E 交互", 0, 0, 640, 30, 22, Muted);
            Pin(keys.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 35));
            BuildPrompt(canvas);
            EditorUtility.SetDirty(hud);
        }

        static void BuildPrompt(Transform canvas)
        {
            InteractPromptUI controller = canvas.GetComponent<InteractPromptUI>(); if (!controller) return;
            var data = new SerializedObject(controller);
            var old = data.FindProperty("promptPanel").objectReferenceValue as GameObject;
            if (old) old.SetActive(false);
            Remove(canvas, "V04InteractPrompt"); Remove(canvas, "V04BagFullNotice");
            RectTransform prompt = Box("V04InteractPrompt", canvas, 0, 0, 560, 52);
            Pin(prompt, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 99));
            var text = Label("Prompt", prompt, "E 交互", 0, 0, 528, 40, 24, TextColor);
            text.enableAutoSizing = true; text.fontSizeMin = 18; text.fontSizeMax = 24;
            var full = Label("V04BagFullNotice", canvas, "背包已满 · 请先整理空间", 0, 0, 590, 38, 23, new Color(.92f, .64f, .40f));
            Pin(full.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 160));
            Bind(data, "promptPanel", prompt.gameObject); Bind(data, "promptText", text); Bind(data, "promptBagFull", full); data.ApplyModifiedPropertiesWithoutUndo();
            prompt.gameObject.SetActive(false); full.gameObject.SetActive(false);
        }

        public static void ConfigureModalCoverage()
        {
            var session = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            var hud = UnityEngine.Object.FindAnyObjectByType<RunHudView>(FindObjectsInactive.Include);
            Transform canvas = hud.GetComponentInParent<Canvas>().transform;
            var groups = new List<CanvasGroup>();
            foreach (string name in new[] { "HUDPanel", "BagPanel", "ItemTooltip", "V04InteractPrompt", "V04BagFullNotice" })
            {
                Transform surface = canvas.Find(name); if (!surface) continue;
                var group = surface.GetComponent<CanvasGroup>();
                if (!group) group = surface.gameObject.AddComponent<CanvasGroup>();
                groups.Add(group);
            }
            var presenter = canvas.GetComponent<RunOverlayPresentation>() ?? canvas.gameObject.AddComponent<RunOverlayPresentation>();
            presenter.Configure(session, groups.ToArray()); EditorUtility.SetDirty(presenter);
            foreach (var upgrade in canvas.GetComponentsInChildren<LevelUpChoiceView>(true))
                if (upgrade.enabled) { upgrade.ConfigureCoveredHud(Array.Empty<CanvasGroup>()); EditorUtility.SetDirty(upgrade); }
        }

        public static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        public static RectTransform Box(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = Rect(name, parent, x, y, width, height);
            var graphic = rect.gameObject.AddComponent<UpgradeRoundedGraphic>(); graphic.color = PanelColor; graphic.Configure(8, 1, BorderColor); graphic.raycastTarget = false;
            return rect;
        }
        public static TextMeshProUGUI Label(string name, Transform parent, string value, float x, float y, float width, float height, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var text = Rect(name, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Font; text.text = value; text.fontSize = size; text.color = color; text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis; text.raycastTarget = false;
            return text;
        }
        public static Button Button(string name, Transform parent, string caption, float width, float height)
        {
            RectTransform rect = Box(name, parent, 0, 0, width, height);
            var panel = rect.GetComponent<UpgradeRoundedGraphic>(); panel.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = panel;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f); colors.pressedColor = new Color(.8f, .8f, .8f); colors.selectedColor = Color.white; colors.fadeDuration = .1f; button.colors = colors;
            Label("Label", rect, caption, 0, 0, width - 18, height - 8, 23, TextColor); return button;
        }
        public static void Pin(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position)
        { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; }
        static Image Fill(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent, 0, 0, 0, 0); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        static Sprite SolidSprite()
        {
            const string path = "Assets/BackpackSurvivor/Art/UI/V04/SolidWhite.asset";
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite sprite) return sprite;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "SolidWhite", filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white); texture.Apply(false, true); AssetDatabase.CreateAsset(texture, path);
            var result = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 100); result.name = "SolidWhite";
            AssetDatabase.AddObjectToAsset(result, texture); AssetDatabase.ImportAsset(path); return result;
        }
        public static void Bind(SerializedObject data, string field, UnityEngine.Object value)
        { var property = data.FindProperty(field); if (property == null) throw new InvalidOperationException("Missing UI binding " + field); property.objectReferenceValue = value; }
        public static void Remove(Transform parent, string name) { Transform child = parent.Find(name); if (child) UnityEngine.Object.DestroyImmediate(child.gameObject); }
        static void ClearChildren(Transform parent)
        { for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject); }
    }
}
