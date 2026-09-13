using System;
using System.Threading;
using System.Threading.Tasks;
using BS.Core.LLM;
using BS.GamePlay.Run;
using BS.GamePlay.Waves;
using BS.Quest;
using UnityEngine;
namespace BS.GamePlay.Npc
{
    public sealed class WavePulseService : MonoBehaviour
    {
        [SerializeField] RadioPulseReplyView view;
        [SerializeField] float delaySeconds=2f;
        [SerializeField] float ttlSeconds=8f;
        WaveDirector wave;
        GameSession session;
        CancellationTokenSource cancellation;
        int token,stage;
        public string LastStatus { get; private set; }
        public string LastAudit { get; private set; }
        public int DisplayedCount { get; private set; }
        public int StageIndex => stage;
        // Isolated Play audits can inject latency without changing player configuration.
        public INpcDialogue DialogueOverride { get; set; }
        public static bool IsPulseCurrent(int requestToken,int activeToken,int stageIndex,int activeStage,int sessionId,int activeSessionId,float ageSeconds,float ttlSeconds)
            => requestToken==activeToken && stageIndex==activeStage && sessionId==activeSessionId && ageSeconds<=ttlSeconds;
        void OnEnable()
        {
            wave=FindAnyObjectByType<WaveDirector>();session=FindAnyObjectByType<GameSession>();
            if(wave)wave.OnWaveStageChanged+=OnStage;
            if(session)session.OnStateChanged+=OnState;
        }
        void OnDisable()
        {
            if(wave)wave.OnWaveStageChanged-=OnStage;
            if(session)session.OnStateChanged-=OnState;
            CancelCurrent();
        }
        void OnState(GameState state)
        {
            if(state==GameState.Victory || state==GameState.Defeat || state==GameState.NotStarted)CancelCurrent();
        }
        void CancelCurrent()
        {
            ++token;cancellation?.Cancel();cancellation?.Dispose();cancellation=null;
            if(view)view.Clear();
        }
        void OnStage(int index,string name,Color color)
        {
            CancelCurrent();stage=index;LastAudit="";
            if(!LlmConfigService.LoadFile().npcEnabled){LastStatus="disabled";return;}
            if(!session || session.CurrentQuest==null){LastStatus="no_contract";return;}
            cancellation=new CancellationTokenSource();
            LastStatus="waiting";
            _=Request(token,index,name,color,session.GetInstanceID(),session.CurrentQuest,cancellation.Token);
        }
        bool IsCurrent(int t,int index,int id,QuestInstance quest,float started)
        {
            return this && isActiveAndEnabled && session && session.State==GameState.Running &&
                ReferenceEquals(quest,session.CurrentQuest) &&
                IsPulseCurrent(t,token,index,stage,id,session.GetInstanceID(),Time.realtimeSinceStartup-started,ttlSeconds);
        }
        async Task Request(int t,int index,string name,Color color,int id,QuestInstance quest,CancellationToken ct)
        {
            float started=Time.realtimeSinceStartup;
            try
            {
                await Task.Delay(Mathf.Max(0,(int)(delaySeconds*1000)),ct);
                if(!IsCurrent(t,index,id,quest,started)){if(t==token)LastStatus="expired_or_inactive";return;}
                LastStatus="requesting";
                var live=DialogueOverride==null?new NpcDialogueService():null;
                var dialogue=DialogueOverride??live;
                string reply=await dialogue.RequestPulseReplyAsync(name,quest,session.BuildLiveQuestSnapshot(),"",ct);
                ct.ThrowIfCancellationRequested();
                if(!IsCurrent(t,index,id,quest,started)){if(t==token)LastStatus="expired_or_inactive";return;}
                if(!LlmConfigService.LoadFile().npcEnabled){LastStatus="disabled";return;}
                LastAudit=live?.Audit??"injected transport";
                if(string.IsNullOrWhiteSpace(reply)){LastStatus="empty_or_rejected:"+live?.LastFailure;return;}
                if(!view){LastStatus="missing_view";return;}
                view.Show(reply,color);DisplayedCount++;LastStatus="shown";
            }
            catch(OperationCanceledException){if(this && t==token)LastStatus="cancelled";}
            catch(Exception e){if(this && t==token)LastStatus="failed:"+e.GetType().Name;}
        }
    }
}
