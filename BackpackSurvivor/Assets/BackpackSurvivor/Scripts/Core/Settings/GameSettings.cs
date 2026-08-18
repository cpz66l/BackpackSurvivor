using UnityEngine;
namespace BS.Core
{
    public class GameSettings 
    {
        public float masterVolume = 1f; //总音量
        public float sfxVolume = 1f;    //音效音量
        public float musicVolume = 1f;  //BGM音量

        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;

        public FullScreenMode fullscreenMode = FullScreenMode.FullScreenWindow;

        public static GameSettings CreateDefault()
        {
            GameSettings defaultSetting = new GameSettings();
            defaultSetting.masterVolume = 1f;
            defaultSetting.sfxVolume = 1f;
            defaultSetting.musicVolume = 1f;

            defaultSetting.resolutionWidth = 1920;
            defaultSetting.resolutionHeight = 1080;

            defaultSetting.fullscreenMode = FullScreenMode.FullScreenWindow;
            return defaultSetting;  
        }
    }
}
