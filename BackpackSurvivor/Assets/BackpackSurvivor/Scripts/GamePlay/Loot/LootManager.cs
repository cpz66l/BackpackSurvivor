using BS.Core;
using BS.Data;
using System.Collections.Generic;
using UnityEngine;
using static BS.Data.LootTableData;
namespace BS.GamePlay.Loot
{
    public class LootManager : MonoBehaviour
    {
        [SerializeField] private ObjectPool dropPool;
        [SerializeField] private ObjectPool currencyPool;
        [SerializeField] private int pityThreshold = 10;//保底数
        [SerializeField] private float offset = 0.8f;

        private LootRoller lootRoller;

        void Start ()
        {
            lootRoller = new LootRoller(pityThreshold);
        }


        //怪物死亡时调用
        public List<GameObject> TrySpawnDrop(Vector3 position , LootTableData bundle)
        {
            List<GameObject> spawned = new List<GameObject>();
            List<LootEntry> list = lootRoller.RollBundle(bundle);
            foreach (LootEntry entry in list)
            {
                GameObject go = SpawnEntry(entry, position);
                if (go != null) spawned.Add(go);
            }
            return spawned;
        }

        // 新的公共口：手里已有 entry 时用（丢弃、以后商店、GM 工具都走这）
        public GameObject SpawnEntry(LootEntry entry, Vector3 position)
        {
            if (entry == null) return null;

            if (entry.category == DropCategory.Equipment)
            {
                DropItem dropItem = dropPool.Get(position).GetComponent<DropItem>();
                dropItem.Initialize(entry);
                return dropItem.gameObject;
            }
            else if (entry.category == DropCategory.Xp)
            {
                Vector2 randomOffset = Random.insideUnitCircle * offset;
                Vector3 pos = position + new Vector3(randomOffset.x, 0, randomOffset.y);
                XpOrb xpOrb = currencyPool.Get(pos).GetComponent<XpOrb>();
                xpOrb.Initialize(entry);
                return xpOrb.gameObject;
            }
            return null;   // Gold 挂账分支
        }
    }
}