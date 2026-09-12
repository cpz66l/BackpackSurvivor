using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace BS.GamePlay.Npc
{
    public sealed class MockNpcDialogue : INpcTransport
    {
        readonly NpcPersona persona;
        public MockNpcDialogue(NpcPersona local){persona=local;}
        public async Task<NpcWireReply> SendAsync(JObject request,string key,CancellationToken ct,Action<string> delta=null)
        {
            ct.ThrowIfCancellationRequested();
            JObject body;
            if(request["tools"] is JArray tools)
            {
                string name=tools.Any(t=>(string)t["function"]?["name"]=="get_contract")?"get_contract":"get_run_state";
                body=new JObject{["choices"]=new JArray(new JObject{["message"]=new JObject{["role"]="assistant",["content"]="",["tool_calls"]=new JArray(new JObject{["id"]="mock-call",["type"]="function",["function"]=new JObject{["name"]=name,["arguments"]="{}"}})}})};
            }
            else
            {
                string factText=((JArray)request["messages"]).Select(m=>(string)m["content"]).First(t=>t!=null&&t.StartsWith("客户端权威事实"));
                var facts=JObject.Parse(factText.Substring(factText.IndexOf('\n')+1));
                int count=((JArray)facts["objectives"]).Count;
                string template=count>0?persona.mockTemplate:"稳住节奏，准备好再出发。";
                string content=new JObject{["objectiveEcho"]=new JArray(Enumerable.Range(0,count)),["text"]=template,["verdict"]=facts["verdict"].DeepClone()}.ToString(Newtonsoft.Json.Formatting.None);
                if(request.Value<bool>("stream"))for(int i=0;i<content.Length;i+=8){ct.ThrowIfCancellationRequested();delta?.Invoke(content.Substring(i,Math.Min(8,content.Length-i)));await Task.Yield();}
                body=new JObject{["choices"]=new JArray(new JObject{["message"]=new JObject{["content"]=content}})};
            }
            body["usage"]=new JObject{["total_tokens"]=100};
            return new NpcWireReply{status=200,body=body,raw="MOCK "+body.ToString(Newtonsoft.Json.Formatting.None),streamDone=true,chunks=request.Value<bool>("stream")?10:0};
        }
    }
}
