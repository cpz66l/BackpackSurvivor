using BS.GamePlay.Combat;
using BS.GamePlay.Loot;
using BS.GamePlay.Player;
using BS.Inventory;
using UnityEngine;
using static BS.Data.LootTableData;
namespace BS.GamePlay
{
    public class InventorySystem : MonoBehaviour
    {
        public InventoryGrid Grid { get; private set; }
        private Health playerHealth;

        private void Awake()
        {
            playerHealth = FindAnyObjectByType<PlayerController>().GetComponent<Health>();
            Grid = new InventoryGrid(6,8);
        }

        private void OnEnable() => DropItem.OnCollected += HandleCollected;
        private void OnDisable() => DropItem.OnCollected -= HandleCollected;

        private void HandleCollected(LootEntry entry)
        {
            if(entry == null) return;
            Item item = new Item(entry.id ,entry.rarity ,entry.width,entry.height);
            if (Grid.TryFindFreeArea(item, out int x, out int y))
            {
                Grid.Place(x, y, item);
            }
            else
            {
                Debug.Log("背包已满");
            }
           
        }
    }
}
