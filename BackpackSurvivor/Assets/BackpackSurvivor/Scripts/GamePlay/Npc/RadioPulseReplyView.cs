using TMPro;
using UnityEngine;
namespace BS.GamePlay.Npc
{
    public sealed class RadioPulseReplyView : MonoBehaviour
    {
        [SerializeField] TMP_Text subtitle;
        [SerializeField] CanvasGroup group;
        [SerializeField] float seconds=5f;
        float until;
        public string DisplayText=>subtitle?subtitle.text:"";
        void Awake(){Clear();}
        public void Show(string text,Color color)
        {
            if(!subtitle || string.IsNullOrWhiteSpace(text)){Clear();return;}
            subtitle.text="小芯 · "+text;subtitle.color=color;
            if(group)group.alpha=1;
            until=Time.unscaledTime+seconds;
        }
        public void Clear(){if(subtitle)subtitle.text="";if(group)group.alpha=0;}
        void Update(){if(Time.unscaledTime>=until)Clear();}
        void OnDisable(){Clear();}
    }
}
