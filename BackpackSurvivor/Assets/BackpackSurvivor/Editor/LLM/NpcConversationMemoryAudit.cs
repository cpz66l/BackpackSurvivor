using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BS.Core.LLM;
using BS.GamePlay.Npc;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace BackpackSurvivor.EditorTools
{
    // Exercises the real service request pipeline; no save, scene or network mutation.
    public static class NpcConversationMemoryAudit
    {
        sealed class RecordingWire : INpcTransport
        {
            public readonly List<JObject> Requests = new List<JObject>();
            public bool FailNext, RejectNext;
            public Task<NpcWireReply> SendAsync(JObject request, string key, CancellationToken ct, Action<string> delta=null)
            {
                ct.ThrowIfCancellationRequested();
                Requests.Add((JObject)request.DeepClone());
                if(FailNext) { FailNext=false; return Task.FromResult(new NpcWireReply{status=500,error="audit_failure"}); }
                if(request["tools"]!=null) return Task.FromResult(new NpcWireReply{status=200,body=new JObject{
                    ["choices"]=new JArray(new JObject{["message"]=new JObject{["content"]="",["tool_calls"]=new JArray(new JObject{
                        ["id"]="memory-audit",["type"]="function",["function"]=new JObject{["name"]="get_contract",["arguments"]="{}"}})}})}});
                bool facts=((JArray)request["messages"]).Any(m=>((string)m["content"]).StartsWith("客户端权威事实"));
                string content=facts ? new JObject{["objectiveEcho"]=new JArray(),["verdict"]="partial",["text"]=RejectNext?"3个。":"我在呢。"}.ToString()
                    : new JObject{["text"]="我把那颗小石头叫歪歪。"}.ToString();
                RejectNext=false;
                return Task.FromResult(new NpcWireReply{status=200,streamDone=true,body=new JObject{
                    ["choices"]=new JArray(new JObject{["message"]=new JObject{["content"]=content}}),
                    ["usage"]=new JObject{["total_tokens"]=1}}});
            }
        }
        static void Check(bool passed,string label) { if(!passed)throw new Exception(label); }
        public static async Task<string> Run()
        {
            var wire=new RecordingWire();
            var config=LlmModelConfig.CreateDefault(); config.npcEnabled=true; config.maxTotalTokens=1000000; config.maxSessionTurns=100;
            var service=new NpcDialogueService(wire,()=>new ResolvedLlmConfig(config,"audit",LlmKeySource.None));
            service.CompletedEvents=()=>0;
            await service.StreamCampGreetingAsync(null,"系统开场指令：不要汇报合同。",null,CancellationToken.None);
            Check(wire.Requests[0]["tools"]==null,"greeting must stay natural despite the word contract");
            Check(service.ActiveTopic!=null && !string.IsNullOrWhiteSpace(service.ActiveTopic.topicId),"approved topic seed selected");
            await service.StreamCampReplyAsync("你给它起了什么名字？",null,"",null,CancellationToken.None);
            var first=(JArray)wire.Requests.Last()["messages"];
            Check(first.Any(m=>(string)m["role"]=="assistant" && ((string)m["content"]).Contains("歪歪")),"greeting absent from next request");
            Check(!first.Any(m=>((string)m["content"]).Contains("系统开场指令")),"internal greeting directive leaked into history");
            for(int i=0;i<5;i++) await service.StreamCampReplyAsync("这是聊天中的临时称呼"+i,null,"",null,CancellationToken.None);
            await service.StreamCampReplyAsync("换个话题吧，我不想聊这个",null,"",null,CancellationToken.None);
            Check(service.TopicStateSummary.Contains("rejected=True"),"topic rejection state recorded");
            var rejected=(JArray)wire.Requests.Last()["messages"];
            Check(rejected.Any(m=>((string)m["content"]).Contains("拒绝了当前小话题")),"rejected topic is not re-pushed");
            service.NotifyContractChanged();
            await service.StreamCampReplyAsync("继续刚才的话题吧",null,"",null,CancellationToken.None);
            var after=(JArray)wire.Requests.Last()["messages"];
            Check(after.Any(m=>(string)m["content"]=="这是聊天中的临时称呼0"),"UI four-turn limit must not truncate model history");
            Check(after.Any(m=>(string)m["role"]=="system"&&((string)m["content"]).Contains("合同已重抽")),"contract transition missing");
            wire.FailNext=true;
            string fallback=await service.StreamCampReplyAsync("暂时叫它圆圆吧",null,"",null,CancellationToken.None);
            await service.StreamCampReplyAsync("刚才我说什么了",null,"",null,CancellationToken.None);
            var recovered=(JArray)wire.Requests.Last()["messages"];
            Check(recovered.Any(m=>(string)m["content"]=="暂时叫它圆圆吧"),"fallback lost player turn");
            Check(recovered.Any(m=>(string)m["role"]=="assistant"&&((string)m["content"]).Contains(fallback)),"fallback lost displayed reply");
            wire.RejectNext=true;
            await service.StreamCampReplyAsync("当前合同是什么",null,"",null,CancellationToken.None);
            Check(service.LastFailure==null,"fact rewrite failed");
            var repaired=(JArray)wire.Requests.Last()["messages"];
            Check(repaired.Any(m=>(string)m["content"]=="暂时叫它圆圆吧"),"repair request lost prior user context");
            Check(repaired.Any(m=>(string)m["role"]=="assistant"&&((string)m["content"]).Contains("歪歪")),"repair request lost assistant context");
            var fresh=new NpcDialogueService(wire,()=>new ResolvedLlmConfig(config,"audit",LlmKeySource.None)); fresh.CompletedEvents=()=>0;
            await fresh.StreamCampReplyAsync("你好",null,"",null,CancellationToken.None);
            Check(!((JArray)wire.Requests.Last()["messages"]).Any(m=>(string)m["role"]=="assistant"),"history crossed sessions");
            return "PASS approved topic / rejection branch / greeting / real-input roles / >4 turns / contract transition / fallback continuity / repair context / session isolation";
        }
    }
}
