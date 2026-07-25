using BS.GamePlay.Interaction;
using TMPro;
using UnityEngine;

namespace BS.Presentation {
    public class InteractPromptUI : MonoBehaviour
    {
        //字段
        [SerializeField] private GameObject promptPanel;// 整个提示框，管显隐
        [SerializeField] private TMP_Text promptText;// 框里的文本（用 TMPro 命名空间）
        [SerializeField] private InteractDetector detector; // 拖 Player 身上那个组件


        private void Start()
        {
            promptPanel.SetActive(false);
        }

        private void HandleTargetChanged(IInteractable target)
        {
            if(target == null) promptPanel.SetActive(false);
            else
            {
                promptText.text = target.GetPrompt();
                promptPanel.SetActive(true);
            }
        }
        private void OnEnable()
        {
            detector.OnTargetChanged += HandleTargetChanged;
        }
        private void OnDisable()
        {
            detector.OnTargetChanged -= HandleTargetChanged;
        }
    }
}
