using BS.Core;
using BS.Data;
using BS.GamePlay.Interaction;
using System.Collections.Generic;
using UnityEngine;

namespace BS.GamePlay.Loot {
    public class LootChest : MonoBehaviour,IInteractable,IPoolable
    {
        [SerializeField] private string chestName = "宝箱";        // "宝箱" / "隐藏宝箱"
        [SerializeField] private LootTableData lootBundle; // 束表
        [SerializeField] private Renderer chestModel;     //变色用
        [SerializeField] private Transform dropPoint;
        [SerializeField] private float survivalTime = 30f;
        [SerializeField] private float scatterRadius = 0.8f;


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
            chestModel = GetComponentInChildren<Renderer>();
            chestCollider = GetComponent<Collider>();
            originalColor = chestModel.material.color;
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
            chestModel.material.color = color;
            lootBundle = bundle;
        }
        public string GetPrompt()
        {
            string prompt = $"按 E 打开 {chestName}";
            return prompt ;
        }
        
        public void Interact()
        {
            if(opened) return;
            opened = true;
            //关闭碰撞器，避免再次检测
            chestCollider.enabled = false;
            //变灰淡色
            if (chestModel != null)
            {
                chestModel.material.color = Color.black;
            }
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
            chestModel.material.color = originalColor;
            ActiveCount++;
        }

        public void OnReturnPool()
        {
            ActiveCount--;
        }

    } 
}
