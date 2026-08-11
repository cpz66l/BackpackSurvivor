using BS.GamePlay.Interaction;
using System.Collections;
using TMPro;
using UnityEngine;

namespace BS.Presentation {
    public class InteractPromptUI : MonoBehaviour
    {
        //字段
        [SerializeField] private GameObject promptPanel;// 整个提示框，管显隐
        [SerializeField] private TMP_Text promptText;// 框里的文本（用 TMPro 命名空间）
        [SerializeField] private TMP_Text promptBagFull; //背包已满闪字
        [SerializeField] private InteractDetector detector; // 拖 Player 身上那个组件


        private void Start()
        {
            promptPanel.SetActive(false);
            promptBagFull.gameObject.SetActive(false);
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

        private void BagIsFull()
        {
            StartCoroutine(BackpackFullReminder());
        }

        private void OnEnable()
        {
            detector.OnTargetChanged += HandleTargetChanged;
            detector.OnInteractFailed += BagIsFull;
        }
        private void OnDisable()
        {
            detector.OnTargetChanged -= HandleTargetChanged;
            detector.OnInteractFailed -= BagIsFull;
        }

        private IEnumerator BackpackFullReminder()
        {
            promptBagFull.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            promptBagFull.gameObject.SetActive(false);
        }
    }
}
