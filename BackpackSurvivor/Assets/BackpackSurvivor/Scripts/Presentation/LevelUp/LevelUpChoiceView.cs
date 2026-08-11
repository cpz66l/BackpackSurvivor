using BS.GamePlay.Combat;
using BS.GamePlay.Player;
using BS.GamePlay.Run;
using BS.GamePlay.Upgrades;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BS.Presentation
{
    public class LevelUpChoiceView : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;

        [SerializeField] private GameObject root;
        [SerializeField] private Button choiceOne;
        [SerializeField] private Button choiceTwo;
        [SerializeField] private Button choiceThree;

        [SerializeField] private TextMeshProUGUI choiceOneTitle;
        [SerializeField] private TextMeshProUGUI choiceTwoTitle;
        [SerializeField] private TextMeshProUGUI choiceThreeTitle;
        [SerializeField] private TextMeshProUGUI choiceOneDescription;
        [SerializeField] private TextMeshProUGUI choiceTwoDescription;
        [SerializeField] private TextMeshProUGUI choiceThreeDescription;
        private List<LevelUpOption> currentOptions;

        private void Awake()
        {
            if (gameSession == null)
                gameSession = FindAnyObjectByType<GameSession>();
            Close();
        }

        private void OnEnable()
        {
            if (gameSession != null)
                gameSession.OnLevelUpChoiceRequested += HandleLevelUpChoiceRequested;
            choiceOne.onClick.AddListener(SelectChoiceOne);
            choiceTwo.onClick.AddListener(SelectChoiceTwo);
            choiceThree.onClick.AddListener(SelectChoiceThree);
        }

        private void OnDisable()
        {
            if (gameSession != null)
                gameSession.OnLevelUpChoiceRequested -= HandleLevelUpChoiceRequested;
            choiceOne.onClick.RemoveListener(SelectChoiceOne);
            choiceTwo.onClick.RemoveListener(SelectChoiceTwo);
            choiceThree.onClick.RemoveListener(SelectChoiceThree);
        }

        private void HandleLevelUpChoiceRequested(List<LevelUpOption> options)
        {
            currentOptions = options;
            choiceOneTitle.text = options[0].Title;
            choiceTwoTitle.text = options[1].Title;
            choiceThreeTitle.text = options[2].Title;
            choiceOneDescription.text = options[0].Description;
            choiceTwoDescription.text = options[1].Description;
            choiceThreeDescription.text = options[2].Description;
            Open();
        }
        private void SelectChoiceOne()
        {
            if (currentOptions == null) return;
            LevelUpOption option = currentOptions[0];
            Close();
            gameSession.ChooseLevelUpOption(option);
        }
        private void SelectChoiceTwo()
        {
            if (currentOptions == null) return;
            LevelUpOption option = currentOptions[1];
            Close();
            gameSession.ChooseLevelUpOption(option);
        }
        private void SelectChoiceThree()
        {
            if (currentOptions == null) return;
            LevelUpOption option = currentOptions[2];
            Close();
            gameSession.ChooseLevelUpOption(option);
        }
        private void Open()
        {
            if (root != null)
                root.SetActive(true);
        }
        private void Close()
        {
            if (root != null)
                root.SetActive(false);
        }
    }
}
