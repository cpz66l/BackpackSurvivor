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
    /// S1 only: validates DeepSeek SSE framing and UTF-8/tool-call delta assembly.
    /// It is deliberately editor-only and does not touch gameplay or save state.
    /// </summary>
    public static class DeepSeekS1StreamingProbe
    {
        private const string ApiKeyName = "DEEPSEEK_API_KEY";
        private const string Endpoint = "https://api.deepseek.com/chat/completions";
        private const string Model = "deepseek-flash";
        private const int TimeoutSeconds = 30;
        private const string ProbeToolName = "echo_stream_probe";
        private const string ExpectedProbeText = "流式中文 UTF-8 分片验证。";
        private static UnityWebRequest activeRequest;
        private static bool cancelRequested;

        [MenuItem("Tools/Backpack Survivor/LLM/S1 Probe DeepSeek (Streaming)")]
        public static async void Run()
        {
            if (activeRequest != null)
            {
                Debug.LogWarning("[LLM S1] 已有流式请求正在运行。");
                return;
            }

            string key = ReadApiKey();
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogError("[LLM S1] DEEPSEEK_API_KEY 未配置。未读取或写入任何配置文件。");
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            StreamingSseHandler handler = new StreamingSseHandler(stopwatch);
            handler.EventReceived += OnSseEvent;
            cancelRequested = false;

            try
            {
                RequestPayload payload = BuildPayload();
                string body = JsonUtility.ToJson(payload);
                using (UnityWebRequest request = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST))
                {
                    activeRequest = request;
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.downloadHandler = handler;
                    request.timeout = TimeoutSeconds;
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", "Bearer " + key);

                    Debug.Log("[LLM S1] starting stream; model=" + Model + "; thinking=disabled; tool_choice=required");
                    await request.SendWebRequest();

                    if (cancelRequested || request.result == UnityWebRequest.Result.ProtocolError && request.responseCode == 0)
                    {
                        Debug.LogWarning("[LLM S1] stream cancelled by editor action.");
                        return;
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError("[LLM S1] 请求失败：HTTP " + request.responseCode + " " + request.error);
                        return;
                    }

                    handler.FlushPending();
                    Debug.Log("[LLM S1] summary: firstByteMs=" + handler.FirstByteMs +
                              ", firstSseDataMs=" + handler.FirstSseDataMs +
                              ", firstTokenMs=" + handler.FirstTokenMs +
                              ", totalMs=" + stopwatch.ElapsedMilliseconds +
                              ", sseEvents=" + handler.EventCount +
                              ", parsedChunks=" + handler.ParsedChunkCount +
                              ", parseFailures=" + handler.ParseFailureCount +
                              ", decodeFailures=" + handler.DecodeFailureCount +
                              ", doneSeen=" + handler.DoneSeen +
                              ", utf8Complete=" + handler.Utf8Complete);
                    Debug.Log("[LLM S1] assembled content:\n" + handler.ContentText);
                    foreach (KeyValuePair<int, StringBuilder> pair in handler.ToolArguments)
                    {
                        string toolName = handler.ToolNames.ContainsKey(pair.Key) ? handler.ToolNames[pair.Key] : string.Empty;
                        bool valid = false;
                        try
                        {
                            StreamProbeArguments arguments = JsonUtility.FromJson<StreamProbeArguments>(pair.Value.ToString());
                            valid = arguments != null && arguments.probe_text == ExpectedProbeText;
                        }
                        catch (Exception) { valid = false; }
                        int fragmentCount = handler.ToolFragmentCounts.ContainsKey(pair.Key) ? handler.ToolFragmentCounts[pair.Key] : 0;
                        Debug.Log("[LLM S1][tool audit] assembled index=" + pair.Key + " name=" + toolName +
                                  " fragments=" + fragmentCount + " arguments=" + pair.Value + " validated=" + valid);
                    }
                }
            }
            catch (Exception exception)
            {
                if (cancelRequested)
                    Debug.LogWarning("[LLM S1] stream cancelled; no gameplay state was changed. detail=" + MaskSecrets(exception.Message, key));
                else
                    Debug.LogError("[LLM S1] 未处理异常：" + MaskSecrets(exception.Message, key));
            }
            finally
            {
                activeRequest = null;
                StreamingSseHandler.ClearCurrent();
                handler.EventReceived -= OnSseEvent;
                stopwatch.Stop();
            }
        }

        [MenuItem("Tools/Backpack Survivor/LLM/S1 Cancel Active Stream")]
        public static void Cancel()
        {
            if (activeRequest == null)
            {
                Debug.Log("[LLM S1] 当前没有活动流式请求。");
                return;
            }

            cancelRequested = true;
            activeRequest.Abort();
            Debug.Log("[LLM S1] 已请求中断活动流式请求。");
        }

        private static void OnSseEvent(string payload)
        {
            StreamingSseHandler handler = StreamingSseHandler.Current;
            if (handler == null || string.IsNullOrWhiteSpace(payload) || payload == "[DONE]") return;

            StreamResponse response;
            try
            {
                response = JsonUtility.FromJson<StreamResponse>(payload);
            }
            catch (Exception exception)
            {
                handler.MarkParseFailure(exception.Message);
                return;
            }

            if (response == null || response.choices == null || response.choices.Length == 0)
            {
                handler.MarkParseFailure("missing choices");
                return;
            }

            foreach (StreamChoice choice in response.choices)
            {
                if (choice == null) continue;
                handler.RecordFinishReason(choice.finish_reason);
                StreamDelta delta = choice.delta;
                if (delta == null) continue;
                if (!string.IsNullOrEmpty(delta.content)) handler.AppendContent(delta.content);
                if (delta.tool_calls == null) continue;
                foreach (ToolCallDelta toolCall in delta.tool_calls)
                {
                    if (toolCall == null || toolCall.function == null) continue;
                    handler.AppendToolDelta(toolCall.index, toolCall.id, toolCall.type,
                        toolCall.function.name, toolCall.function.arguments);
                }
            }
            handler.ParsedChunkCount++;
        }

        private static RequestPayload BuildPayload()
        {
            return new RequestPayload
            {
                model = Model,
                stream = true,
                messages = new[]
                {
                    new ChatMessage
                    {
                        role = "system",
                        content = "You are an SSE compatibility probe. Use the supplied read-only tool exactly once, then stop."
                    },
                    new ChatMessage
                    {
                        role = "user",
                        content = "Call echo_stream_probe with probe_text exactly equal to: " + ExpectedProbeText + " Do not add extra text."
                    }
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
                            description = "Read-only streaming probe. It never changes game state.",
                            parameters = new ToolParameters
                            {
                                type = "object",
                                properties = new ToolProperties
                                {
                                    probe_text = new ToolProperty { type = "string", description = "The exact supplied probe text." }
                                },
                                required = new[] { "probe_text" }
                            }
                        }
                    }
                },
                tool_choice = "required"
            };
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

        private static string MaskSecrets(string value, string key)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(key)) return value ?? string.Empty;
            return value.Replace(key, "***MASKED***");
        }

        [Serializable]
        private sealed class RequestPayload
        {
            public string model;
            public bool stream;
            public ChatMessage[] messages;
            public Thinking thinking;
            public ToolDefinition[] tools;
            public string tool_choice;
        }

        [Serializable] private sealed class Thinking { public string type; }
        [Serializable] private sealed class ToolDefinition { public string type; public FunctionDefinition function; }
        [Serializable] private sealed class FunctionDefinition { public string name; public string description; public ToolParameters parameters; }
        [Serializable] private sealed class ToolParameters { public string type; public ToolProperties properties; public string[] required; }
        [Serializable] private sealed class ToolProperties { public ToolProperty probe_text; }
        [Serializable] private sealed class ToolProperty { public string type; public string description; }
        [Serializable] private sealed class ChatMessage { public string role; public string content; }

        [Serializable] private sealed class StreamResponse { public StreamChoice[] choices; }
        [Serializable] private sealed class StreamChoice { public StreamDelta delta; public string finish_reason; }
        [Serializable] private sealed class StreamDelta { public string role; public string content; public ToolCallDelta[] tool_calls; }
        [Serializable] private sealed class ToolCallDelta { public int index; public string id; public string type; public FunctionDelta function; }
        [Serializable] private sealed class FunctionDelta { public string name; public string arguments; }
        [Serializable] private sealed class StreamProbeArguments { public string probe_text; }

        private sealed class StreamingSseHandler : DownloadHandlerScript
        {
            private static StreamingSseHandler current;
            private readonly Stopwatch stopwatch;
            private readonly Decoder decoder = new UTF8Encoding(false, true).GetDecoder();
            private readonly char[] charBuffer = new char[8192];
            private readonly StringBuilder pending = new StringBuilder();
            private readonly StringBuilder eventData = new StringBuilder();
            private readonly StringBuilder content = new StringBuilder();
            private readonly Dictionary<int, StringBuilder> toolArguments = new Dictionary<int, StringBuilder>();
            private readonly Dictionary<int, string> toolNames = new Dictionary<int, string>();
            private readonly Dictionary<int, int> toolFragmentCounts = new Dictionary<int, int>();
            private bool eventHasData;
            private bool sawByte;

            public StreamingSseHandler(Stopwatch stopwatch) : base(new byte[8192])
            {
                this.stopwatch = stopwatch;
                current = this;
            }

            public static StreamingSseHandler Current { get { return current; } }
            public event Action<string> EventReceived;
            public int EventCount { get; private set; }
            public int ParsedChunkCount { get; set; }
            public int ParseFailureCount { get; private set; }
            public bool DoneSeen { get; private set; }
            public bool Utf8Complete { get; private set; }
            public int DecodeFailureCount { get; private set; }
            public long FirstByteMs { get; private set; } = -1;
            public long FirstSseDataMs { get; private set; } = -1;
            public long FirstTokenMs { get; private set; } = -1;
            public string ContentText { get { return content.ToString(); } }
            public Dictionary<int, StringBuilder> ToolArguments { get { return toolArguments; } }
            public Dictionary<int, string> ToolNames { get { return toolNames; } }
            public Dictionary<int, int> ToolFragmentCounts { get { return toolFragmentCounts; } }

            public static void ClearCurrent()
            {
                current = null;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength <= 0) return true;
                if (!sawByte) FirstByteMs = stopwatch.ElapsedMilliseconds;
                sawByte = true;
                try
                {
                    int charCount = decoder.GetChars(data, 0, dataLength, charBuffer, 0, false);
                    pending.Append(charBuffer, 0, charCount);
                    ParseLines();
                }
                catch (DecoderFallbackException exception)
                {
                    DecodeFailureCount++;
                    Utf8Complete = false;
                    Debug.LogError("[LLM S1] UTF-8 decode failure: " + exception.Message);
                }
                return true;
            }

            protected override void CompleteContent()
            {
                try
                {
                    int charCount = decoder.GetChars(Array.Empty<byte>(), 0, 0, charBuffer, 0, true);
                    pending.Append(charBuffer, 0, charCount);
                    ParseLines();
                    if (pending.Length > 0) ConsumeLine(pending.ToString());
                    FlushPending();
                    Utf8Complete = sawByte && DecodeFailureCount == 0;
                }
                catch (DecoderFallbackException exception)
                {
                    DecodeFailureCount++;
                    Utf8Complete = false;
                    Debug.LogError("[LLM S1] UTF-8 final decode failure: " + exception.Message);
                }
            }

            public void FlushPending()
            {
                if (eventData.Length > 0) FlushEvent();
            }

            public void MarkParseFailure(string reason)
            {
                ParseFailureCount++;
                Debug.LogWarning("[LLM S1] SSE JSON parse failure: " + reason);
            }

            public void AppendContent(string value)
            {
                if (string.IsNullOrEmpty(value)) return;
                MarkFirstToken();
                content.Append(value);
                Debug.Log("[LLM S1][chunk] content=" + value);
            }

            public void AppendToolDelta(int index, string id, string type, string name, string arguments)
            {
                MarkFirstToken();
                if (!toolArguments.ContainsKey(index)) toolArguments[index] = new StringBuilder();
                if (!toolFragmentCounts.ContainsKey(index)) toolFragmentCounts[index] = 0;
                toolFragmentCounts[index]++;
                if (!string.IsNullOrEmpty(name)) toolNames[index] = name;
                if (!string.IsNullOrEmpty(arguments)) toolArguments[index].Append(arguments);
                Debug.Log("[LLM S1][chunk] tool index=" + index + " id=" + (id ?? string.Empty) + " type=" + (type ?? string.Empty) + " name=" + (name ?? string.Empty) + " arguments=" + (arguments ?? string.Empty));
            }

            public void RecordFinishReason(string reason)
            {
                if (!string.IsNullOrEmpty(reason)) Debug.Log("[LLM S1][finish] " + reason);
            }

            private void MarkFirstToken()
            {
                if (FirstTokenMs < 0) FirstTokenMs = stopwatch.ElapsedMilliseconds;
            }

            private void ParseLines()
            {
                while (true)
                {
                    int newline = pending.ToString().IndexOf('\n');
                    if (newline < 0) break;
                    string line = pending.ToString(0, newline).TrimEnd('\r');
                    pending.Remove(0, newline + 1);
                    ConsumeLine(line);
                }
            }

            private void ConsumeLine(string line)
            {
                if (string.IsNullOrEmpty(line))
                {
                    FlushEvent();
                    return;
                }
                if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    string value = line.Length > 5 && line[5] == ' ' ? line.Substring(6) : line.Substring(5);
                    if (FirstSseDataMs < 0) FirstSseDataMs = stopwatch.ElapsedMilliseconds;
                    if (eventHasData) eventData.Append('\n');
                    eventData.Append(value);
                    eventHasData = true;
                }
            }

            private void FlushEvent()
            {
                string payload = eventData.ToString();
                eventData.Length = 0;
                eventHasData = false;
                if (string.IsNullOrEmpty(payload)) return;
                EventCount++;
                if (payload == "[DONE]")
                {
                    DoneSeen = true;
                    Debug.Log("[LLM S1][SSE] data: [DONE]");
                    return;
                }
                Debug.Log("[LLM S1][SSE event=" + EventCount + "] " + payload);
                EventReceived?.Invoke(payload);
            }
        }
    }
}
