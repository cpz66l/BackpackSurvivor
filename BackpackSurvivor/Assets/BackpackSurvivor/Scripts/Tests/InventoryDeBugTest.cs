using BS.Inventory;
using UnityEngine;

namespace BS.Tests
{
    public class InventoryDeBugTest : MonoBehaviour
    {
        public InventoryGrid inventoryGrid;
        
        void Start()
        {
            inventoryGrid = new InventoryGrid(6, 8);
            //
            Item item1 = new Item("gun1",2,2);
            bool place1 = inventoryGrid.Place(0, 0, item1);
            Debug.Log($"物品:{item1.Id},大小{item1.Width}x{item1.Height}。" +
                $"放置状态:{place1}");
            bool place4 = inventoryGrid.Place(4, 4, item1);
            Debug.Log(place4);
            //
            Item item2 = new Item("gun2", 1, 1);
            bool place2 = inventoryGrid.Place(1, 1, item2);
            Debug.Log($"物品:{item2.Id},大小{item2.Width} x {item2.Height}。" +
                $"放置状态:{place2}");
            //
            bool place3 = inventoryGrid.Place(2, 2, item2);
            Debug.Log($"物品:{item2.Id},大小{item2.Width}x{item2.Height}。" +
                $"放置状态:{place3}");
            //
            Item item3 = new Item("gun3", 2, 1);
            if (inventoryGrid.TryFindFreeArea(item3,out int x1,out int y1))
            {
                Debug.Log($"物品:{item3.Id} ,大小 {item3.Width} x {item3.Height}。" +
               $"找到空位（{x1}，{y1}）");
            }
            //
            Item item4 = new Item("gun4", 7, 1);
            if (inventoryGrid.TryFindFreeArea(item4, out int x2, out int y2))
            {
                Debug.Log($"物品:{item4.Id}  ,大小  {item4.Width}  x  {item4.Height}。" +
               $"找到空位（{x2}，{y2}）");
            }
            else
            {
                Debug.Log("越界了");
            }
            //
                inventoryGrid.Remove(item1);
            Debug.Log(inventoryGrid.GetItemAt(0,0));
        }

      
    }
}
