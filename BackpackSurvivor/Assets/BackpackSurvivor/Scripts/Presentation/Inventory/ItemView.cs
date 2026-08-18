using BS.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



namespace BS.Presentation
{
    public class ItemView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler,IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] private Image bg;          // 自己身上的 Image
        [SerializeField] private TextMeshProUGUI label;  // 子物体的文字
        [SerializeField] private Image topConnector;
        [SerializeField] private Image rightConnector;
        [SerializeField] private Image bottomConnector;
        [SerializeField] private Image leftConnector;
        [SerializeField] private Image activeWeaponUI;  //武器激活UI效果
        [SerializeField] private Image iconImage; // 显示物品图标的Image组件
        [SerializeField] private Image LevelOne;
        [SerializeField] private Image LevelTwo;
        [SerializeField] private Image LevelThree;

        private Sprite iconSprite; // 存储物品图标的Sprite
        private Item item;
        public Item Item => item;

        private InventoryUIController controller;

        //将UI显示层与Item绑定起来
        public void Bind(Item item ,float step , InventoryUIController ctrl , Sprite sprite)
        {
            controller = ctrl;
            this.item = item;
            iconSprite = sprite;
            label.text = "";
            GetComponent<RectTransform>().sizeDelta = new Vector2(item.Width * step, item.Height * step);

            switch (item.Rarity)
            {
                case Rarity.Common:
                    bg.color = new Color(1f,1f,1f,0.8f);                 // 白
                    break;
                case Rarity.Uncommon:
                    bg.color = new Color(0f,1f,0f,0.8f);                // 绿
                    break;
                case Rarity.Rare:
                    bg.color = new Color(0f,0f,1f,0.8f);                  // 蓝
                    break;
                case Rarity.Epic:
                    bg.color = new Color(0.6f, 0.2f, 0.9f,0.8f); // 紫
                    break;
                case Rarity.Legendary:
                    bg.color = new Color(1f, 0f, 0f,0.8f);       // 红
                    break;
            }
            //更新联接口和武器激活UI布局
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
                connector.color = new Color(1f, 0.78f, 0.15f, 8f);
            else
                connector.color = new Color(0.55f, 0.55f, 0.55f, 0.7f);//灰色
        }

        //设置激活武器效果UI
        public void SetActiveWeapon(bool isActive)
        {
            if(activeWeaponUI == null) return;
            activeWeaponUI.gameObject.SetActive(isActive);
        }


        public void UpdateOverlayLayout(float step)
        {
            //计算物品UI尺寸，选出短边
            float itemPixelWidth = item.Width * step;
            float itemPixelHeight = item.Height * step;

            float inset = Mathf.Clamp(step * 0.1f, 6f, 8f);
            float activeMarkerSize = Mathf.Clamp(step * 0.28f, 18f, 24f);
            //星星图标
            float starSize = Mathf.Clamp(step * 0.20f, 12f, 18f);
            float starGap = Mathf.Clamp(step * 0.04f, 2f, 4f);
            //邻接边设置
            float edgeThickness = Mathf.Clamp(step * 0.08f, 4f, 7f);
            float edgeInset = Mathf.Clamp(step * 0.05f, 3f, 5f);
            Vector2 upAndDown = new Vector2(itemPixelWidth - inset * 2, edgeThickness);
            Vector2 leftAndRight = new Vector2(edgeThickness, itemPixelHeight - inset * 2);

            LayoutEdge(topConnector, new Vector2(0.5f, 1), new Vector2(0, -edgeInset), upAndDown);
            LayoutEdge(rightConnector, new Vector2(1, 0.5f), new Vector2(-edgeInset, 0), leftAndRight);
            LayoutEdge(bottomConnector, new Vector2(0.5f, 0), new Vector2(0, edgeInset), upAndDown);
            LayoutEdge(leftConnector, new Vector2(0, 0.5f), new Vector2(edgeInset, 0), leftAndRight);

            LayoutImage(activeWeaponUI, new Vector2(0, 1), new Vector2(inset, -inset), activeMarkerSize);

            LayoutImage(LevelOne, new Vector2(1, 1), new Vector2(-inset, -inset-3f), starSize);
            LayoutImage(LevelTwo, new Vector2(1, 1), new Vector2(-inset - 1 * (starSize + starGap), -inset-3f), starSize);
            LayoutImage(LevelThree, new Vector2(1, 1), new Vector2(-inset - 2 * (starSize + starGap), -inset-3f), starSize);

            SetLevelStars(item.Level);
            
            LayoutIcon(step, inset, starSize);
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

        private void LayoutEdge(Image image, Vector2 anchor, Vector2 position, Vector2 size)
        {
            if (image == null) return;
            RectTransform rect = image.rectTransform;
            rect.anchorMax = anchor;
            rect.anchorMin = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void SetLevelStars(int level)
        {
            switch (level)
            {
                case 1:
                    LevelOne.enabled = true;
                    LevelTwo.enabled = false;
                    LevelThree.enabled = false;
                    break;
                case 2:
                    LevelThree.enabled = false;
                    LevelTwo.enabled = true;
                    LevelOne.enabled = true;
                    break;
                case 3:
                    LevelThree.enabled = true;
                    LevelTwo.enabled = true;
                    LevelOne.enabled = true;
                    break;
            }
        }

        private void LayoutIcon(float step, float inset, float starSize)
        {
            //计算贴图图标大小
            float reservedTop = starSize + inset;
            float itemPixelWidth = item.Width * step;
            float itemPixelHeight = item.Height * step;
            float availableW = itemPixelWidth - inset * 2f;
            float availableH = itemPixelHeight - inset * 2f;
            float iconWidth = availableW * 0.85f;
            float iconHeight = availableH * 0.85f;
            Vector2 size = new Vector2(iconWidth, iconHeight);

            if (iconImage != null && iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                Vector2 iconPosition = new Vector2(0f, -reservedTop * 0.25f);
                LayoutEdge(iconImage, new Vector2(0.5f, 0.5f), iconPosition, size);
                iconImage.enabled = true;
            }
            else if (iconImage != null)
            {
                iconImage.enabled = false;
                label.text = $"{item.Id}";
            }
        }

        //当鼠标进入时显示提示框，离开时隐藏提示框
        public void OnPointerEnter(PointerEventData eventData)
        {
            controller.ShowTooltip(item, eventData.position);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            controller.MoveTooltip(eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            controller.HideTooltip();
        }
    }
}
