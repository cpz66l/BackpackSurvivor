using UnityEngine;
using BS.Inventory;
using BS.GamePlay;
namespace BS.Presentation
{
    public class InventoryUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform itemLayer;    // 拖 ItemLayer
        [SerializeField] private ItemView itemViewPrefab;    // 拖预制体
        [SerializeField] private float step = 70f;           // 64格 + 2缝
        [SerializeField] private InventorySystem inventorySystem;

        private InventoryGrid grid;
        private bool isDragging = false;

        //拖拽相关
        private Item dragItem;
        private ItemView ghost;// 被拖的那个视图
        private int oldX, oldY;// 旧锚点（回滚用）
        private int targetX, targetY;// 当前鼠标悬停的目标格子

        private void Start ()
        {
            grid = inventorySystem.Grid;
            grid.OnChanged += Redraw;
            Redraw();
        }

        private void Redraw ()
        {
            if (isDragging) return;// 拖拽期间，数据变化攒着不画

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
                    if(itemView != null)
                        itemView.Bind(item , step ,this);
                }
            }
        }

        //拖拽接口
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

        }

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

            bool canPlace = grid.CanPlaceAt(targetX, targetY, dragItem);
            ghost.SetValidColor(canPlace);
        }

        public void EndDrag()
        {
            if (!isDragging) return;

            isDragging = false;

            if (grid.CanPlaceAt(targetX, targetY, dragItem))
                grid.Place(targetX, targetY, dragItem); 

            else if (grid.CanPlaceAt(oldX, oldY, dragItem))
                grid.Place(oldX, oldY, dragItem); // 回滚到旧位置

            else if (grid.TryFindFreeArea(dragItem, out int fx, out int fy)
                && grid.Place(fx, fy, dragItem)) { }

            else 
            {
                isDragging = true;// 包真的满了：不丢东西，继续手持
                return;// 别清 dragItem/ghost
             }

            // 清理拖拽引用（ghost 会在 Redraw 中被销毁）
            dragItem = null;
            ghost = null;
        }


        private void DestroyAllChilden(RectTransform parent)
        {
            //倒序删除，不然索引会乱
            for(int i = parent.childCount -1;i>= 0; i--)
            {
                Transform child = parent.GetChild(i);
                Destroy(child.gameObject);
            }
        }
    }
}
