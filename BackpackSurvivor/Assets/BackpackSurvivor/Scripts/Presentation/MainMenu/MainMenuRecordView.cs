using BS.GamePlay.Save;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace BS.Presentation
{
    public class MainMenuRecordView : MonoBehaviour
    {
        [SerializeField] private TMP_Text totalRunsText;
        [SerializeField] private TMP_Text totalWinsText;
        [SerializeField] private TMP_Text bestBackpackValueText;
        [SerializeField] private TMP_Text totalGoldText;
        [SerializeField] private TMP_Text legendaryFoundCountText;
        [SerializeField] private TMP_Text legendaryCollectedValueText;

        [SerializeField] private GameObject recordViewPanel;

        [SerializeField] private Button closeButton;
        [SerializeField] private Button recordButton;

        [SerializeField] private MainMenuController mainMenuController;

        private void OnEnable()
        { 
                Refresh();
            if (recordViewPanel != null)
                recordViewPanel.SetActive(false);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (recordButton != null)
                recordButton.onClick.AddListener(Open);
            if(mainMenuController == null)
                mainMenuController = FindAnyObjectByType<MainMenuController>();
        }

        private void OnDisable()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (recordButton != null)
                recordButton.onClick.RemoveListener(Open);
        }

        public void Refresh()
        {
            if (SaveService.Instance == null || SaveService.Instance.CurrentData == null)
            {
                SetText(totalRunsText, $"总开局：{0}");
                SetText(totalWinsText, $"胜利次数：{0}");
                SetText(bestBackpackValueText, $"最高背包价值：￥{0}");
                SetText(totalGoldText, $"局外金币：￥{0}");
                SetText(legendaryFoundCountText, $"传说物品带出数：{0}");
                SetText(legendaryCollectedValueText, $"传说物品累计价值：￥{0}");
            }
            else
            {
                SaveData data = SaveService.Instance.CurrentData;
                SetText(totalRunsText, $"总开局：{data.totalRuns}");
                SetText(totalWinsText, $"胜利次数：{data.totalWins}");
                SetText(bestBackpackValueText, $"最高背包价值：￥{data.bestBackpackValue}");
                SetText(totalGoldText, $"局外金币：￥{data.totalGold}");
                SetText(legendaryFoundCountText, $"传说物品带出：{data.legendaryFoundCount}");
                SetText(legendaryCollectedValueText, $"传说物品累计价值：￥{data.legendaryCollectedValue}");
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text == null) return;
            text.text = value;
        }

        private void Close()
        {
            mainMenuController?.PlayButtonClick();
            if (recordViewPanel != null)
                recordViewPanel.SetActive(false);
        }

        private void Open()
        {
            mainMenuController?.PlayButtonClick();
            if (recordViewPanel != null)
                recordViewPanel.SetActive(true);
        } 

    }
}
