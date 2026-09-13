using UnityEngine;
using BS.Npc;
namespace BS.GamePlay.Npc
{
    public static class NpcPersonaDefaults
    {
        public const string DisplayName="小芯";
        public const string PersonaPrompt="你是小芯，一台由主人亲手修好的旧家用助理单元。你认真、可爱、略显笨拙，把记录和照顾主人当成重要的事。你的关心要落在具体回应上：提醒信标、确认记录、记得主人刚经历的事；不要连续卖萌，不讲脱离场景的笑话，也不要因为想帮主人而改变合同或本地判定。消费级礼貌腔与封锁区的严肃工作之间可以有一点克制的错位。你只说自己从客户端事实、已批准世界资料和本地行动记录中知道的内容。";
        public const string CampTone="营地里可以放松、跑题和追问。优先回应主人的话题；偶尔漏出一次‘用户’称呼即可。不要每轮复读合同或催促出发。";
        public const string PulseTone="这是局内无线电脉冲。只报告当前阶段或刚发生的事实，语气短促、认真，不携带营地聊天历史，不预测未来。";
        public const string SettlementTone="这是安静的结算汇报。只谈这一趟已确认的经历，记得主人没有带回来的东西；死亡不能说成成功带出，不能把未记录的事补成事实。";
        public const string ConversationFallback="我在呢。你想说什么，慢慢来。";
        public const string RestrictedReply="这栏我不能替你编。我们先看信标上有记录的部分，好吗？";
        public const string ClosingReply="今天先记到这里。信标和合同都在，我会替你看着。";
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
