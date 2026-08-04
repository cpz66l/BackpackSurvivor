using BS.Core;
using BS.Data;
using BS.GamePlay.Interaction;
using BS.Presentation;
using System.Collections.Generic;
using UnityEngine;

namespace BS.GamePlay.Loot {
    public class LootChest : MonoBehaviour,IInteractable,IPoolable
    {
        //静态列表，全局的宝箱都可以使用
        private static readonly List<LootChest> unopenedChests = new List<LootChest>();

        [SerializeField] private string chestName = "宝箱";        // "宝箱" / "隐藏宝箱"
        [SerializeField] private LootTableData lootBundle; // 束表
        [SerializeField] private Renderer modelRb;     //变色用
        [SerializeField] private Transform dropPoint;
        [SerializeField] private float survivalTime = 30f;
        [SerializeField] private float scatterRadius = 0.8f;
        [SerializeField] private SfxPlayer sfx;

        private float survivalTimer = 0f;
        private Collider chestCollider;
        private LootManager lootManager;
        private bool opened = false;
        private Color originalColor;

        //宝箱数目
        public static int ActiveCount { get; private set; }

        //池化
        private ObjectPool pool;
        public void SetPool(ObjectPool p) => pool = p;

        private void Awake()
        {
            lootManager = FindAnyObjectByType<LootManager>();
            modelRb = GetComponentInChildren<Renderer>();
            chestCollider = GetComponent<Collider>();
            originalColor = modelRb.material.color;
            if (sfx == null)
                sfx = FindAnyObjectByType<SfxPlayer>();
        }

        private void Update()
        {
            if (opened)
            {
                survivalTimer += Time.deltaTime;
                if(survivalTimer >= survivalTime)
                {
                    Recycle();
                }
            }
        }

        public void Initialize(string name, Color color, LootTableData bundle)
        {
            chestName = name;
            modelRb.material.color = color;
            lootBundle = bundle;
        }
        public string GetPrompt()
        {
            string prompt = $"按 E 打开 {chestName}";
            return prompt ;
        }
        
        public bool Interact()
        {
            if(opened) return false;
            opened = true;
            unopenedChests.Remove(this);
            //关闭碰撞器，避免再次检测
            chestCollider.enabled = false;
            //变灰淡色
            if (modelRb != null)
            {
                modelRb.material.color = Color.black;
            }
            //开宝箱音效
            sfx?.PlayChestOpen();
            //生成物品
            //drops先拿到总共生成的物品
            List<GameObject> drops = lootManager.TrySpawnDrop(dropPoint == null ? transform.position : dropPoint.position, lootBundle);
            //遍历每个drop,让它们随机沿抛物线散落
            foreach (GameObject d in drops)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                Vector3 target = transform.position + new Vector3(offset.x, 0, offset.y);
                d.GetComponent<DropItem>()?.PlayScatterFlight(dropPoint == null ? transform.position : dropPoint.position, target);
            }
            return true;
        }

        public void Recycle()
        {
            if (pool != null) pool.Return(gameObject);
            else Destroy(gameObject);//（无池兜底）
        }

        public void OnGetFromPool()
        {
            survivalTimer = 0f;
            opened = false;
            chestCollider.enabled = true;
            modelRb.material.color = originalColor;
            ActiveCount++;

            if (!unopenedChests.Contains(this))
                unopenedChests.Add(this);
        }

        public void OnReturnPool()
        {
            ActiveCount--;
            unopenedChests.Remove(this);
        }


        public static bool TryGetNearestUnopened(Vector3 from, out LootChest nearest)
        {
            nearest = null;
            float sqrNearestDistance = float.MaxValue;
            if(unopenedChests != null && unopenedChests.Count != 0)
                foreach (var chest in unopenedChests)
                {
                    if(chest == null) continue;
                    if(chest.gameObject.activeInHierarchy == false) continue;
                    float sqrDistance = (chest.transform.position - from).sqrMagnitude;
                    if (sqrDistance < sqrNearestDistance)
                    {
                        sqrNearestDistance = sqrDistance;
                        nearest = chest;
                    }
                }

            return nearest != null;
        }
    } 
}
