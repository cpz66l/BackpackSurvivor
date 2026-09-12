using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace BS.GamePlay.Npc
{
    public sealed class NpcWireReply
    {
        public long status;
        public string raw, error;
        public JObject body;
        public bool streamDone;
        public int chunks;
    }

    // Injectable wire boundary: tests can exercise exactly the same service with malformed replies.
    public interface INpcTransport
    {
        Task<NpcWireReply> SendAsync(JObject request, string key, CancellationToken ct, Action<string> contentDelta = null);
    }

    public sealed class DeepSeekNpcDialogue : INpcTransport
    {
        readonly string endpoint;
        public DeepSeekNpcDialogue(string address="https://api.deepseek.com/chat/completions") { endpoint=address; }
        public async Task<NpcWireReply> SendAsync(JObject request, string key, CancellationToken ct, Action<string> contentDelta = null)
        {
            ct.ThrowIfCancellationRequested();
            bool streaming = request.Value<bool>("stream");
            using (var http = new UnityWebRequest(endpoint, "POST"))
            {
                http.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(request.ToString(Newtonsoft.Json.Formatting.None)));
                var stream = streaming ? new SseDownload(contentDelta) : null;
                http.downloadHandler = streaming ? (DownloadHandler)stream : new DownloadHandlerBuffer();
                http.SetRequestHeader("Content-Type", "application/json");
                http.SetRequestHeader("Authorization", "Bearer " + key);
                http.timeout = 30;
                var operation = http.SendWebRequest();
                // All UnityWebRequest interaction stays on the Unity synchronization context.
                while (!operation.isDone)
                {
                    if (ct.IsCancellationRequested) { http.Abort(); ct.ThrowIfCancellationRequested(); }
                    await Task.Yield();
                }
                ct.ThrowIfCancellationRequested();
                string raw = streaming ? stream.Raw.ToString() : http.downloadHandler.text;
                JObject body = null;
                string error = http.result == UnityWebRequest.Result.Success ? null : "HTTP " + http.responseCode + ": " + http.error;
                if (streaming)
                {
                    error = error ?? stream.Error;
                    body = new JObject { ["choices"] = new JArray(new JObject { ["message"] = new JObject { ["role"] = "assistant", ["content"] = stream.Text.ToString() } }), ["usage"] = stream.Usage };
                }
                else if (!string.IsNullOrWhiteSpace(raw))
                {
                    try { body = JObject.Parse(raw); }
                    catch (Exception) { error = error ?? "invalid_response_json"; }
                }
                return new NpcWireReply { status = http.responseCode, raw = raw, error = error, body = body,
                    streamDone = !streaming || stream.Done, chunks = streaming ? stream.Chunks : 0 };
            }
        }

        sealed class SseDownload : DownloadHandlerScript
        {
            readonly Decoder decoder = new UTF8Encoding(false, true).GetDecoder();
            readonly StringBuilder pending = new StringBuilder();
            readonly Action<string> delta;
            public readonly StringBuilder Raw = new StringBuilder(), Text = new StringBuilder();
            public bool Done;
            public int Chunks;
            public string Error;
            public JObject Usage;
            public SseDownload(Action<string> callback) : base(new byte[8192]) { delta = callback; }
            protected override bool ReceiveData(byte[] data, int length)
            {
                if (data == null || length == 0) return true;
                try
                {
                    char[] chars = new char[Encoding.UTF8.GetMaxCharCount(length)];
                    int n = decoder.GetChars(data, 0, length, chars, 0, false);
                    string decoded = new string(chars, 0, n);
                    if (Raw.Length + n > 262144) { Error = "response_too_large"; return false; }
                    Raw.Append(decoded); pending.Append(decoded);
                    int newline;
                    while ((newline = pending.ToString().IndexOf('\n')) >= 0)
                    {
                        string line = pending.ToString(0, newline).TrimEnd('\r'); pending.Remove(0, newline + 1);
                        if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
                        string payload = line.Substring(5).Trim();
                        if (payload == "[DONE]") { Done = true; continue; }
                        if (Done || payload.Length == 0) continue;
                        var chunk = JObject.Parse(payload); Chunks++;
                        if (chunk["usage"] is JObject usage) Usage = usage;
                        string content = (string)chunk["choices"]?[0]?["delta"]?["content"];
                        if (!string.IsNullOrEmpty(content)) { Text.Append(content); delta?.Invoke(content); }
                    }
                    return true;
                }
                catch (Exception) { Error = "invalid_sse"; return false; }
            }
        }
    }
}
