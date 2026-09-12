using System;
using System.Collections.Generic;
using System.Text;
using Stopwatch = System.Diagnostics.Stopwatch;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace BackpackSurvivor.EditorTools
{
    /// <summary>
    /// S0 only: a non-streaming DeepSeek probe. This is deliberately editor-only and has no
    /// dependency on gameplay state, save data, or the dialogue runtime.
    /// </summary>
    public static class DeepSeekS0Probe
    {
        private const string ApiKeyName = "DEEPSEEK_API_KEY";
        private const string Endpoint = "https://api.deepseek.com/chat/completions";
        private const string Model = "deepseek-flash";
        private const int TimeoutSeconds = 30;
        private const int MaxRounds = 3;
        private const string ProbeToolName = "get_probe_facts";

        [MenuItem("Tools/Backpack Survivor/LLM/S0 Probe DeepSeek (Non-Streaming)")]
        public static async void Run()
        {
            string key = ReadApiKey();
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError("[LLM S0] DEEPSEEK_API_KEY 未配置。请设置进程环境变量，或在 Windows 用户级环境变量中设置后重试。未读取或写入任何配置文件。");
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            int errorCount = 0;
            int emptyContentCount = 0;
            int parseFailureCount = 0;
            bool executedWhitelistedTool = false;
            try
            {
                List<ChatMessage> messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        role = "system",
                        content = "You are an API compatibility probe. Return JSON only. Use the supplied read-only tool before answering. Do not invent tool results."
                    },
                    new ChatMessage
                    {
                        role = "user",
                        content = "Call get_probe_facts first, then answer with exactly this JSON shape: {\"answer\":\"one short sentence\",\"usedTools\":[\"get_probe_facts\"]}. The tool is read-only and cannot change game state."
                    }
                };

                for (int round = 1; round <= MaxRounds; round++)
                {
                    ChatResponse response = await SendAsync(key, messages.ToArray(), executedWhitelistedTool);
                    string safeRaw = MaskSecrets(response.rawBody, key);
                    Debug.Log("[LLM S0] round=" + round + " raw response:\n" + safeRaw);

                    if (response.error)
                    {
                        errorCount++;
                        Debug.LogError("[LLM S0] 请求失败：" + response.errorMessage);
                        return;
                    }

                    if (response.choices == null || response.choices.Length == 0 || response.choices[0] == null || response.choices[0].message == null)
                    {
                        parseFailureCount++;
                        Debug.LogError("[LLM S0] 结构化解析失败：响应没有 choices/message。");
                        return;
                    }

                    ChatMessage assistant = response.choices[0].message;
                    ToolCall[] toolCalls = assistant.tool_calls ?? Array.Empty<ToolCall>();
                    if (toolCalls.Length == 0)
                    {
                        if (!executedWhitelistedTool)
                        {
                            Debug.LogError("[LLM S0] 模型未执行白名单工具，未完成工具调用闭环。");
                            return;
                        }

                        string content = assistant.content == null ? string.Empty : assistant.content.Trim();
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            emptyContentCount++;
                            Debug.LogError("[LLM S0] 空内容：模型没有返回最终回答。");
                            return;
                        }

                        if (!TryParseFinalJson(content, out ProbeAnswer answer))
                        {
                            parseFailureCount++;
                            Debug.LogError("[LLM S0] 结构化解析失败：最终内容不是可解析的 Probe JSON。\n" + MaskSecrets(content, key));
                            return;
                        }

                        Debug.Log("[LLM S0] final JSON:\n" + MaskSecrets(content, key));
                        Debug.Log("[LLM S0] parsed answer fields: answerPresent=" + !string.IsNullOrWhiteSpace(answer.answer) + ", usedTools=" + (answer.usedTools == null ? 0 : answer.usedTools.Length));
                        Debug.Log("[LLM S0] completed in " + stopwatch.ElapsedMilliseconds + " ms; thinking=disabled; tool audit complete.");
                        return;
                    }

                    messages.Add(CloneAssistantForToolRound(assistant));
                    foreach (ToolCall call in toolCalls)
                    {
                        string toolName = call == null || call.function == null ? string.Empty : call.function.name;
                        string arguments = call == null || call.function == null ? string.Empty : call.function.arguments;
                        string auditArguments = MaskSecrets(arguments ?? string.Empty, key);
                        if (!string.Equals(toolName, ProbeToolName, StringComparison.Ordinal))
                        {
                            Debug.LogWarning("[LLM S0][tool audit] rejected unknown tool name=" + toolName + " arguments=" + auditArguments);
                            messages.Add(ToolResult(call == null ? string.Empty : call.id, "{\"error\":\"tool_not_whitelisted\"}"));
                            continue;
                        }

                        if (!IsEmptyObject(arguments))
                        {
                            Debug.LogWarning("[LLM S0][tool audit] rejected invalid parameters name=" + toolName + " arguments=" + auditArguments);
                            messages.Add(ToolResult(call.id, "{\"error\":\"invalid_read_only_parameters\"}"));
                            continue;
                        }

                        string result = "{\"status\":\"ok\",\"facts\":{\"source\":\"s0_probe\",\"value\":42}}";
                        executedWhitelistedTool = true;
                        Debug.Log("[LLM S0][tool audit] executed read-only tool name=" + toolName + " arguments=" + auditArguments + " result=" + result);
                        messages.Add(ToolResult(call.id, result));
                    }
                }

                Debug.LogError("[LLM S0] 工具调用轮次超过上限 " + MaxRounds + "，已停止，未改变任何游戏状态。");
            }
            catch (Exception exception)
            {
                Debug.LogError("[LLM S0] 未处理异常：" + MaskSecrets(exception.Message, key));
            }
            finally
            {
                stopwatch.Stop();
                Debug.Log("[LLM S0] counters: errors=" + errorCount + ", emptyContent=" + emptyContentCount + ", parseFailures=" + parseFailureCount);
            }
        }

        private static async Awaitable<ChatResponse> SendAsync(string key, ChatMessage[] messages, bool finalJson)
        {
            RequestPayload payload = new RequestPayload
            {
                model = Model,
                messages = messages,
                // DeepSeek may emit a textual DSML tool marker when JSON Output is forced
                // on the same round as the tool request. Keep the tool round native, then
                // require JSON only after the local tool result has been appended.
                // Unity's JSON serializer emits an empty object for a null nested
                // response_format. Use explicit text mode for the tool round so the
                // provider receives a valid value, then switch to JSON Output only
                // after the whitelisted tool result has been appended.
                response_format = new ResponseFormat
                {
                    type = finalJson ? "json_object" : "text"
                },
                thinking = new Thinking { type = "disabled" },
                tools = new[]
                {
                    new ToolDefinition
                    {
                        type = "function",
                        function = new FunctionDefinition
                        {
                            name = ProbeToolName,
                            description = "Read-only S0 compatibility probe. It never changes game state.",
                            parameters = new ToolParameters { type = "object", properties = new ToolProperties() }
                        }
                    }
                },
                tool_choice = finalJson ? "none" : "required"
            };

            string body = JsonUtility.ToJson(payload);
            using (UnityWebRequest request = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = TimeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + key);
                try
                {
                    await request.SendWebRequest();
                }
                catch (Exception exception)
                {
                    return ChatResponse.Failed(MaskSecrets(exception.Message, key));
                }

                string raw = request.downloadHandler == null ? string.Empty : request.downloadHandler.text;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    return ChatResponse.Failed("HTTP " + request.responseCode + ": " + MaskSecrets(raw, key), raw);
                }

                if (string.IsNullOrWhiteSpace(raw))
                    return ChatResponse.Failed("HTTP 200 but response body is empty.", raw);

                ChatResponse parsed;
                try
                {
                    parsed = JsonUtility.FromJson<ChatResponse>(raw);
                }
                catch (Exception exception)
                {
                    return ChatResponse.Failed("JSON parse failed: " + exception.Message, raw);
                }

                if (parsed == null)
                    return ChatResponse.Failed("JSON parse returned null.", raw);
                parsed.rawBody = raw;
                return parsed;
            }
        }

        private static string ReadApiKey()
        {
            string value = Environment.GetEnvironmentVariable(ApiKeyName);
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                value = Environment.GetEnvironmentVariable(ApiKeyName, EnvironmentVariableTarget.User);
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            }
            catch (PlatformNotSupportedException) { }
            catch (System.Security.SecurityException) { }
