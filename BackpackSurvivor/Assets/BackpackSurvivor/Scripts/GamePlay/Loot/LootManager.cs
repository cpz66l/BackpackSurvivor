using BS.Core;
using BS.Data;
using UnityEngine;
using static BS.Data.LootTableData;
namespace BS.GamePlay.Loot
{
    public class LootManager : MonoBehaviour
    {
        [SerializeField] private LootTableData lootTable;//获取一组掉落物配置表
        [SerializeField] private ObjectPool dropPool;
        [SerializeField] private int pityThreshold = 10;//保底数

        private LootRoller lootRoller;

        void Start ()
        {
            lootRoller = new LootRoller(pityThreshold);
        }


        public void TrySpawnDrop(Vector3 position)
        {
            //从这组table配置表中获取掉落物条目
            LootEntry entry = lootRoller.Roll(lootTable);
            if (entry == null) return;
            DropItem dropItem = dropPool.Get(position).GetComponent<DropItem>();
            dropItem.Initialize(entry.rarity);
        }
    }
}