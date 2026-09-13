using System;
using System.Collections.Generic;
using BS.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackSurvivor.EditorTools
{
    /// <summary>Builds the standalone model configuration panel in the main menu scene.</summary>
    public static class LlmConfigPanelBuilder
    {
        public const string ScenePath = "Assets/BackpackSurvivor/Scenes/MainMenu/MainMenu.unity";

        [MenuItem("Tools/Backpack Survivor/UI/Build LLM Config Panel")]
        public static void BuildMenu()
        {
            MainMenuController controller = UnityEngine.Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
            if (controller == null || controller.gameObject.scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open MainMenu outside Play Mode.");
            Build(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LLM Config] panel built in " + ScenePath);
        }

        public static void Build(MainMenuController controller)
        {
            Transform main = controller.transform;
            Canvas canvas = controller.GetComponentInParent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("MainMenuController must be under a Canvas.");

            V04HudArtBuilder.Remove(main, "LlmConfigButton");
            V04HudArtBuilder.Remove(canvas.transform, "NpcConfigModalRoot");

            Button entry = V04HudArtBuilder.Button("LlmConfigButton", main, "AI NPC 设置", 190, 56);
            V04HudArtBuilder.Pin((RectTransform)entry.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(-195, 31));

            GameObject rootObject = new GameObject("NpcConfigModalRoot", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            rootObject.layer = 5;
            RectTransform root = (RectTransform)rootObject.transform;
            root.SetParent(canvas.transform, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            CanvasGroup rootGroup = rootObject.GetComponent<CanvasGroup>(); rootGroup.alpha = 1; rootGroup.interactable = true; rootGroup.blocksRaycasts = true;
            Image dimmer = rootObject.GetComponent<Image>(); dimmer.color = new Color(.012f, .024f, .036f, .68f); dimmer.raycastTarget = true;

            RectTransform panel = V04HudArtBuilder.Box("NpcConfigPanel", root, 0, 0, 1200, 880);
            V04HudArtBuilder.Pin(panel, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero);
            panel.GetComponent<UpgradeRoundedGraphic>().raycastTarget = true;
            V04HudArtBuilder.Label("TitleText", panel, "AI NPC 设置", 0, 370, 600, 58, 42, V04HudArtBuilder.TextColor);
            V04HudArtBuilder.Label("IntroText", panel, "营地对话、波次提醒、结算汇报统一开关；合同始终由本地判定。", 0, 322, 920, 32, 22, V04HudArtBuilder.Muted);

            RectTransform switchBox = V04HudArtBuilder.Box("NpcEnabledToggle", panel, -480, 265, 40, 40);
            Toggle enabled = switchBox.gameObject.AddComponent<Toggle>();
            enabled.targetGraphic = switchBox.GetComponent<UpgradeRoundedGraphic>();
            enabled.targetGraphic.raycastTarget = true;
            TMP_Text check = V04HudArtBuilder.Label("Check", switchBox, "✓", 0, 0, 40, 40, 30, V04HudArtBuilder.TextColor);
            check.raycastTarget = false;
            enabled.graphic = check;
            enabled.isOn = true;
            enabled.navigation = new Navigation { mode = Navigation.Mode.None };
            V04HudArtBuilder.Label("EnabledLabel", panel, "启用 AI NPC（关闭后使用本地简报）", -100, 265, 690, 40, 24, V04HudArtBuilder.TextColor, TextAlignmentOptions.Left);
            V04HudArtBuilder.Label("ModelLabel", panel, "DeepSeek 模型 ID", -385, 192, 260, 32, 22, V04HudArtBuilder.TextColor, TextAlignmentOptions.Left);
            TMP_InputField model = Input("ModelInput", panel, 120, 192, 720, 48, BS.Core.LLM.LlmModelConfig.DefaultModel);
            V04HudArtBuilder.Label("EndpointText", panel, "https://api.deepseek.com · thinking 关闭 · 事实查询启用只读工具", 0, 147, 1030, 30, 19, V04HudArtBuilder.Muted);
            V04HudArtBuilder.Label("ApiKeyLabel", panel, "DeepSeek API Key", -385, 95, 260, 32, 22, V04HudArtBuilder.TextColor, TextAlignmentOptions.Left);
            TMP_InputField apiKey = Input("ApiKeyInput", panel, 120, 95, 720, 48, "留空保持已保存的 Key；环境变量优先");
            apiKey.contentType = TMP_InputField.ContentType.Password;
            apiKey.inputType = TMP_InputField.InputType.Password;
            TMP_Text source = V04HudArtBuilder.Label("SourceText", panel, "当前生效：未配置", 0, 49, 1000, 30, 21, V04HudArtBuilder.Muted);

            V04HudArtBuilder.Label("TurnsLabel", panel, "会话轮次上限", -395, -26, 230, 30, 22, V04HudArtBuilder.TextColor, TextAlignmentOptions.Left);
            TMP_InputField turns = Input("TurnsInput", panel, -155, -26, 220, 46, "20");
            V04HudArtBuilder.Label("TokensLabel", panel, "会话 token 上限", 145, -26, 260, 30, 22, V04HudArtBuilder.TextColor, TextAlignmentOptions.Left);
            TMP_InputField tokens = Input("TokensInput", panel, 425, -26, 220, 46, "40000");
            V04HudArtBuilder.Label("ResponseLabel", panel, "回复字数上限", -395, -100, 230, 30, 22, V04HudArtBuilder.TextColor, TextAlignmentOptions.Left);
            TMP_InputField response = Input("ResponseInput", panel, -155, -100, 220, 46, "200");
            V04HudArtBuilder.Label("PulseLabel", panel, "波次提醒字数上限", 145, -100, 260, 30, 22, V04HudArtBuilder.TextColor, TextAlignmentOptions.Left);
            TMP_InputField pulse = Input("PulseInput", panel, 425, -100, 220, 46, "60");

            TMP_Text status = V04HudArtBuilder.Label("StatusText", panel, "", 0, -215, 1060, 84, 22, V04HudArtBuilder.Muted);
            status.enableAutoSizing = true; status.fontSizeMin = 18; status.fontSizeMax = 22;
            Button defaults = V04HudArtBuilder.Button("DefaultsButton", panel, "开发默认值", 220, 60);
            V04HudArtBuilder.Pin((RectTransform)defaults.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-390, -350));
            Button apply = V04HudArtBuilder.Button("ApplyButton", panel, "保存配置", 220, 60);
            V04HudArtBuilder.Pin((RectTransform)apply.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-130, -350));
            Button test = V04HudArtBuilder.Button("SelfTestButton", panel, "连通性自检", 220, 60);
            V04HudArtBuilder.Pin((RectTransform)test.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(130, -350));
            Button close = V04HudArtBuilder.Button("CloseButton", panel, "关闭", 220, 60);
            V04HudArtBuilder.Pin((RectTransform)close.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(390, -350));

            NpcConfigView view = panel.gameObject.AddComponent<NpcConfigView>();
            SerializedObject data = new SerializedObject(view);
            Bind(data, "panelRoot", rootObject);
            Bind(data, "npcEnabledToggle", enabled);
            Bind(data, "modelInput", model);
            Bind(data, "defaultsButton", defaults);
            Bind(data, "apiKeyInput", apiKey);
            Bind(data, "maxTurnsInput", turns);
            Bind(data, "maxTokensInput", tokens);
            Bind(data, "maxResponseInput", response);
            Bind(data, "maxPulseInput", pulse);
            Bind(data, "sourceText", source);
            Bind(data, "statusText", status);
            Bind(data, "applyButton", apply);
            Bind(data, "selfTestButton", test);
            Bind(data, "closeButton", close);
            data.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(entry.onClick, view.Open);
            rootObject.SetActive(false);

            ConfigureOverlay(controller, canvas.transform);
        }

        private static TMP_InputField Input(string name, Transform parent, float x, float y, float width, float height, string placeholder)
        {
            RectTransform rect = V04HudArtBuilder.Box(name, parent, x, y, width, height);
            UpgradeRoundedGraphic background = rect.GetComponent<UpgradeRoundedGraphic>();
            background.raycastTarget = true;
            TMP_InputField field = rect.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = background;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.navigation = new Navigation { mode = Navigation.Mode.None };
            TMP_Text text = V04HudArtBuilder.Label("Text", rect, string.Empty, 0, 0, width - 26, height - 8, 22, V04HudArtBuilder.TextColor, TextAlignmentOptions.Left);
            text.margin = new Vector4(14, 0, 14, 0);
            text.richText = false;
            field.richText = false;
            field.textViewport = rect;
            field.textComponent = text;
            TMP_Text hint = V04HudArtBuilder.Label("Placeholder", rect, placeholder, 0, 0, width - 26, height - 8, 21, new Color(.65f, .75f, .84f, .65f), TextAlignmentOptions.Left);
            hint.margin = new Vector4(14, 0, 14, 0);
            field.placeholder = hint;
            return field;
        }

        private static void Bind(SerializedObject data, string field, UnityEngine.Object value)
        {
            SerializedProperty property = data.FindProperty(field);
            if (property == null) throw new InvalidOperationException("Missing NpcConfigView field " + field);
            property.objectReferenceValue = value;
        }

        private static void ConfigureOverlay(MainMenuController controller, Transform canvas)
        {
            MainMenuOverlayPresentation overlay = controller.GetComponent<MainMenuOverlayPresentation>();
            if (overlay == null) overlay = controller.gameObject.AddComponent<MainMenuOverlayPresentation>();
            List<GameObject> modals = new List<GameObject>();
            foreach (string name in new[] { "AboutPanel", "GameplayGuidePanel", "SettingsModalRoot", "RecordModalRoot", "NpcConfigModalRoot" })
            {
                Transform modal = canvas.Find(name);
                if (modal != null) modals.Add(modal.gameObject);
            }
            List<CanvasGroup> surfaces = new List<CanvasGroup>();
            foreach (Transform child in controller.transform)
            {
                if (!child.name.StartsWith("V04", StringComparison.Ordinal) && child.GetComponent<Button>() == null && child.name != "LlmConfigButton") continue;
                CanvasGroup group = child.GetComponent<CanvasGroup>() ?? child.gameObject.AddComponent<CanvasGroup>();
                surfaces.Add(group);
            }
            overlay.Configure(modals.ToArray(), surfaces.ToArray());
            EditorUtility.SetDirty(overlay);
        }
    }
}
