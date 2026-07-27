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
        [SerializeField] private Image topConnector;
        [SerializeField] private Image rightConnector;
        [SerializeField] private Image bottomConnector;
        [SerializeField] private Image leftConnector;

        private Item item;
        public Item Item => item;

        private InventoryUIController controller;

        public void Bind(Item item ,float step , InventoryUIController ctrl)
        {
            controller = ctrl;
            this.item = item;
            label.text = $"{item.Id} Lv.{item.Level}";
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
        public void OnPointerUp(PointerEventData e) => controller.EndDrag(e.position);

        public void SetValidColor(bool rightful)
        {
                bg.color = rightful
        ? new Color(0.1f, 0.55f, 0.15f, 0.8f)   // 暗绿
        : new Color(0.6f, 0.12f, 0.12f, 0.8f);  // 暗红
        }

        public void SetConnectors(ConnectableSides visibleSides, ConnectableSides  activeSides)
        {
            SetConnector(topConnector,ConnectableSides.Up, visibleSides, activeSides);//top
            SetConnector(rightConnector,ConnectableSides.Right, visibleSides, activeSides);//right
            SetConnector(bottomConnector,ConnectableSides.Down, visibleSides, activeSides);//bottom
            SetConnector(leftConnector,ConnectableSides.Left, visibleSides, activeSides);//left
        }

        private void SetConnector(Image connector, 
            ConnectableSides side,
            ConnectableSides visibleSides,
            ConnectableSides activeSides)
        {
            if ((visibleSides & side) == 0)
            {
                connector.gameObject.SetActive(false);
                return;//若没有对应方向的接口直接禁掉
            }
            else connector.gameObject.SetActive(true);

            if ((activeSides & side) != 0) //如果链接成功显示金色
                connector.color = new Color(1f, 0.78f, 0.15f, 1f);
            else
                connector.color = new Color(0.55f, 0.55f, 0.55f, 0.9f);//灰色
        }
    }
}
