using System;
using System.Collections.Generic;
using BS.GamePlay.Run;
using BS.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BackpackSurvivor.EditorTools
{
    /// <summary>Rebuilds only the owned V04Pause/V04Result visuals, retaining the existing result controller.</summary>
    public static class V04RunMenusArtBuilder
    {
        public const string PauseRootName = "V04Pause";
        public const string ResultRootName = "V04Result";
        public const string PauseControllerName = "V04PauseController";
        private const string FontPath = "Assets/BackpackSurvivor/Art/Font/SourceHanSansCN-Normal SDF.asset";
        private static readonly Color Panel = new Color(.066f, .108f, .15f, .965f);
        private static readonly Color Line = new Color(.36f, .48f, .59f, .9f);
        private static readonly Color White = new Color(.94f, .96f, .98f, 1f);
        private static readonly Color Muted = new Color(.65f, .75f, .84f, 1f);
        private static TMP_FontAsset font;

        [MenuItem("Tools/Backpack Survivor/Art/V0.4/Apply Pause And Result UI")]
        public static void ApplyToActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Build pause and result UI outside Play Mode.");
            Scene scene = SceneManager.GetActiveScene();
            GameSession session = FindSingle<GameSession>(scene);
            ResultView resultView = FindSingle<ResultView>(scene);
            Canvas canvas = resultView.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                throw new InvalidOperationException("The existing ResultView must belong to the screen-space gameplay Canvas.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidOperationException("Missing shared Chinese UI font: " + FontPath);

            SerializedObject resultData = new SerializedObject(resultView);
            GameObject oldVisual = resultData.FindProperty("panel").objectReferenceValue as GameObject;
            if (oldVisual != null && oldVisual != canvas.gameObject &&
                !resultView.transform.IsChildOf(oldVisual.transform) && oldVisual.transform != resultView.transform)
                oldVisual.SetActive(false);

            RemoveOwned(canvas.transform, PauseRootName);
            RemoveOwned(canvas.transform, ResultRootName);
            RemoveOwned(canvas.transform, PauseControllerName);
            BuildPause(canvas.transform, session);
            BuildResult(canvas.transform, session, resultView);
            EditorUtility.SetDirty(resultView);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static T FindSingle<T>(Scene scene) where T : Component
        {
            List<T> matches = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects()) matches.AddRange(root.GetComponentsInChildren<T>(true));
            if (matches.Count != 1) throw new InvalidOperationException("Expected exactly one " + typeof(T).Name + " in the active gameplay scene; found " + matches.Count + ".");
            return matches[0];
        }

        [MenuItem("Tools/Backpack Survivor/Art/V0.4/Apply Result UI Only")]
        public static void ApplyResultToActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Build result UI outside Play Mode.");
            Scene scene = SceneManager.GetActiveScene();
            GameSession session = FindSingle<GameSession>(scene);
            ResultView view = FindSingle<ResultView>(scene);
            Canvas canvas = view.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                throw new InvalidOperationException("ResultView requires the gameplay overlay Canvas.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidOperationException("Missing shared Chinese UI font.");
            RemoveOwned(canvas.transform, ResultRootName);
            BuildResult(canvas.transform, session, view);
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void BuildPause(Transform canvas, GameSession session)
        {
            RectTransform root = Modal(PauseRootName, canvas, 220);
            RectTransform dialog = Rect("PauseDialog", root, 0, 0, 790, 548);
            Rounded(dialog, Panel, 12, 1.5f, Line, true);
            Text("Heading", dialog, "战术暂停", 0, 190, 670, 76, 52, White, FontStyles.Bold);
            Button resume = MenuButton("Continue", dialog, "继续游戏", 0, 62, 510, 88, true);
            Button restart = MenuButton("Restart", dialog, "回营地重试", 0, -50, 510, 88, false);
            Button mainMenu = MenuButton("MainMenu", dialog, "回到主菜单", 0, -162, 510, 88, false);
            GameObject controller = new GameObject(PauseControllerName);
            controller.transform.SetParent(canvas, false); controller.layer = 5;
            PauseMenuView view = controller.AddComponent<PauseMenuView>();
            view.Configure(session, root.gameObject, dialog, resume, restart, mainMenu);
            EditorUtility.SetDirty(view);
            root.gameObject.SetActive(false);
        }

        private static void BuildResult(Transform canvas, GameSession session, ResultView resultView)
        {
            RectTransform root = Modal(ResultRootName, canvas, 230);
            RectTransform dialog = Rect("ResultDialog", root, 0, 0, 1160, 900);
            Rounded(dialog, Panel, 12, 1.5f, Line, true);
            TextMeshProUGUI title = Text("Heading", dialog, "行动结束", 0, 372, 1036, 78, 52, White, FontStyles.Bold);
            TextMeshProUGUI subtitle = Text("State", dialog, "本次行动已结束", 0, 310, 1036, 44, 26, Muted);
            Divider("HeaderDivider", dialog, 276, 1036);

            string[] labels = { "生存时间", "达到等级", "击杀数量", "获得金币", "背包价值", "传说装备" };
            string[] names = { "Elapsed", "Level", "Kills", "Gold", "BackpackValue", "LegendaryCount" };
            TMP_Text[] values = new TMP_Text[6];
            for (int i = 0; i < values.Length; i++)
            {
                float x = (i % 3 - 1) * 352f;
                float y = 196 - (i / 3) * 144f;
                RectTransform stat = Rect("Stat_" + names[i], dialog, x, y, 332, 128);
                Rounded(stat, new Color(.045f, .083f, .117f, .76f), 7, 1.2f, new Color(.30f, .41f, .51f, .8f), false);
                Text("Label", stat, labels[i], 0, 30, 294, 42, 27, Muted);
                TextMeshProUGUI value = Text("Value", stat, i == 0 ? "00:00" : "0", 0, -23, 294, 55, 42, White, FontStyles.Bold);
                value.enableAutoSizing = true; value.fontSizeMin = 26; value.fontSizeMax = 42;
                values[i] = value;
            }

            TextMeshProUGUI summary = Text("SupplementaryStats", dialog, "总经验  0    ·    传说装备价值  ￥0", 0, -40, 1036, 36, 25, Muted);
            summary.enableAutoSizing = true; summary.fontSizeMin = 20; summary.fontSizeMax = 25;
            RectTransform viewport = Rect("ResultDetails", dialog, 0, -194, 1036, 244);
            Image hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = new Color(.045f, .083f, .117f, .76f);
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            RectTransform details = Rect("Content", viewport, 0, 0, 0, 0);
            details.anchorMin = new Vector2(0, 1); details.anchorMax = Vector2.one;
            details.pivot = new Vector2(.5f, 1);
            VerticalLayoutGroup layout = details.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 12); layout.spacing = 12;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = details.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = details; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 36;
            FlowText("DebriefHeading", details, "小芯 · 行动评语", 22, White, FontStyles.Bold);
            TextMeshProUGUI debrief = FlowText("Debrief", details, "小芯在门口等你，看到你回来，先偷偷松了一口气……", 24, White);
            TextMeshProUGUI questOutcome = FlowText("QuestOutcome", details, "当前没有进行中的合同", 22, Muted);
            Text("ScrollHint", dialog, "评语与带出清单 · 滚动查看", 0, -329, 1036, 22, 16, Muted);
            Divider("FooterDivider", dialog, -349, 1036);
            Button restart = MenuButton("Restart", dialog, "返回营地", -245, -394, 450, 72, true);
            Button mainMenu = MenuButton("MainMenu", dialog, "查看合同", 245, -394, 450, 72, false);
            resultView.ConfigurePresentation(session, root.gameObject, dialog, title, subtitle, values, summary,
                restart, mainMenu, new Color(.56f, .88f, .72f, 1f), new Color(.95f, .73f, .65f, 1f), questOutcome, debrief);
            root.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI FlowText(string name, Transform parent, string content,
            float size, Color color, FontStyles style = FontStyles.Normal)
        {
            TextMeshProUGUI text = Text(name, parent, content, 0, 0, 1000, 32, size, color, style);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static RectTransform Modal(string name, Transform parent, int sortingOrder)
        {
            RectTransform root = Rect(name, parent, 0, 0, 0, 0);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            Canvas canvas = root.gameObject.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = sortingOrder;
            root.gameObject.AddComponent<GraphicRaycaster>();
            root.gameObject.AddComponent<CanvasGroup>();
            Image dimmer = root.gameObject.AddComponent<Image>();
            dimmer.color = new Color(.022f, .037f, .058f, .56f); dimmer.raycastTarget = true;
            return root;
        }

        private static Button MenuButton(string name, Transform parent, string label, float x, float y, float width, float height, bool primary)
        {
            RectTransform rect = Rect(name, parent, x, y, width, height);
            // Neutral tint keeps both the blue fill and the fine border readable in every button state.
            Color fill = primary ? new Color(.13f, .225f, .30f, 1f) : new Color(.055f, .10f, .14f, 1f);
            Color tintBase = new Color(fill.r / .7f, fill.g / .7f, fill.b / .7f, fill.a);
            Color tintLine = new Color(Line.r / .7f, Line.g / .7f, Line.b / .7f, Line.a);
            UpgradeRoundedGraphic graphic = Rounded(rect, tintBase, 7, 1.5f, tintLine, true);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = new Color(.7f, .7f, .7f, 1f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.5f, .5f, .5f, 1f);
            colors.disabledColor = new Color(.35f, .35f, .35f, .65f);
            colors.colorMultiplier = 1f; colors.fadeDuration = .08f;
            button.colors = colors;
            Text("Label", rect, label, 0, 0, width - 32, height - 12, 34, primary ? White : Muted);
            return button;
        }

        private static UpgradeRoundedGraphic Rounded(RectTransform rect, Color fill, float radius, float stroke, Color line, bool raycast)
        {
            if (rect.GetComponent<CanvasRenderer>() == null) rect.gameObject.AddComponent<CanvasRenderer>();
            UpgradeRoundedGraphic graphic = rect.gameObject.AddComponent<UpgradeRoundedGraphic>();
            graphic.color = fill; graphic.Configure(radius, stroke, line); graphic.raycastTarget = raycast;
            return graphic;
        }

        private static void Divider(string name, Transform parent, float y, float width)
        {
            Image image = Rect(name, parent, 0, y, width, 1.25f).gameObject.AddComponent<Image>();
            image.color = new Color(.36f, .48f, .59f, .72f); image.raycastTarget = false;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string content, float x, float y, float width, float height,
            float size, Color color, FontStyles style = FontStyles.Normal)
        {
            TextMeshProUGUI text = Rect(name, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = content; text.fontSize = size; text.fontStyle = style;
            text.color = color; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis; text.margin = Vector4.zero;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            RectTransform rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static void RemoveOwned(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }
    }
}
