using System;
using System.Threading.Tasks;
using BS.GamePlay.Waves;
using UnityEngine;
using BS.GamePlay.Run;
using BS.Quest;

namespace BS.GamePlay.Npc
{
    public sealed class WavePulseService : MonoBehaviour
    {
        [SerializeField] RadioPulseReplyView view;
        [SerializeField] float delaySeconds=2f;
        [SerializeField] float ttlSeconds=8f;
        WaveDirector wave; int token; int stage;
        void OnEnable(){wave=FindAnyObjectByType<WaveDirector>();if(wave!=null)wave.OnWaveStageChanged+=OnStage;}
        void OnDisable(){if(wave!=null)wave.OnWaveStageChanged-=OnStage;}
        void OnStage(int index,string name,Color color){stage=index;int t=++token;_=Request(t,index,name,color);}
        async Task Request(int t,int index,string name,Color color){float start=Time.realtimeSinceStartup;await Task.Delay(Mathf.Max(0,(int)(delaySeconds*1000)));if(t!=token)return;var session=FindAnyObjectByType<GameSession>();string text=await new NpcDialogueService().RequestPulseReplyAsync(name,session==null?null:session.CurrentQuest,session==null?null:session.BuildLiveQuestSnapshot(),$"无线电：{name} 阶段已开始。");if(Time.realtimeSinceStartup-start>ttlSeconds||index!=stage||t!=token)return;view?.Show(text,color);}
    }
    public sealed class RadioPulseReplyView : MonoBehaviour
    {
        [SerializeField] TMPro.TMP_Text subtitle; [SerializeField] float seconds=3f; float until;
        public void Show(string text,Color color){if(subtitle==null)return;subtitle.text=text;subtitle.color=color;until=Time.unscaledTime+seconds;}
        void Update(){if(subtitle!=null&&Time.unscaledTime>until)subtitle.text=string.Empty;}
    }
}
