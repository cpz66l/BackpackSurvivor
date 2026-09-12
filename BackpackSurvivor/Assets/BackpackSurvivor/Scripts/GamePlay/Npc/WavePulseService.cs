using System;
using System.Threading.Tasks;
using System.Threading;
using BS.GamePlay.Waves;
using UnityEngine;
using BS.GamePlay.Run;
using BS.Quest;

namespace BS.GamePlay.Npc
{
    public sealed class WavePulseService : MonoBehaviour
    {
        public static bool IsPulseCurrent(int requestToken, int activeToken, int stageIndex, int activeStage, int sessionId, int activeSessionId, float ageSeconds, float ttlSeconds)
        { return requestToken == activeToken && stageIndex == activeStage && sessionId == activeSessionId && ageSeconds <= ttlSeconds; }
        [SerializeField] RadioPulseReplyView view;
        [SerializeField] float delaySeconds=2f;
        [SerializeField] float ttlSeconds=8f;
        WaveDirector wave; GameSession session; int token; int stage; int sessionId; CancellationTokenSource cancellation;
        void OnEnable(){wave=FindAnyObjectByType<WaveDirector>();session=FindAnyObjectByType<GameSession>();sessionId=session==null?0:session.GetInstanceID();cancellation=new CancellationTokenSource();if(wave!=null)wave.OnWaveStageChanged+=OnStage;}
        void OnDisable(){if(wave!=null)wave.OnWaveStageChanged-=OnStage;if(cancellation!=null){cancellation.Cancel();cancellation.Dispose();cancellation=null;}++token;}
        void OnStage(int index,string name,Color color){stage=index;int t=++token;_=Request(t,index,name,color,sessionId,cancellation==null?CancellationToken.None:cancellation.Token);}
        async Task Request(int t,int index,string name,Color color,int expectedSessionId,CancellationToken ct){float start=Time.realtimeSinceStartup;try{await Task.Delay(Mathf.Max(0,(int)(delaySeconds*1000)),ct);if(t!=token)return;var current=FindAnyObjectByType<GameSession>();if(current==null||current.GetInstanceID()!=expectedSessionId)return;string text=await new NpcDialogueService().RequestPulseReplyAsync(name,current.CurrentQuest,current.BuildLiveQuestSnapshot(),$"无线电：{name} 阶段已开始。");if(ct.IsCancellationRequested||!IsPulseCurrent(t,token,index,stage,expectedSessionId,current.GetInstanceID(),Time.realtimeSinceStartup-start,ttlSeconds))return;view?.Show(text,color);}catch(OperationCanceledException){}}
    }
    public sealed class RadioPulseReplyView : MonoBehaviour
    {
        [SerializeField] TMPro.TMP_Text subtitle; [SerializeField] float seconds=3f; float until;
        public void Show(string text,Color color){if(subtitle==null)return;subtitle.text=text;subtitle.color=color;until=Time.unscaledTime+seconds;}
        void Update(){if(subtitle!=null&&Time.unscaledTime>until)subtitle.text=string.Empty;}
    }
}
