using UnityEngine;

namespace BS.Presentation
{
    public class SfxPlayer : MonoBehaviour
    {
        [System.Serializable]
        private class WeaponAudioCue
        {
            public WeaponSfxId id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 0.6f;
            public float pitchMin = 0.96f;
            public float pitchMax = 1.04f;
            public float cooldown = 0.05f;

            [System.NonSerialized] public float lastPlayTime = -999f;
        }
        [SerializeField] private WeaponAudioCue[] weaponCues;

        [System.Serializable]
        private class AudioCue
        {
            public SfxId id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 0.8f;
            public float pitchMin = 1f;
            public float pitchMax = 1f;
            public float cooldown = 0f;

            [System.NonSerialized] public float lastPlayTime = -999f;
        }
        [SerializeField] private AudioCue[] cues;


        [SerializeField] private AudioSource audioSource;


        private void Awake()
        {
            if (audioSource == null) 
                audioSource = GetComponent<AudioSource>();
        }

        //武器音效
        public void PlayWeaponShoot(WeaponSfxId id)
        {
            WeaponAudioCue cue = FindWeaponCue(id);
            if (cue == null) return;

            PlayWeaponCue(cue);
        }

        //普通音效
        public void PlaySfx(SfxId id)
        {
            AudioCue cue = FindCue(id);
            if (cue == null) return;

            PlayCue(cue);
        }

        //找到相同id的武器音频提示
        private WeaponAudioCue FindWeaponCue(WeaponSfxId id)
        {
            if(id == WeaponSfxId.None) return null;
            if(weaponCues == null) return null;
            foreach (var weaponCue in weaponCues)
            {
                if(weaponCue.id != id) continue;
                return weaponCue;
            }
            return null;
        }
        //武器音频播放方法
        private void PlayWeaponCue(WeaponAudioCue cue)
        {
            if (cue == null) return;
            if(audioSource == null ||cue.clips == null ||cue.clips.Length == 0) return;
            if(Time.unscaledTime - cue.lastPlayTime < cue.cooldown) return; //若当前时间距离上次音效播放时间还没结束冷却，则不进行音效播放
            //抽取音效
            int size = cue.clips.Length;
            AudioClip clip = cue.clips[UnityEngine.Random.Range(0,size)];
            if (clip == null) return;
            //调整播放速度
            float oldPitch = audioSource.pitch; //记录原来的播放速度
            audioSource.pitch = UnityEngine.Random.Range(cue.pitchMin, cue.pitchMax); //改变播放速度
            //播放声音
            audioSource.PlayOneShot(clip, cue.volume);
            //还原播放速度
            audioSource.pitch = oldPitch;
            cue.lastPlayTime = Time.unscaledTime;   //记录本次音效播放时间
            //这里用 Time.unscaledTime，不是 Time.time。因为暂停 Time.timeScale = 0，不会影响Time.unscaledTime计时。
            //音频系统最好不要被游戏暂停时间影响。射击时无所谓，但音频层用 unscaled 更稳。
        }


        //找到相同id的音频提示
        private AudioCue FindCue(SfxId id)
        {
            if (id == SfxId.None) return null;
            if (cues == null) return null;

            foreach (AudioCue cue in cues)
            {
                if (cue == null) continue;
                if (cue.id != id) continue;

                return cue;
            }

            return null;
        }
        //音频播放方法
        private void PlayCue(AudioCue cue)
        {
            if (cue == null) return;
            if (audioSource == null) return;
            if (cue.clips == null || cue.clips.Length == 0) return;
            if (Time.unscaledTime - cue.lastPlayTime < cue.cooldown) return;

            AudioClip clip = cue.clips[UnityEngine.Random.Range(0, cue.clips.Length)];
            if (clip == null) return;

            float oldPitch = audioSource.pitch;
            audioSource.pitch = UnityEngine.Random.Range(cue.pitchMin, cue.pitchMax);
            audioSource.PlayOneShot(clip, cue.volume);
            audioSource.pitch = oldPitch;

            cue.lastPlayTime = Time.unscaledTime;
        }
    }
}
