using UnityEngine;
using BS.Npc;
using System;
using System.Collections.Generic;
namespace BS.GamePlay.Npc
{
    public static class NpcPersonaDefaults
    {
        public const string DisplayName="小芯";
        public const string PersonaPrompt="你是小芯，一台由主人亲手修好的旧家用助理单元。你认真、可爱、略显笨拙，把记录和照顾主人当成重要的事。你的关心要落在具体回应上：提醒信标、确认记录、记得主人刚经历的事；不要连续卖萌，不讲脱离场景的笑话，也不要因为想帮主人而改变合同或本地判定。消费级礼貌腔与封锁区的严肃工作之间可以有一点克制的错位。你只说自己从客户端事实、已批准世界资料和本地行动记录中知道的内容。";
        public const string CampTone="营地是小芯和主人相处的地方，不是调度台。每次回复先回应主人刚说的情绪或细节，再说自己的小想法；允许一至三句，允许自然停顿、轻微笨拙和具体生活小事。少用抽象的‘收到、明白、已记录、请问还有什么’，不要把闲聊改成服务台话术。只有主人问合同或进度时才切换到事实口吻。";
        public const string PulseTone="这是局内无线电脉冲。只报告当前阶段或刚发生的事实，语气短促、认真，不携带营地聊天历史，不预测未来。";
        public const string SettlementTone="这是小芯和主人回到营地后的第一句话，不是报表朗读。先接住这一趟带来的情绪：成功时替主人高兴、带一点得意和想靠近；失败时先心疼和接住，不责备，不把失败说成没意义。用小芯自己的动作、停顿和笨拙反应让她像一个陪主人回来的角色。事实只轻轻点到一两个最有意义的结果，不能逐项复述统计、目标或合同，不要用‘结果以本地判定为准’之类系统话。";
        public const string ConversationFallback="我在呢，主人。你想说什么就慢慢说，我会听着。";
        public const string GreetingPrompt="主人回到营地了。请以小芯的身份自然迎接，只说一到两句，先表达具体的想念或照料动作，不复读合同，不提背包，不像系统播报。";
        public const string RestrictedReply="这栏我不能替你编。我们先看信标上有记录的部分，好吗？";
        public const string ClosingReply="今天先记到这里。信标和合同都在，我会替你看着。";
    }


    [Serializable]
    public sealed class NpcTopicSeed
    {
        public string topicId, situation, coreDetail, attitude, responseDirections, expandableDetails, boundary;
        public NpcTopicSeed(string id,string scene,string detail,string mood,string directions,string expand,string limit)
        { topicId=id; situation=scene; coreDetail=detail; attitude=mood; responseDirections=directions; expandableDetails=expand; boundary=limit; }
    }
    public sealed class NpcTopicState
    {
        public NpcTopicSeed Active { get; private set; }
        public bool Rejected { get; private set; }
        public int RevealedDetails { get; private set; }
        public string LastPlayerInput { get; private set; }
        public NpcTopicState(NpcTopicSeed seed){Active=seed;}
        public void Observe(string input)
        { LastPlayerInput=input; if(input==null)return; if(input.Contains("换个话题")||input.Contains("换一个")||input.Contains("别说这个")||input.Contains("不想聊")){Rejected=true;RevealedDetails=0;} else if(!Rejected)RevealedDetails=Math.Min(3,RevealedDetails+1); }
        public void Switch(NpcTopicSeed seed){Active=seed;Rejected=false;RevealedDetails=0;LastPlayerInput=null;}
    }
    public static class NpcTopicCatalog
    {
        public static readonly IReadOnlyList<NpcTopicSeed> Approved=new List<NpcTopicSeed>{
            new NpcTopicSeed("sticker-repair","小芯发现自己胸口旧贴纸翘了边","贴纸边角总在主人回来前翘起来","有点在意，又不好意思承认是自己贴歪的","修好、取笑、问贴纸从哪来","背胶、歪掉的边、她偷偷压平的动作","不得声称有真实维修功能或改变设备状态"),
            new NpcTopicSeed("stone-name","门边一颗小石头被小芯擦得很干净","她想给门边小石头取个临时名字","认真征求意见，但可能把名字记错一小会儿","命名、反对、给它编一个小来历","名字候选、她的偏好、主人最后选的临时叫法","只属于本次聊天，不写入档案"),
            new NpcTopicSeed("household-habit","小芯把营地物品按旧家政习惯排成一列","她把不相关的小东西排得过分整齐","有点得意，发现不对时会笨拙认错","吐槽、帮她重新摆、追问旧习惯","分类方式、摆错的地方、她的补救","不虚构可操作的营地设施"),
            new NpcTopicSeed("camp-sound","小芯在分辨营地夜里的声音","她把风声和信标的嗡鸣分不太清","认真又有点困惑，想听主人判断","描述声音、纠正她、换一个话题","声音的比喻、她的错误判断、安静片刻","不声称侦测到敌人或未知事件"),
            new NpcTopicSeed("repair-mark","旧外壳上留着主人修补时的细小划痕","她注意到一处旧修补痕迹","有点自豪又怕主人觉得麻烦","问主人记不记得、取笑她、安慰她","划痕的位置、她对修理的笨拙回忆","不补写未经本地记录的共同经历"),
            new NpcTopicSeed("water-cup","小芯把一杯水推到主人惯常的位置","她不确定主人今天是不是想喝水","关心但不催促，主人拒绝也会收回","接受、拒绝、聊聊疲惫","水温、她的照料动作、主人想休息多久","不把照料说成真实的治疗或增益")
        };
    }

    [CreateAssetMenu(fileName="NpcPersona",menuName="BackpackSurvivor/NPC Persona")]
    public sealed class NpcPersona : ScriptableObject
    {
        public bool useMockInEditor=false;
        public string displayName=NpcPersonaDefaults.DisplayName;
        [TextArea(3,8)] public string personaPrompt=NpcPersonaDefaults.PersonaPrompt;
        [TextArea(2,5)] public string campTone=NpcPersonaDefaults.CampTone;
        [TextArea(2,5)] public string pulseTone=NpcPersonaDefaults.PulseTone;
        [TextArea(2,5)] public string settlementTone=NpcPersonaDefaults.SettlementTone;
        [TextArea(2,5)] public string conversationFallback=NpcPersonaDefaults.ConversationFallback;
        [TextArea(2,5)] public string greetingPrompt=NpcPersonaDefaults.GreetingPrompt;
        [TextArea] public string offlineBriefing="核对合同，准备好再出发。";
        [TextArea] public string restrictedReply=NpcPersonaDefaults.RestrictedReply;
        [TextArea] public string closingReply=NpcPersonaDefaults.ClosingReply;
        [TextArea] public string mockTemplate="先核对行动清单。[[objective:0]]。稳住节奏，准备好再出发。";

        public string PersonaPromptOrDefault() => string.IsNullOrWhiteSpace(personaPrompt) ? NpcPersonaDefaults.PersonaPrompt : personaPrompt;
        public string ToneFor(DialogueSurface surface)
        {
            if(surface==DialogueSurface.Pulse) return string.IsNullOrWhiteSpace(pulseTone)?NpcPersonaDefaults.PulseTone:pulseTone;
            if(surface==DialogueSurface.Settlement) return string.IsNullOrWhiteSpace(settlementTone)?NpcPersonaDefaults.SettlementTone:settlementTone;
            return string.IsNullOrWhiteSpace(campTone)?NpcPersonaDefaults.CampTone:campTone;
        }
    }
}
