using System;
using System.Text;
using System.Threading.Tasks;
using BS.Core.LLM;
using BS.Npc;
using BS.Quest;
using UnityEngine;
using UnityEngine.Networking;

namespace BS.GamePlay.Npc
{
    public sealed class NpcDialogueService
    {
        [Serializable] class Request { public string model="deepseek-flash"; public Message[] messages; public bool stream=false; public Thinking thinking=new Thinking(); public Tool[] tools; public string tool_choice="auto"; }
        [Serializable] class Thinking { public string type="disabled"; }
        [Serializable] class Message { public string role; public string content; public ToolCall[] tool_calls; public string tool_call_id; }
        [Serializable] class Tool { public string type="function"; public Function function; }
        [Serializable] class Function { public string name; public string description; public Parameters parameters; }
        [Serializable] class Parameters { public string type="object"; }
        [Serializable] class ToolCall { public string id; public FunctionCall function; }
        [Serializable] class FunctionCall { public string name; public string arguments; }
        [Serializable] class Response { public Choice[] choices; }
        [Serializable] class Choice { public Message message; }
        public async Task<string> RequestCampReplyAsync(string playerText, QuestInstance quest, QuestRunSnapshot snapshot, string offline)
        { return await RequestReplyAsync(DialogueSurface.Camp, playerText, quest, snapshot, offline); }
        public async Task<string> RequestPulseReplyAsync(string stage, QuestInstance quest, QuestRunSnapshot snapshot, string offline)
        { return await RequestReplyAsync(DialogueSurface.Pulse, "当前阶段="+stage, quest, snapshot, offline); }
        async Task<string> RequestReplyAsync(DialogueSurface surface, string playerText, QuestInstance quest, QuestRunSnapshot snapshot, string offline)
        {
            if (!DialogueRouter.TryRoute(surface, out int max)) return offline;
            string facts=FactBlockBuilder.Build(quest,snapshot);
            ResolvedLlmConfig config=LlmConfigService.Resolve();
            if (string.IsNullOrEmpty(config.ApiKey)) return offline;
            var body=new Request { tools=new[]{new Tool { function=new Function { name="read_contract_facts", description="读取当前合同的只读事实，不接受参数。", parameters=new Parameters() } }}, messages=new[]{new Message{role="system",content="你是只读 NPC。只陈述事实，不承诺任务完成。事实区是唯一权威数值来源。"},new Message{role="system",content=facts},new Message{role="user",content=playerText}}};
            string raw=await SendAsync(JsonUtility.ToJson(body),config.ApiKey);
            Debug.Log("[NPC audit] raw="+Mask(raw,config.ApiKey));
            if (string.IsNullOrEmpty(raw)) return offline;
            Response response=JsonUtility.FromJson<Response>(raw);
            Message assistant=response?.choices != null && response.choices.Length>0 ? response.choices[0].message : null;
            ToolCall[] calls=assistant == null ? null : assistant.tool_calls;
            bool auditedTool = calls != null && calls.Length == 1 && calls[0] != null && calls[0].function != null && calls[0].function.name == "read_contract_facts" && (string.IsNullOrEmpty(calls[0].function.arguments) || calls[0].function.arguments.Trim() == "{}");
            if (calls != null && calls.Length > 0)
            {
                if (!auditedTool) return offline;
                var followup = new Request { messages = new[] { body.messages[0], body.messages[1], body.messages[2], assistant, new Message { role="tool", tool_call_id=calls[0].id, content=facts } } };
                string secondRaw=await SendAsync(JsonUtility.ToJson(followup),config.ApiKey);
                Debug.Log("[NPC audit] raw-final="+Mask(secondRaw,config.ApiKey));
                response=JsonUtility.FromJson<Response>(secondRaw);
            }
            string text=response?.choices != null && response.choices.Length>0 && response.choices[0].message != null ? response.choices[0].message.content : string.Empty;
            Debug.Log("[NPC audit] tool=" + (auditedTool ? "read_contract_facts:executed(read-only)" : "none") + "; facts-bound=true");
            return NpcResponseValidator.IsSafe(text,max) ? text : offline;
        }
        static async Task<string> SendAsync(string json,string key)
        {
            using(var req=new UnityWebRequest("https://api.deepseek.com/chat/completions","POST"))
            {
                byte[] bytes=Encoding.UTF8.GetBytes(json); req.uploadHandler=new UploadHandlerRaw(bytes); req.downloadHandler=new DownloadHandlerBuffer(); req.SetRequestHeader("Content-Type","application/json"); req.SetRequestHeader("Authorization","Bearer "+key); req.timeout=30;
                var op=req.SendWebRequest(); while(!op.isDone) await Task.Yield();
                return req.result==UnityWebRequest.Result.Success ? req.downloadHandler.text : string.Empty;
            }
        }
        static string Mask(string value,string key) { return string.IsNullOrEmpty(key) ? value : value.Replace(key,"***MASKED***"); }
    }
}
