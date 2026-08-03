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
        private LootManager lootManager;

        private void Awake()
        {
            playerHealth = FindAnyObjectByType<PlayerController>().GetComponent<Health>();
            Grid = new InventoryGrid(6,8);
            lootManager = FindAnyObjectByType<LootManager>();
        }
        private void OnEnable()
        {
            DropItem.OnCollected += HandleCollected;
            XpOrb.OnCollected += HandleCurrency;
        }
        private void OnDisable()
        {
            DropItem.OnCollected -= HandleCollected;
            XpOrb.OnCollected -= HandleCurrency;
        }

        //交互的许可侦察兵,只问不放
        public bool CanAccept(LootEntry entry)
        {
            if (entry == null) return false;
            Item probe = CreateItemFromLootEntry(entry);
            if (Grid.TryFindFreeArea(probe, out _, out _)) return true;
            return false;
        }

        private void HandleCollected(LootEntry entry)
        {
            if(entry == null) return;
            Item item = CreateItemFromLootEntry(entry);
            if (Grid.TryFindFreeArea(item, out int x, out int y))
            {
                Grid.Place(x, y, item);
            }
            else
            {
                //背包满了直接丢弃到世界
                DiscardToWorld(item);
            }
           
        }

        private void HandleCurrency(LootEntry entry)
        {
            if (entry == null) return;
            Debug.Log($"经验 +{entry.amount}");
        }

        public void DiscardToWorld(Item item)
        {
            // Item → LootEntry 还原（往返保真的关键一步，字段一个不能丢）
            LootEntry entry = new LootEntry
            {
                category = DropCategory.Equipment,
                id = item.Id,
                rarity = item.Rarity,
                width = item.Width,
                height = item.Height,
                amount = 1,
                itemTag = item.Tag,
                connectableSides = item.LocalConnectableSides,
                scoreValue = item.BaseScoreValue,
                effectValue = item.BaseEffectValue,
            };

            Vector3 from = playerHealth.Position;                       // 玩家胸口（aimPoint）
            GameObject go = lootManager.SpawnEntry(entry, from);

            Vector2 offset = Random.insideUnitCircle * 2f;              // 抛到身旁 2m 内
            Vector3 to = new Vector3(from.x + offset.x, 0.5f, from.z + offset.y);
            go.GetComponent<DropItem>()?.PlayScatterFlight(from, to);
        }

        //物品定义入口
        private Item CreateItemFromLootEntry(LootEntry entry)
        {
            if (entry == null) return null;

            return new Item(
                entry.id,
                entry.rarity,
                entry.width,
                entry.height,
                entry.itemTag,
                entry.connectableSides,
                entry.scoreValue,
                entry.effectValue);
        }
    }
}
