using BS.Data;
using UnityEngine;
using static BS.Data.LootTableData;

namespace BS.GamePlay.Loot
{
    public class PickupLogger : MonoBehaviour
    {
        private void OnEnable() => DropItem.OnCollected += HandleCollected;
        private void OnDisable() => DropItem.OnCollected -= HandleCollected;

        private void HandleCollected(LootEntry entry)
        {
            Debug.Log($"捡到：{entry.dropPrefab?.name}，稀有度 {entry.rarity}");
        }
    }
}