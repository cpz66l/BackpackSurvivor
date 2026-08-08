using BS.GamePlay;
using BS.GamePlay.Player;
using BS.Inventory;
using UnityEngine;
using System.Collections.Generic;
using BS.GamePlay.Combat;
using TMPro;
namespace BS.Presentation
{
    public class InventoryUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform itemLayer;    // 拖 ItemLayer
        [SerializeField] private ItemView itemViewPrefab;    // 拖预制体
        [SerializeField] private float step = 70f;           // 64格 + 2缝
        [SerializeField] private InventorySystem inventorySystem;
        [SerializeField] private RectTransform bagPanel;   // Inspector 拖背包底框
        [SerializeField] private TextMeshProUGUI totalValueText; //总价值文本
        [SerializeField] private ItemTooltipView tooltipView; //item属性提示面板
        [SerializeField] private ItemIconResolver itemIconResolver; //物品图标解析器
        [SerializeField] private CanvasGroup bagPanelcanvasGroup;

        private InventoryGrid grid;
        private bool isDragging = false;
        private InputReader inputReader;
        private BackpackWeaponActivator backpackWeaponActivator;

        //拖拽相关
        private Item dragItem;
        private ItemView ghost;// 被拖的那个视图
        private int oldX, oldY;// 旧锚点（回滚用）
        private int targetX, targetY;// 当前鼠标悬停的目标格子
        private bool needsRedrawAfterDrag; //拖拽期间漏掉重绘的补偿机制

        private bool isBagOpen = false;

        private void Awake()
        {
            inputReader = FindAnyObjectByType<InputReader>();
            backpackWeaponActivator = FindAnyObjectByType<BackpackWeaponActivator>();
            if (itemIconResolver == null) 
                itemIconResolver = FindAnyObjectByType<ItemIconResolver>();
        }
        private void Start ()
        {
            grid = inventorySystem.Grid;
            grid.OnChanged += Redraw;
            needsRedrawAfterDrag = false;
            Redraw();
            isBagOpen = true;
            HandleOpenBag();
        }

        //订阅输入事件
        private void OnEnable()
        {
            inputReader.OnRotate += HandleRotate;
            inputReader.OnOpenBag += HandleOpenBag;
        }
        private void OnDisable()
        {
            inputReader.OnRotate -= HandleRotate;
            inputReader.OnOpenBag -= HandleOpenBag;
        }


        private void Redraw ()
        {
            if (isDragging)
            {
                needsRedrawAfterDrag = true;
                return;// 拖拽期间，数据变化攒着不画
            }
            needsRedrawAfterDrag = false;
            //邻接扫描
            List<AdjacencyEffect> candidateEffects = grid.ScanAdjacency(AdjacencyRuleBook.Rules);
            List<AdjacencyEffect> validEffects = AdjacencyEffectResolver.ResolveValidEffects(candidateEffects);

            //清空表现
            DestroyAllChilden(itemLayer);//清场：销毁 itemLayer 所有子物体

            //数据发生改变，让表现重新按照数据生成一遍

            //也就是添加或删除都全部重新按照数据再生成一遍
            for(int y = 0;y< grid.Height; y++)
            {
                for(int x = 0;x< grid.Width; x++)
                {
                    Item item = grid[x, y];
                    if(item == null) continue;

                    // 锚点判断：当前格是物品的"左上角"才画
                    // 检查左边（x>0 且左边的物品引用与当前相同）
                    if(x >0 && grid[x-1,y] == item) continue;
                    // 检查上边（y>0 且上边的物品引用与当前相同）
                    if(y >0 && grid[x,y-1] == item) continue;

                    //都没跳过->这格就是锚点，开始实例化
                    ItemView itemView = Instantiate(itemViewPrefab, itemLayer);
                    RectTransform rect = itemView.GetComponent<RectTransform>();

                    // 设置位置：x 正方向，y 翻转（向下为负）
                    rect.anchoredPosition = new Vector2(x * step, -y * step);

                    //绑定数据
                    if (itemView == null) return;
                    Sprite sprite = itemIconResolver == null ? null : itemIconResolver.GetIcon(item);
                    itemView.Bind(item , step ,this , sprite);

                    //计算联接口产生效果的UI投影
                    ConnectableSides visibleSides = item.GetWorldConnectableSides();
                    ConnectableSides activeSides = GetActiveSides(item, validEffects);
                    itemView.SetConnectors(visibleSides, activeSides);

                    //判断是否要投影武器激活效果UI

                    itemView.SetActiveWeapon(backpackWeaponActivator.IsWeaponItemActive(item));
                    
                }
            }

            RefreshTotalValue();
        }

        //拖拽接口
        //监听鼠标按下瞬间，获取选中的物品
        public void BeginDrag(Item item, ItemView view)
        {
            //获取oldX和oldY
            if (!grid.TryGetAnchor(item, out oldX, out oldY)) return;
            targetX = oldX; targetY = oldY;
            dragItem = item; 
            ghost = view;
            // 关闭门闸，阻止 Redraw
            isDragging = true;
            // 拿起：数据层先离包（Redraw 被门闸拦住）
            grid.Remove(item);

            // 将 ghost 提到网格视觉最上层
            ghost.transform.SetAsLastSibling();
            //关闭提示面板，避免拖拽时提示面板挡住鼠标
            HideTooltip();

        }

        //监听鼠标持续按下时，让物品图标跟随，并计算目标位置
        public void Dragging(Vector2 pointerPos)
        {
            if (!isDragging || ghost == null) return;

            //ghost 跟随鼠标（Screen Space - Overlay 模式下直接使用鼠标位置）
            ghost.transform.position = pointerPos;

            //计算目标格子索引
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                itemLayer, pointerPos, null, out Vector2 localPos);

