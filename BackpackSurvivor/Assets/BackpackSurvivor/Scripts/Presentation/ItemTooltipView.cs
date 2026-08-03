using BS.Inventory;
using TMPro;
using UnityEngine;
namespace BS.Presentation
{
    public class ItemTooltipView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Vector2 offset = new Vector2(18f, -18f);


        private void Awake()
        {
            root.SetActive(false);
        }

        public void Show(Item item, Vector2 position)
        {
            if (root == null || panel == null) return;
            root.SetActive(true);

            titleText.text = $"{item.Id} (Lv.{item.Level})";
            if (item.EffectValue > 0)
            {
                int percent = Mathf.RoundToInt(item.EffectValue * 100f);
                bodyText.text = $"稀有度: {item.Rarity}\n" +
                                $"大小: {item.Width}x{item.Height}\n" +
                                $"价值: ￥{item.ScoreValue}\n" +
                                $"效果: +{percent}%";
            }
            else
            {
                bodyText.text = $"稀有度: {item.Rarity}\n" +
                                $"大小: {item.Width}x{item.Height}\n" +
                                $"价值: ￥{item.ScoreValue}";
            }
            panel.position = position + offset; //让tooltip显示在鼠标位置的右下方
        }

        public void Hide()
        {
            if(root == null) return;    
            root.SetActive(false);
        }

        public void Move(Vector2 screenPosition)
        {
            if (panel == null) return;
            panel.position = screenPosition + offset;
        }
    }
}
