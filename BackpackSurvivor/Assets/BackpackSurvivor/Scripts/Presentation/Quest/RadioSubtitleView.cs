using BS.GamePlay.Waves;
using TMPro;
using UnityEngine;

namespace BS.Presentation
{
    public sealed class RadioSubtitleView : MonoBehaviour
    {
        [SerializeField] TMP_Text subtitle;
        [SerializeField] float visibleSeconds=3f;
        float until;
        void OnEnable(){var wave=FindAnyObjectByType<WaveDirector>();if(wave!=null)wave.OnWaveStageChanged+=OnStage;}
        void OnDisable(){var wave=FindAnyObjectByType<WaveDirector>();if(wave!=null)wave.OnWaveStageChanged-=OnStage;}
        void Update(){if(subtitle!=null&&Time.unscaledTime>until)subtitle.text=string.Empty;}
        void OnStage(int index,string stage,Color color){if(subtitle==null)return;subtitle.color=color;subtitle.text=$"无线电：阶段 {index+1} · {stage}";until=Time.unscaledTime+visibleSeconds;}
    }
}
