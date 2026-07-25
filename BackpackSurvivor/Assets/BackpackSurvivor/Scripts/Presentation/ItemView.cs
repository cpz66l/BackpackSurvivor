using BS.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



namespace BS.Presentation
{
    public class ItemView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private Image bg;          // 自己身上的 Image
        [SerializeField] private TextMeshProUGUI label;  // 子物体的文字

        private Item item;
        public Item Item => item;

        private InventoryUIController controller;

        public void Bind(Item item ,float step , InventoryUIController ctrl)
        {
            controller = ctrl;
            this.item = item;
            label.text = item.Id;
            GetComponent<RectTransform>().sizeDelta = new Vector2(item.Width * step, item.Height * step);
            switch (item.Rarity)
            {
                case Rarity.Common:
                    bg.color = Color.white;                 // 白
                    break;
                case Rarity.Uncommon:
                    bg.color = Color.green;                 // 绿
                    break;
                case Rarity.Rare:
                    bg.color = Color.blue;                  // 蓝
                    break;
                case Rarity.Epic:
                    bg.color = new Color(0.6f, 0.2f, 0.9f); // 紫
                    break;
                case Rarity.Legendary:
                    bg.color = new Color(1f, 0.84f, 0f);    // 金
                    break;
            }
        }


        public void OnPointerDown(PointerEventData e) => controller.BeginDrag(item, this);
        public void OnDrag(PointerEventData e) => controller.Dragging(e.position);
        public void OnPointerUp(PointerEventData e) => controller.EndDrag();

        public void SetValidColor(bool canPlace)
        {
                bg.color = canPlace
        ? new Color(0.1f, 0.55f, 0.15f, 0.8f)   // 暗绿
        : new Color(0.6f, 0.12f, 0.12f, 0.8f);  // 暗红
        }
    }
}
