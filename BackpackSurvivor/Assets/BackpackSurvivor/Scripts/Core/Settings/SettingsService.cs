using System;
using UnityEngine;

namespace BS.Core
{
    public static class SettingsService 
    {
        public static event Action<GameSettings> Applied;

        private const string MasterVolumeKey = "Settings.MasterVolume";
        private const string SfxVolumeKey = "Settings.SfxVolume";
        private const string MusicVolumeKey = "Settings.MusicVolume";
        private const string ResolutionWidthKey = "Settings.ResolutionWidth";
        private const string ResolutionHeightKey = "Settings.ResolutionHeight";
        private const string FullscreenModeKey = "Settings.FullscreenMode";

        public static GameSettings Load()//读取配置到新建的settings
        {
            GameSettings settings = GameSettings.CreateDefault();
            settings.masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, settings.masterVolume);
            settings.sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, settings.sfxVolume);
            settings.musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, settings.musicVolume);

            settings.resolutionHeight = PlayerPrefs.GetInt(ResolutionHeightKey, settings.resolutionHeight);
            settings.resolutionWidth = PlayerPrefs.GetInt(ResolutionWidthKey, settings.resolutionWidth);

            settings.fullscreenMode = (FullScreenMode)PlayerPrefs.GetInt(FullscreenModeKey,(int)settings.fullscreenMode);
            //fullscreenMode 用 int 保存，读出来后强转
            return settings;
        }

        public static void Save(GameSettings settings)  //写入数据
        {
            if (settings == null) return;
            settings.masterVolume = Mathf.Clamp01(settings.masterVolume);
            settings.sfxVolume = Mathf.Clamp01(settings.sfxVolume);
            settings.musicVolume = Mathf.Clamp01(settings.musicVolume);

            PlayerPrefs.SetFloat(MasterVolumeKey, settings.masterVolume );
            PlayerPrefs.SetFloat(SfxVolumeKey, settings.sfxVolume);
            PlayerPrefs.SetFloat(MusicVolumeKey, settings.musicVolume);

            PlayerPrefs.SetInt(ResolutionWidthKey, settings.resolutionWidth);
            PlayerPrefs.SetInt(ResolutionHeightKey, settings.resolutionHeight);

            PlayerPrefs.SetInt(FullscreenModeKey, (int)settings.fullscreenMode);

            PlayerPrefs.Save();
        }

        public static void Apply(GameSettings settings)
        {
            if (settings == null) return;
            Screen.SetResolution(settings.resolutionWidth,
                settings.resolutionHeight,
                settings.fullscreenMode);

            Applied?.Invoke(settings);
        }

        public static GameSettings ResetToDefault()
        {
            GameSettings settings = GameSettings.CreateDefault();
            Save(settings);
            Apply(settings);
            return settings;
        }

        //计算音量
        public static float GetEffectiveSfxVolume(GameSettings settings)
        {
            return Mathf.Clamp01(settings.masterVolume) * Mathf.Clamp01(settings.sfxVolume);
        }

        public static float GetEffectiveMusicVolume(GameSettings settings)
        {
            return Mathf.Clamp01(settings.masterVolume) * Mathf.Clamp01(settings.musicVolume);
        }
    }
}
