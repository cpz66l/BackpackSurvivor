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
        [Serializable] class Request { public string model="deepseek-chat"; public Message[] messages; public bool stream=false; public Thinking thinking=new Thinking(); }
        [Serializable] class Thinking { public string type="disabled"; }
        [Serializable] class Message { public string role; public string content; }
        [Serializable] class Response { public Choice[] choices; }
        [Serializable] class Choice { public Message message; }
        public async Task<string> RequestCampReplyAsync(string playerText, QuestInstance quest, QuestRunSnapshot snapshot, string offline)
        {
            if (!DialogueRouter.TryRoute(DialogueSurface.Camp, out int max)) return offline;
            string facts=FactBlockBuilder.Build(quest,snapshot);
            ResolvedLlmConfig config=LlmConfigService.Resolve();
            if (string.IsNullOrEmpty(config.ApiKey)) return offline;
            var body=new Request { messages=new[]{new Message{role="system",content="你是只读 NPC。只陈述事实，不承诺任务完成。"},new Message{role="system",content=facts},new Message{role="user",content=playerText}}};
            string json=JsonUtility.ToJson(body); using(var req=new UnityWebRequest("https://api.deepseek.com/chat/completions","POST"))
            {
                byte[] bytes=Encoding.UTF8.GetBytes(json); req.uploadHandler=new UploadHandlerRaw(bytes); req.downloadHandler=new DownloadHandlerBuffer(); req.SetRequestHeader("Content-Type","application/json"); req.SetRequestHeader("Authorization","Bearer "+config.ApiKey); req.timeout=30;
                var op=req.SendWebRequest(); while(!op.isDone) await Task.Yield();
                string raw=req.downloadHandler.text; Debug.Log("[NPC audit] response="+raw);
                if(req.result!=UnityWebRequest.Result.Success) return offline;
                Response response=JsonUtility.FromJson<Response>(raw); string text=response?.choices != null && response.choices.Length>0 ? response.choices[0].message.content : string.Empty;
                Debug.Log("[NPC audit] tools=none; facts-bound=true");
                return NpcResponseValidator.IsSafe(text,max) ? text : offline;
            }
        }
    }
}
