using BS.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace BS.Presentation
{
    public class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;

        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown windowModeDropdown;

        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private MainMenuController mainMenuController;

        private GameSettings currentSettings;
        private readonly List<Vector2Int> resolutionOptions = new();//在BuildResolutionOptions()中初始化了

        private void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;
            if (mainMenuController == null)
                mainMenuController = FindAnyObjectByType<MainMenuController>();

            BuildResolutionOptions();
            BuildWindowModeOptions();

            panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (applyButton != null)
                applyButton.onClick.AddListener(Apply);
            if (resetButton != null)
                resetButton.onClick.AddListener(Reset);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeSliderChange);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeSliderChange);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChange);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (windowModeDropdown != null)
                windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);
        }

        private void OnDisable()
        {
            if (applyButton != null)
                applyButton.onClick.RemoveListener(Apply);
            if (resetButton != null)
                resetButton.onClick.RemoveListener(Reset);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeSliderChange);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeSliderChange);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChange);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            if (windowModeDropdown != null)
                windowModeDropdown.onValueChanged.RemoveListener(OnWindowModeChanged);
        }

        public void Open()
        {
            currentSettings = SettingsService.Load();
            RefreshView(currentSettings);
            panelRoot.SetActive(true);
        }

        private void Apply()
        {
            if (currentSettings == null) return;
            mainMenuController.PlayButtonClick();
            SettingsService.Save(currentSettings);
            SettingsService.Apply(currentSettings);
        }

        private void Reset()
        {
            mainMenuController.PlayButtonClick();
            currentSettings = SettingsService.ResetToDefault();
            RefreshView(currentSettings);
        }

        private void Close()
        {
            mainMenuController.PlayButtonClick();
            currentSettings = null;
            panelRoot.SetActive(false);
        }

        //每次打开根据currentSetting刷新UI状态
        private void RefreshView(GameSettings settings)
        {
            if (settings == null) return;

            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);

            if (musicVolumeSlider != null)
                musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);

            if (resolutionDropdown != null)
            {
                int resolutionIndex = FindResolutionIndex(
                    settings.resolutionWidth,
                    settings.resolutionHeight
                );

                resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
            }

            if (windowModeDropdown != null)
            {
                int windowModeIndex = GetWindowModeIndex(settings.fullscreenMode);
                windowModeDropdown.SetValueWithoutNotify(windowModeIndex);
            }
        }
        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < resolutionOptions.Count; i++)
            {
                Vector2Int option = resolutionOptions[i];

                if (option.x == width && option.y == height)
                    return i;
            }
            return 0;
        }
        private int GetWindowModeIndex(FullScreenMode mode)
        {
            if (mode == FullScreenMode.FullScreenWindow)
                return 1;
            return 0;
        }


        private void BuildResolutionOptions()
        {
            resolutionOptions.Clear();
            resolutionOptions.Add(new Vector2Int(1280, 720));
            resolutionOptions.Add(new Vector2Int(1600, 900));
            resolutionOptions.Add(new Vector2Int(1920, 1080));
            AddResolutionOption(
                Screen.currentResolution.width,
                Screen.currentResolution.height);//支持原生分辨率

            //按照宽度大小进行排序
            resolutionOptions.Sort((b, a) =>
            {
                int widthCompare = a.x.CompareTo(b.x);
                if (widthCompare != 0) return widthCompare;

                return a.y.CompareTo(b.y);
            });

            if (resolutionDropdown == null) return;

            resolutionDropdown.ClearOptions();

            List<string> labels = new List<string>();
            foreach (Vector2Int option in resolutionOptions)
            {
                labels.Add($"{option.x} x {option.y}");
            }

            resolutionDropdown.AddOptions(labels);
        }
        //添加原生分辨率
        private void AddResolutionOption(int width, int height)
        {
            if (width <= 0 || height <= 0) return;

            Vector2Int option = new Vector2Int(width, height);

            if (resolutionOptions.Contains(option)) return;

            resolutionOptions.Add(option);
        }

        private void BuildWindowModeOptions()
        {
            if (windowModeDropdown == null) return;

            windowModeDropdown.ClearOptions();

            windowModeDropdown.AddOptions(new List<string>{"窗口模式","无边框全屏"});
        }


        //监听玩家操作,写入当前设置
        private void OnMasterVolumeSliderChange(float value)
        {
            if(currentSettings == null) return;
            currentSettings.masterVolume = value;
        }
        private void OnSfxVolumeSliderChange(float value)
        {
            if (currentSettings == null) return;
            currentSettings.sfxVolume = value;
        }
        private void OnMusicVolumeSliderChange(float value)
        {
            if (currentSettings == null) return;
            currentSettings.musicVolume = value;
        }
        private void OnResolutionChanged(int index)
        {
            if (currentSettings == null) return;
            if (index < 0 || index >= resolutionOptions.Count) return;

            Vector2Int selected = resolutionOptions[index];

            currentSettings.resolutionWidth = selected.x;
            currentSettings.resolutionHeight = selected.y;
        }
        private void OnWindowModeChanged(int index)
        {
            if (currentSettings == null) return;

            currentSettings.fullscreenMode = index == 1
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
        }
    }
}
