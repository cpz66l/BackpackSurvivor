using System;
using BS.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackSurvivor.EditorTools
{
    /// <summary>Changes menu artwork and layout while retaining all existing menu/settings/record controllers.</summary>
    public static class V04MainMenuArtBuilder
    {
        public const string ScenePath = "Assets/BackpackSurvivor/Scenes/MainMenu/MainMenu.unity";
        public const string BackgroundPath = "Assets/BackpackSurvivor/Art/UI/V04/MainMenuBackground.png";

        public static void ApplyToActiveScene()
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
            if (!controller || controller.gameObject.scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open MainMenu outside Play Mode.");
            var background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            if (!background) throw new InvalidOperationException("Import generated menu background first.");
            var main = (RectTransform)controller.transform;
            main.GetComponent<Image>().sprite = background; main.GetComponent<Image>().color = Color.white;
            Transform oldTitle = main.Find("TitalImage"); if (oldTitle) oldTitle.gameObject.SetActive(false);
            V04HudArtBuilder.Remove(main, "V04Title"); V04HudArtBuilder.Remove(main, "V04Subtitle"); V04HudArtBuilder.Remove(main, "V04Version");
            V04HudArtBuilder.Label("V04Title", main, "背包幸存者", -465, 330, 640, 100, 70, V04HudArtBuilder.TextColor);
            V04HudArtBuilder.Label("V04Subtitle", main, "生存 · 搜刮 · 构筑", -465, 249, 620, 45, 26, V04HudArtBuilder.Muted);
            var version = V04HudArtBuilder.Label("V04Version", main, "v0.4  ·  美术迭代预览", 0, 0, 400, 34, 18, V04HudArtBuilder.Muted);
            V04HudArtBuilder.Pin(version.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(35, 24));
            StyleButton(main.Find("StartButton").GetComponent<Button>(), "开始行动", new Vector2(-465, 120), new Vector2(340, 70), true);
            StyleButton(main.Find("SettingsButton").GetComponent<Button>(), "设置", new Vector2(-555, 31), new Vector2(160, 56));
            StyleButton(main.Find("RecordButton").GetComponent<Button>(), "本地记录", new Vector2(-375, 31), new Vector2(160, 56));
            StyleButton(main.Find("GameplayGuideButton").GetComponent<Button>(), "玩法说明", new Vector2(-465, -55), new Vector2(340, 60));
            StyleButton(main.Find("StatementButton").GetComponent<Button>(), "制作者说明", new Vector2(-465, -134), new Vector2(340, 60));
            StyleButton(main.Find("QuitButton").GetComponent<Button>(), "退出游戏", new Vector2(-465, -213), new Vector2(340, 60));
            var data = new SerializedObject(controller); data.FindProperty("runSceneName").stringValue = "01-Run_ArtFull"; data.ApplyModifiedPropertiesWithoutUndo();
            Transform canvas = main.parent;
            StyleTextPanel(canvas.Find("AboutPanel") as RectTransform);
            StyleTextPanel(canvas.Find("GameplayGuidePanel") as RectTransform);
            StyleSettings(canvas.Find("SettingsModalRoot/SettingsPanel") as RectTransform);
            StyleRecords(canvas.Find("RecordModalRoot/RecordPanel") as RectTransform);
            var surfaces = new System.Collections.Generic.List<CanvasGroup>();
            foreach (Transform child in main)
                if (child.name.StartsWith("V04") || child.GetComponent<Button>())
                {
                    var group = child.GetComponent<CanvasGroup>();
                    if (!group) group = child.gameObject.AddComponent<CanvasGroup>();
                    surfaces.Add(group);
                }
            var modals = new System.Collections.Generic.List<GameObject>();
            foreach (string name in new[] { "AboutPanel", "GameplayGuidePanel", "SettingsModalRoot", "RecordModalRoot" })
                if (canvas.Find(name)) modals.Add(canvas.Find(name).gameObject);
            var overlay = main.GetComponent<MainMenuOverlayPresentation>() ?? main.gameObject.AddComponent<MainMenuOverlayPresentation>();
            overlay.Configure(modals.ToArray(), surfaces.ToArray());
            // Keep the independently-owned model configuration panel in sync when
            // the main-menu art builder is rerun.
            LlmConfigPanelBuilder.Build(controller);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        static void StyleTextPanel(RectTransform panel)
        {
            if (!panel) return;
            Center(panel, Vector2.zero, new Vector2(1280, 830)); StylePanel(panel);
            foreach (var text in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.color = V04HudArtBuilder.TextColor;
                text.enableVertexGradient = false;
                if (text.name == "BodyText")
                {
                    text.fontSize = 27; text.textWrappingMode = TextWrappingModes.Normal;
                    text.alignment = TextAlignmentOptions.TopLeft;
                    text.text = System.Text.RegularExpressions.Regex.Replace(text.text, "</?color[^>]*>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                else { text.fontSize = 42; }
            }
            Transform close = panel.Find("CloseButton"); if (close) StyleButton(close.GetComponent<Button>(), "返回", new Vector2(0, -325), new Vector2(240, 60));
            foreach (var image in panel.GetComponentsInChildren<Image>(true))
            {
                if (image.transform == panel || image.GetComponent<Mask>()) continue;
                image.color = image.name == "Handle" ? new Color(.36f, .49f, .58f) : new Color(.10f, .16f, .20f, .45f);
            }
            panel.gameObject.SetActive(false);
        }
        static void StyleSettings(RectTransform panel)
        {
            if (!panel) return;
            Center(panel, Vector2.zero, new Vector2(1200, 880)); StylePanel(panel);
            StyleButton(panel.Find("ApplyButton").GetComponent<Button>(), "应用", new Vector2(-150, -340), new Vector2(220, 60), true);
            StyleButton(panel.Find("ResetButton").GetComponent<Button>(), "重置设置", new Vector2(150, -340), new Vector2(220, 60));
            StyleButton(panel.Find("CloseButton").GetComponent<Button>(), "关闭", new Vector2(492, 387), new Vector2(126, 48));
            foreach (var text in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.color = V04HudArtBuilder.TextColor; text.enableVertexGradient = false;
                text.characterSpacing = 0;
                text.fontSize = text.name == "TitleText" ? 42 : 30;
            }
            foreach (var image in panel.GetComponentsInChildren<Image>(true))
            {
                if (image.transform == panel || image.GetComponent<Mask>()) continue;
                if (image.name == "Handle" || image.name == "Fill" || image.name == "Arrow" || image.name == "Item Checkmark") image.color = new Color(.46f, .67f, .68f);
                else image.color = new Color(.16f, .23f, .28f);
            }
            panel.parent.gameObject.SetActive(false);
        }
        static void StyleRecords(RectTransform panel)
        {
            if (!panel) return;
            Center(panel, Vector2.zero, new Vector2(1200, 780)); StylePanel(panel);
            string[] fields = { "totalRunsText", "totalWinsText", "bestBackpackValueText", "totalGoldText", "legendaryFoundCountText", "legendaryCollectedValueText" };
            for (int i = 0; i < fields.Length; i++)
            {
                var text = panel.Find(fields[i]).GetComponent<TextMeshProUGUI>();
                Vector2 pos = new Vector2(i % 2 == 0 ? -265 : 265, 190 - i / 2 * 145);
                Center(text.rectTransform, pos, new Vector2(470, 100)); text.fontSize = 25; text.color = V04HudArtBuilder.TextColor;
                text.enableAutoSizing = true; text.fontSizeMin = 19; text.fontSizeMax = 25; text.alignment = TextAlignmentOptions.Center;
                V04HudArtBuilder.Remove(panel, "V04Stat_" + i);
                var box = V04HudArtBuilder.Box("V04Stat_" + i, panel, pos.x, pos.y, 500, 110); box.SetAsFirstSibling();
            }
            var title = panel.Find("Text (TMP)").GetComponent<TextMeshProUGUI>(); Center(title.rectTransform, new Vector2(0, 313), new Vector2(640, 70)); title.fontSize = 42; title.color = V04HudArtBuilder.TextColor;
            StyleButton(panel.Find("CloseButton").GetComponent<Button>(), "返回", new Vector2(0, -280), new Vector2(260, 60));
            panel.parent.gameObject.SetActive(false);
        }
        static void StylePanel(RectTransform panel)
        {
            var image = panel.GetComponent<Image>(); if (image) UnityEngine.Object.DestroyImmediate(image);
            var graphic = panel.GetComponent<UpgradeRoundedGraphic>() ?? panel.gameObject.AddComponent<UpgradeRoundedGraphic>();
            graphic.color = new Color(.045f, .08f, .115f, 1); graphic.Configure(8, 1, V04HudArtBuilder.BorderColor); graphic.raycastTarget = true;
        }
        static void StyleButton(Button button, string caption, Vector2 position, Vector2 size, bool primary = false)
        {
            Center((RectTransform)button.transform, position, size);
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true)) text.gameObject.SetActive(false);
            var image = button.GetComponent<Image>(); if (image) UnityEngine.Object.DestroyImmediate(image);
            var panel = button.GetComponent<UpgradeRoundedGraphic>() ?? button.gameObject.AddComponent<UpgradeRoundedGraphic>();
            panel.color = primary ? new Color(.14f, .24f, .32f) : V04HudArtBuilder.PanelColor; panel.Configure(7, 1, V04HudArtBuilder.BorderColor); panel.raycastTarget = true;
            button.targetGraphic = panel; button.transition = Selectable.Transition.ColorTint; button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = ColorBlock.defaultColorBlock; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f); colors.selectedColor = Color.white; colors.pressedColor = new Color(.75f, .75f, .75f); button.colors = colors;
            V04HudArtBuilder.Remove(button.transform, "V04Caption");
            V04HudArtBuilder.Label("V04Caption", button.transform, caption, 0, 0, size.x - 12, size.y - 8, 26, V04HudArtBuilder.TextColor);
        }
        static void Center(RectTransform rect, Vector2 position, Vector2 size)
        { V04HudArtBuilder.Pin(rect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position); rect.sizeDelta = size; rect.localScale = Vector3.one; }
    }
}