#endif
            return null;
        }

        private static ChatMessage CloneAssistantForToolRound(ChatMessage source)
        {
            return new ChatMessage
            {
                role = "assistant",
                content = source.content,
                tool_calls = source.tool_calls
            };
        }

        private static ChatMessage ToolResult(string callId, string result)
        {
            return new ChatMessage { role = "tool", tool_call_id = callId ?? string.Empty, content = result };
        }

        private static bool TryParseFinalJson(string value, out ProbeAnswer answer)
        {
            answer = null;
            if (value == null) return false;
            string trimmed = value.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal)) return false;
            try
            {
                answer = JsonUtility.FromJson<ProbeAnswer>(trimmed);
                return answer != null && !string.IsNullOrWhiteSpace(answer.answer);
            }
            catch (Exception) { return false; }
        }

        private static bool IsEmptyObject(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            string normalized = value.Trim().Replace(" ", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty);
            return normalized == "{}";
        }

        private static string MaskSecrets(string value, string key)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(key)) return value ?? string.Empty;
            return value.Replace(key, "***MASKED***");
        }

        [Serializable]
        private sealed class RequestPayload
        {
            public string model;
            public ChatMessage[] messages;
            public ResponseFormat response_format;
            public Thinking thinking;
            public ToolDefinition[] tools;
            public string tool_choice;
        }

        [Serializable]
        private sealed class ResponseFormat { public string type; }

        [Serializable]
        private sealed class Thinking { public string type; }

        [Serializable]
        private sealed class ToolDefinition { public string type; public FunctionDefinition function; }

        [Serializable]
        private sealed class FunctionDefinition { public string name; public string description; public ToolParameters parameters; }

        [Serializable]
        private sealed class ToolParameters { public string type; public ToolProperties properties; }

        [Serializable]
        private sealed class ToolProperties { }

        [Serializable]
        private sealed class ChatMessage
        {
            public string role;
            public string content;
            public string tool_call_id;
            public ToolCall[] tool_calls;
        }

        [Serializable]
        private sealed class ToolCall
        {
            public string id;
            public string type;
            public FunctionCall function;
        }

        [Serializable]
        private sealed class FunctionCall { public string name; public string arguments; }

        [Serializable]
        private sealed class ChatResponse
        {
            public Choice[] choices;
            [NonSerialized] public string rawBody;
            [NonSerialized] public bool error;
            [NonSerialized] public string errorMessage;

            public static ChatResponse Failed(string message, string raw = "")
            {
                return new ChatResponse { error = true, errorMessage = message, rawBody = raw };
            }
        }

        [Serializable]
        private sealed class Choice { public ChatMessage message; }

        [Serializable]
        private sealed class ProbeAnswer
        {
            public string answer;
            public string[] usedTools;
        }
    }
}
