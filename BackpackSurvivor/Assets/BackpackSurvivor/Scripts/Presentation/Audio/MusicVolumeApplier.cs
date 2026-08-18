using BS.Core;
using UnityEngine;

namespace BS.Presentation
{
    public class MusicVolumeApplier : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;

        private float baseVolume = 1f;

        private void Awake()
        {
            if (musicSource == null)
                musicSource = GetComponent<AudioSource>();

            if (musicSource != null)
                baseVolume = musicSource.volume;

            ApplySettings(SettingsService.Load());
        }

        private void OnEnable()
        {
            SettingsService.Applied += ApplySettings;
        }

        private void OnDisable()
        {
            SettingsService.Applied -= ApplySettings;
        }

        private void ApplySettings(GameSettings settings)
        {
            if (musicSource == null || settings == null) return;

            musicSource.volume =
                baseVolume * SettingsService.GetEffectiveMusicVolume(settings);
        }
    }
}