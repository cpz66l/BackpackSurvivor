using System;
using System.Text;
using BS.Core.LLM;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BS.Presentation
{
    /// <summary>Runtime-only model configuration panel. It owns no gameplay state.</summary>
    public sealed class NpcConfigView : MonoBehaviour
    {
        private const string Endpoint = "https://api.deepseek.com/chat/completions";
        
        private const int TimeoutSeconds = 30;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Toggle npcEnabledToggle;
        [SerializeField] private TMP_InputField modelInput;
        [SerializeField] private Button defaultsButton;
        [SerializeField] private TMP_InputField apiKeyInput;
        [SerializeField] private TMP_InputField maxTurnsInput;
        [SerializeField] private TMP_InputField maxTokensInput;
        [SerializeField] private TMP_InputField maxResponseInput;
        [SerializeField] private TMP_InputField maxPulseInput;
        [SerializeField] private TMP_Text sourceText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button selfTestButton;
        [SerializeField] private Button closeButton;

        private LlmModelConfig fileConfig;
        private bool requestInProgress;
        private UnityWebRequest activeRequest;
        public string Status => statusText == null ? "" : statusText.text;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
        }

        private void OnEnable()
        {
            if (defaultsButton != null) defaultsButton.onClick.AddListener(RestoreDefaults);
            if (applyButton != null) applyButton.onClick.AddListener(Apply);
            if (selfTestButton != null) selfTestButton.onClick.AddListener(SelfTest);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            activeRequest?.Abort();
            SetInput(apiKeyInput, string.Empty);
            if (defaultsButton != null) defaultsButton.onClick.RemoveListener(RestoreDefaults);
            if (applyButton != null) applyButton.onClick.RemoveListener(Apply);
            if (selfTestButton != null) selfTestButton.onClick.RemoveListener(SelfTest);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            fileConfig = LlmConfigService.LoadFile();
            Populate(fileConfig);
            SetStatus("关闭 AI 后使用本地简报，合同照常进行。修改后点击保存。", false);
            RefreshSource();
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(true);
        }

        private void Populate(LlmModelConfig config)
        {
            if (npcEnabledToggle) npcEnabledToggle.SetIsOnWithoutNotify(config.npcEnabled);
            SetInput(modelInput, config.model);
            SetInput(apiKeyInput, string.Empty);
            SetInput(maxTurnsInput, config.maxSessionTurns.ToString());
            SetInput(maxTokensInput, config.maxTotalTokens.ToString());
            SetInput(maxResponseInput, config.maxResponseCharacters.ToString());
            SetInput(maxPulseInput, config.maxPulseCharacters.ToString());

        }

        public void RestoreDefaults()
        {
            if (requestInProgress) return;
            Populate(LlmModelConfig.CreateDefault());
            SetStatus("已填入开发默认配置；保存后生效，保留已有密钥。", false);
        }

        public void Close()
        {
            activeRequest?.Abort();
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Apply()
        {
            if (requestInProgress) return;
            if (!TryBuildConfig(out LlmModelConfig config, out string error))
            {
                SetStatus(error, true);
                return;
            }

            if (!LlmConfigService.SaveFile(config, out error))
            {
                SetStatus("保存失败：" + error, true);
                return;
            }

            fileConfig = config;
            Populate(config);
            SetStatus("已保存：AI NPC " + (config.npcEnabled ? "开启 · " + config.model : "关闭 · 使用本地简报") + "。", false);
            RefreshSource();
        }

        public async void SelfTest()
        {
            if (requestInProgress) return;
            if (!TryBuildConfig(out LlmModelConfig config, out string error))
            {
                SetStatus(error, true);
                return;
            }

            ResolvedLlmConfig resolved = LlmConfigService.Resolve(config);
            RefreshSource(resolved);
            if (string.IsNullOrWhiteSpace(resolved.ApiKey))
            {
                SetStatus("自检失败：没有可用的 API Key。", true);
                return;
            }

            requestInProgress = true;
            if (selfTestButton != null) selfTestButton.interactable = false;
            SetStatus("正在连接 DeepSeek…", false);
            try
            {
                RequestPayload payload = new RequestPayload
                {
                    model = config.model,
                    messages = new[]
                    {
                        new ChatMessage { role = "system", content = "Return JSON only." },
                        new ChatMessage { role = "user", content = "Reply with exactly {\"ok\":true}." }
                    },
                    response_format = new ResponseFormat { type = "json_object" },
                    thinking = new Thinking { type = "disabled" }
                };

                using (UnityWebRequest request = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST))
                {
                    activeRequest = request;
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = TimeoutSeconds;
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + resolved.ApiKey);
                    await request.SendWebRequest();

                    if (this == null || !isActiveAndEnabled) return;
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        string detail = request.error;
                        if (string.IsNullOrWhiteSpace(detail)) detail = "HTTP " + request.responseCode;
                        SetStatus("自检失败：" + MaskSecrets(detail, resolved.ApiKey), true);
                        return;
                    }

                    string body = request.downloadHandler == null ? string.Empty : request.downloadHandler.text;
                    ChatResponse response = JsonUtility.FromJson<ChatResponse>(body);
                    bool valid = response != null && response.choices != null && response.choices.Length > 0 &&
                                 response.choices[0] != null && response.choices[0].message != null &&
                                 !string.IsNullOrWhiteSpace(response.choices[0].message.content);
                    if (valid) valid = JsonUtility.FromJson<ProbeResponse>(response.choices[0].message.content)?.ok == true;
                    SetStatus(valid ? "自检成功：" + config.model + " 已连通（" + LlmConfigService.GetKeySourceLabel(resolved.KeySource) + "）。草稿未自动保存。" :
                        "自检失败：HTTP 200 但响应结构不可用。", !valid);
                }
            }
            catch (Exception exception)
            {
                if (this != null && isActiveAndEnabled) SetStatus("自检失败：" + MaskSecrets(exception.Message, resolved.ApiKey), true);
            }
            finally
            {
                activeRequest = null;
                requestInProgress = false;
                if (selfTestButton != null) selfTestButton.interactable = true;
                RefreshSource();
            }
        }

        private bool TryBuildConfig(out LlmModelConfig config, out string error)
        {
            config = JsonUtility.FromJson<LlmModelConfig>(JsonUtility.ToJson(fileConfig ?? LlmConfigService.LoadFile()));
            error = string.Empty;
            config.npcEnabled = npcEnabledToggle == null || npcEnabledToggle.isOn;
            config.model = modelInput == null ? LlmModelConfig.DefaultModel : modelInput.text.Trim();
            if (string.IsNullOrWhiteSpace(config.model) || config.model.Length > 100 ||
                !System.Text.RegularExpressions.Regex.IsMatch(config.model, @"^[a-zA-Z0-9][a-zA-Z0-9._-]*$"))
            { error = "请输入有效的 DeepSeek 模型 ID（字母、数字、点、横线或下划线）。"; return false; }
            string key = apiKeyInput == null ? string.Empty : apiKeyInput.text.Trim();
            if (!string.IsNullOrWhiteSpace(key)) config.apiKey = key;
            if (!TryReadInt(maxTurnsInput, "会话轮次", out config.maxSessionTurns, out error)) return false;
            if (!TryReadInt(maxTokensInput, "总 token", out config.maxTotalTokens, out error)) return false;
            if (!TryReadInt(maxResponseInput, "单次响应长度", out config.maxResponseCharacters, out error)) return false;
            if (!TryReadInt(maxPulseInput, "脉冲响应长度", out config.maxPulseCharacters, out error)) return false;
            config.Normalize();
            return true;
        }

        private static bool TryReadInt(TMP_InputField field, string label, out int value, out string error)
        {
            error = string.Empty;
            value = 0;
            if (field == null || !int.TryParse(field.text, out value) || value <= 0)
            {
                error = label + "必须是正整数。";
                return false;
            }
            return true;
        }

        private void RefreshSource()
        {
            RefreshSource(LlmConfigService.Resolve());
        }

        private void RefreshSource(ResolvedLlmConfig resolved)
        {
            if (sourceText == null) return;
            sourceText.text = "当前生效：" + LlmConfigService.GetKeySourceLabel(resolved.KeySource) +
                              "  " + LlmConfigService.MaskKey(resolved.ApiKey);
        }

        private void SetStatus(string message, bool error)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = error ? new Color(.96f, .55f, .48f) : new Color(.65f, .84f, .76f);
        }

        private static void SetInput(TMP_InputField field, string value)
        {
            if (field != null) field.SetTextWithoutNotify(value ?? string.Empty);
        }

        private static string MaskSecrets(string value, string key)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(key)) return value ?? string.Empty;
            return value.Replace(key, "***MASKED***");
        }

        [Serializable] private sealed class RequestPayload
        {
            public string model;
            public ChatMessage[] messages;
            public ResponseFormat response_format;
            public Thinking thinking;
        }
        [Serializable] private sealed class ProbeResponse { public bool ok; }
        [Serializable] private sealed class ResponseFormat { public string type; }
        [Serializable] private sealed class Thinking { public string type; }
        [Serializable] private sealed class ChatMessage { public string role; public string content; }
        [Serializable] private sealed class ChatResponse { public Choice[] choices; }
        [Serializable] private sealed class Choice { public ChatMessage message; }
    }
}
