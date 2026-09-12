using UnityEngine;
namespace BS.GamePlay.Npc
{
    [CreateAssetMenu(fileName="NpcPersona",menuName="BackpackSurvivor/NPC Persona")]
    public sealed class NpcPersona : ScriptableObject
    {
        public bool useMockInEditor=true;
        [TextArea] public string offlineBriefing="核对合同，准备好再出发。";
        [TextArea] public string restrictedReply="先把注意力放回行动清单。按合同准备，现场稳住节奏。";
        [TextArea] public string closingReply="今天先谈到这里。核对合同，准备好就出发。";
        [TextArea] public string mockTemplate="先核对行动清单。[[objective:0]]。稳住节奏，准备好再出发。";
    }
}
