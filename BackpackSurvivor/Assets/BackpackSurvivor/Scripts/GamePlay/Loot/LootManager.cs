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
        public void TrySpawnDrop(Vector3 position , LootTableData bundle)
        {
            List<LootEntry> list = lootRoller.RollBundle(bundle);
            foreach (LootEntry entry in list)
            {
                if (entry == null) continue;
                if (entry.category == DropCategory.Equipment)
                {
                    DropItem dropItem = dropPool.Get(position).GetComponent<DropItem>();
                    dropItem.Initialize(entry);
                }
                else if(entry.category == DropCategory.Xp)
                {
                    Vector2 randomOffset = Random.insideUnitCircle * offset;
                    Vector3 pos = position + new Vector3(randomOffset.x,0, randomOffset.y);
                    XpOrb xpOrb = currencyPool.Get(pos).GetComponent<XpOrb>();
                    xpOrb.Initialize(entry);
                }
                else if(entry.category == DropCategory.Gold)
                {

                }
            }
        }
    }
}