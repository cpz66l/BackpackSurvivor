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
        [SerializeField] private Image activeWeaponUI;  //武器激活UI效果

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
            UpdateOverlayLayout(step);
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

        //设置激活武器效果UI
        public void SetActiveWeapon(bool isActive)
        {
            if(activeWeaponUI == null) return;
            activeWeaponUI.gameObject.SetActive(isActive);
        }

        public void UpdateOverlayLayout(float step)
        {
            float connectorSize = Mathf.Clamp(step * 0.16f, 10f, 14f);
            float activeMarkerSize = Mathf.Clamp(step * 0.28f, 18f, 24f);
            float inset = Mathf.Clamp(step * 0.1f, 6f, 8f);

            LayoutImage(topConnector, new Vector2(0.5f, 1), new Vector2(0, -inset), connectorSize);
            LayoutImage(rightConnector, new Vector2(1, 0.5f), new Vector2(-inset, 0), connectorSize);
            LayoutImage(bottomConnector, new Vector2(0.5f, 0), new Vector2(0, inset), connectorSize);
            LayoutImage(leftConnector, new Vector2(0, 0.5f), new Vector2(inset, 0), connectorSize);
            LayoutImage(activeWeaponUI, new Vector2(0, 1), new Vector2(inset, -inset), activeMarkerSize);
        }
        private void LayoutImage(Image image, Vector2 anchor,
            Vector2 position, float size)
        {
            if(image == null) return;
            RectTransform rect = image.rectTransform;
            rect.anchorMax = anchor;
            rect.anchorMin = anchor;
            rect.pivot = new Vector2(0.5f,0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
        }
    }
}
