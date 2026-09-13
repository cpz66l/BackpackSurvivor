using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BS.Core.LLM;
using BS.GamePlay.Npc;
using BS.GamePlay.Quest;
using Newtonsoft.Json.Linq;
using UnityEngine;
using TMPro;

namespace BackpackSurvivor.EditorTools
{
    public static class NpcRecordStreamingAudit
    {
        sealed class Wire : INpcTransport
        {
            public bool Finished, Incomplete, WrongRecord;
            public readonly List<JObject> Requests=new List<JObject>();
            public Task<NpcWireReply> SendAsync(JObject request,string key,CancellationToken ct,Action<string> delta=null)
            {
                ct.ThrowIfCancellationRequested(); Requests.Add((JObject)request.DeepClone());
                var messages=(JArray)request["messages"];
                if(request["tools"] is JArray tools)
                {
                    Check(tools.Count==1 && (string)tools[0]["function"]["name"]=="get_player_records","record query offered wrong tools");
                    return Task.FromResult(new NpcWireReply{status=200,body=new JObject{["choices"]=new JArray(new JObject{["message"]=new JObject{["tool_calls"]=new JArray(new JObject{["id"]="record-test",["type"]="function",["function"]=new JObject{["name"]="get_player_records",["arguments"]="{}"}})}})}});
                }
                bool record=messages.Any(m=>(string)m["role"]=="tool");
                var answer=record?new JObject{["text"]=WrongRecord?"99999。":"主人，[[record:0]]。"}:new JObject{["text"]="主人，我在呢。慢慢说。"};
                string text=answer.ToString(Newtonsoft.Json.Formatting.None); Finished=false;
                for(int i=0;i<text.Length;i++) { ct.ThrowIfCancellationRequested(); delta?.Invoke(text.Substring(i,1)); }
                Finished=true;
                return Task.FromResult(new NpcWireReply{status=200,streamDone=!Incomplete,body=new JObject{["choices"]=new JArray(new JObject{["message"]=new JObject{["content"]=text}}),["usage"]=new JObject{["total_tokens"]=1}}});
            }
        }
        static void Check(bool value,string label){if(!value)throw new Exception(label);}
        static NpcDialogueService Service(Wire wire,int? best)
        {
            var config=LlmModelConfig.CreateDefault(); config.npcEnabled=true; config.maxTotalTokens=1000000;
            var s=new NpcDialogueService(wire,()=>new ResolvedLlmConfig(config,"audit",LlmKeySource.None)); s.CompletedEvents=()=>0; s.BestBackpackValue=()=>best; return s;
        }
        public static async Task<string> Run()
        {
            foreach(string question in new[]{"最高带回价值是多少？","最高记录是多少？"})
            {
                var w=new Wire(); var s=Service(w,12345); string streamed="";
                var quest=new BS.Quest.QuestInstance{objectives=new List<BS.Quest.ObjectiveClause>{new BS.Quest.ObjectiveClause{type=BS.Quest.ObjectiveType.CarryTag,count=2}}};
                string result=await s.StreamCampReplyAsync(question,quest,"错误合同简报",x=>streamed+=x,CancellationToken.None);
                Check(s.LastFailure==null && s.ToolCount==1,"record service failed");
                Check(result.Contains("12,345") && !result.Contains("[[") && result==streamed,"record display binding failed");
                var refill=((JArray)w.Requests.Last()["messages"]).First(m=>(string)m["role"]=="tool");
                Check(((string)refill["content"]).Contains("SaveData.bestBackpackValue"),"record source missing");
            }
            foreach(int? value in new int?[]{0,null})
            {
                var s=Service(new Wire(),value); string text=await s.StreamCampReplyAsync("最高记录是多少？",null,"",null,CancellationToken.None);
                Check(s.LastFailure==null && (value.HasValue?text.Contains("￥0"):text.Contains("无法读取")),"zero/unavailable conflated");
            }
            var wrong=Service(new Wire{WrongRecord=true},12345);
            string fixedText=await wrong.StreamCampReplyAsync("最高记录是多少？",null,"合同简报",null,CancellationToken.None);
            Check(wrong.UsedFallback && fixedText.Contains("12,345") && !fixedText.Contains("99999") && !fixedText.Contains("合同简报"),"wrong model number leaked or fallback lost record");
            var wire=new Wire(); var chat=Service(wire,0); bool beforeDone=false; string chunks="";
            string reply=await chat.StreamCampReplyAsync("陪我聊聊吧",null,"",x=>{beforeDone|=!wire.Finished;chunks+=x;},CancellationToken.None);
            Check(beforeDone && chunks==reply && chat.LastFailure==null,"natural stream waited for DONE or duplicated characters");
            string prefix;
            Check(NpcDialogueService.TryTextPrefix("{\"text\":\"\\u4F60\\u597D。",-1,out prefix)&&prefix=="你好。","escaped Unicode prefix decode failed");
            var partial=Service(new Wire{Incomplete=true},0); string partialChunks="";
            string partialText=await partial.StreamCampReplyAsync("你好",null,"",x=>partialChunks+=x,CancellationToken.None);
            Check(partial.LastFailure!=null && partialText==partialChunks && partialText.StartsWith("主人，我在呢。"),"incomplete stream lost safe prefix");
            CheckTypewriter();
            return "PASS two exact queries / record source and binding / zero vs unavailable / wrong-number fallback / SSE before DONE / Unicode / incomplete stream / UI one-character reveal and cancel";
        }
        static void CheckTypewriter()
        {
            var go=new GameObject("MemoryTypewriterAudit");
            try
            {
                var label=go.AddComponent<TextMeshProUGUI>(); var camp=go.AddComponent<CampController>(); camp.enabled=false;
                camp.Configure(null,null,null,null,null,null,null,null,label);
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                Action<string,object[]> call=(name,args)=>typeof(CampController).GetMethod(name,flags).Invoke(camp,args);
                call("BeginTyping",new object[]{"小芯："}); call("QueueTyping",new object[]{"你好😀。"});
                call("AdvanceTyping",new object[]{1f/45f}); Check(label.text=="小芯：你","first frame dumped whole reply");
                call("AdvanceTyping",new object[]{1f/45f}); Check(label.text=="小芯：你好","second character missing");
                call("AdvanceTyping",new object[]{1f/45f}); Check(label.text=="小芯：你好😀","surrogate pair split");
                call("CancelReply",new object[0]); string frozen=label.text;
                call("AdvanceTyping",new object[]{1f}); Check(label.text==frozen,"cancelled text kept appearing");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