            targetX = Mathf.FloorToInt(localPos.x / step);
            targetY = Mathf.FloorToInt(-localPos.y / step);// y 翻转

            //判断拖拽位置合法性
            bool rightful = (grid.CanPlaceAt(targetX, targetY, dragItem) 
                || grid.CanMerge(dragItem, grid.GetItemAt(targetX, targetY)));
            ghost.SetValidColor(rightful);

        }

        //监听鼠标松手时，将物品置位或丢弃
        public void EndDrag(Vector2 pointerPos)
        {
            if (!isDragging) return;
            isDragging = false;


            // 面板外松手 = 丢弃到世界（第三结局，最优先判）
            if (!RectTransformUtility.RectangleContainsScreenPoint(bagPanel, pointerPos, null))
            {
                inventorySystem.DiscardToWorld(dragItem);
                Destroy(ghost.gameObject);   // 没有 Place → 没有 OnChanged → 没人替你 Redraw
                dragItem = null;
                ghost = null;
                if (needsRedrawAfterDrag) Redraw(); //在不会触发Redraw的路劲判断是否需要重绘
                return;
            }
            //合成
            if(!grid.CanPlaceAt(targetX, targetY, dragItem))
            {
                Item targetItem = grid.GetItemAt(targetX, targetY);
                if(grid.TryMerge(dragItem , targetItem))
                {
                    // 清理拖拽引用（ghost 会在 Redraw 中被销毁）
                    dragItem = null;
                    ghost = null;
                    if (needsRedrawAfterDrag) Redraw(); //在不会触发Redraw的路劲判断是否需要重绘
                    return;
                }
            }

            //放置
            //松手时有位置，判断放置
            if (grid.CanPlaceAt(targetX, targetY, dragItem))
                grid.Place(targetX, targetY, dragItem); //有位置就放下

            // 新位置不存在，回滚到旧位置
            else if (grid.CanPlaceAt(oldX, oldY, dragItem))
                grid.Place(oldX, oldY, dragItem); 

            //旧位置也被占了就找新位置
            else if (grid.TryFindFreeArea(dragItem, out int fx, out int fy)
                && grid.Place(fx, fy, dragItem)) { }

            else
            {
                //背包满了直接丢弃到世界
                inventorySystem.DiscardToWorld(dragItem);
                Destroy(ghost.gameObject);   // 没有 Place → 没有 OnChanged → 没人替你 Redraw
                dragItem = null;
                ghost = null;
                if (needsRedrawAfterDrag) Redraw(); //在不会触发Redraw的路劲判断是否需要重绘
                return;
            }

            // 清理拖拽引用（ghost 会在 Redraw 中被销毁）
            dragItem = null;
            ghost = null;
        }

        //旋转90度
        private void HandleRotate()
        {
            if (!isDragging || dragItem == null) return;// 非拖拽时按 R 无效
            dragItem.Rotate();
            // ghost 尺寸跟着换（top-left pivot 不动，位置不跳）
            ghost.GetComponent<RectTransform>().sizeDelta =
                new Vector2(dragItem.Width * step, dragItem.Height * step);
            // 红绿判定立刻重算：targetX/Y 没变，但宽高效互换了
            bool rightful = (grid.CanPlaceAt(targetX, targetY, dragItem)
                || grid.CanMerge(dragItem, grid.GetItemAt(targetX, targetY)));
            ghost.SetValidColor(rightful);
            //根据新尺寸sizeDelta再重排接口点和激活角标;
            ConnectableSides visibleSides = ghost.Item.GetWorldConnectableSides();
            ConnectableSides activeSides = ConnectableSides.None;
            ghost.SetConnectors(visibleSides, activeSides);
            ghost.UpdateOverlayLayout(step);
        }

        //Redraw()重绘时调用
        private void DestroyAllChilden(RectTransform parent)
        {
            //倒序删除，不然索引会乱
            for(int i = parent.childCount -1;i>= 0; i--)
            {
                Transform child = parent.GetChild(i);
                Destroy(child.gameObject);
            }
        }

        //获得UI投影需要表现变化的边
        private ConnectableSides GetActiveSides(Item item, List<AdjacencyEffect> effects)
        {
            ConnectableSides activeSides = ConnectableSides.None;
            foreach(var effect in effects)//找到item对应的effect,并获取effect激活的边
            {
                if (effect.ItemA == item)
                    activeSides |= effect.SideA;
                if (effect.ItemB == item)
                    activeSides |= effect.SideB;
            }
            return activeSides;
        }

        private void RefreshTotalValue()
        {
            if (totalValueText == null) return;
            if (grid == null) return;

            totalValueText.text = $"背包价值：￥{grid.GetTotalScoreValue()}";
        }

        public void ShowTooltip(Item item, Vector2 screenPosition)
        {
            if(isDragging) return; //拖拽期间不显示提示面板
            tooltipView?.Show(item, screenPosition);
        }

        public void MoveTooltip(Vector2 screenPosition)
        {
            tooltipView?.Move(screenPosition);
        }

        public void HideTooltip()
        {
            tooltipView?.Hide();
        }

        public void HandleOpenBag()
        {
            if(bagPanelcanvasGroup == null || isDragging) return;
            isBagOpen = !isBagOpen;
            if (isBagOpen)
            {
                bagPanelcanvasGroup.alpha = 1;
                bagPanelcanvasGroup.interactable = true;
                bagPanelcanvasGroup.blocksRaycasts = true;
            }
            else
            {
                bagPanelcanvasGroup.alpha = 0;
                bagPanelcanvasGroup.interactable = false;
                bagPanelcanvasGroup.blocksRaycasts = false;
                HideTooltip();
            }
            Redraw();
        }
    }
}
