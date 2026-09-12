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
        private const string Model = "deepseek-flash";
        private const int TimeoutSeconds = 30;

        [SerializeField] private GameObject panelRoot;
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

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (applyButton != null) applyButton.onClick.AddListener(Apply);
            if (selfTestButton != null) selfTestButton.onClick.AddListener(SelfTest);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            if (applyButton != null) applyButton.onClick.RemoveListener(Apply);
            if (selfTestButton != null) selfTestButton.onClick.RemoveListener(SelfTest);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            fileConfig = LlmConfigService.LoadFile();
            SetInput(apiKeyInput, string.Empty);
            SetInput(maxTurnsInput, fileConfig.maxSessionTurns.ToString());
            SetInput(maxTokensInput, fileConfig.maxTotalTokens.ToString());
            SetInput(maxResponseInput, fileConfig.maxResponseCharacters.ToString());
            SetInput(maxPulseInput, fileConfig.maxPulseCharacters.ToString());
            SetStatus("配置仅保存到本机，不会写入游戏存档。", false);
            RefreshSource();
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (requestInProgress) return;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Apply()
        {
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
            SetStatus("配置已保存到 persistentDataPath。", false);
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

            if (!LlmConfigService.SaveFile(config, out error))
            {
                SetStatus("保存失败：" + error, true);
                return;
            }

            ResolvedLlmConfig resolved = LlmConfigService.Resolve();
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
                    model = Model,
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
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = TimeoutSeconds;
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + resolved.ApiKey);
                    await request.SendWebRequest();

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
                    SetStatus(valid ? "自检成功：DeepSeek 已连通（" + LlmConfigService.GetKeySourceLabel(resolved.KeySource) + "）。" :
                        "自检失败：HTTP 200 但响应结构不可用。", !valid);
                }
            }
            catch (Exception exception)
            {
                SetStatus("自检失败：" + MaskSecrets(exception.Message, resolved.ApiKey), true);
            }
            finally
            {
                requestInProgress = false;
                if (selfTestButton != null) selfTestButton.interactable = true;
                RefreshSource();
            }
        }

        private bool TryBuildConfig(out LlmModelConfig config, out string error)
        {
            config = fileConfig ?? LlmConfigService.LoadFile();
            error = string.Empty;
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
        [Serializable] private sealed class ResponseFormat { public string type; }
        [Serializable] private sealed class Thinking { public string type; }
        [Serializable] private sealed class ChatMessage { public string role; public string content; }
        [Serializable] private sealed class ChatResponse { public Choice[] choices; }
        [Serializable] private sealed class Choice { public ChatMessage message; }
    }
}
