using BS.Core;
using BS.GamePlay.Interaction;
using BS.Inventory;
using System;
using System.Collections;
using UnityEngine;
using static BS.Data.LootTableData;

namespace BS.GamePlay.Loot
{
    public class DropItem : MonoBehaviour, IPoolable ,ICollectable,IInteractable
    {
        //事件声明
        public static event Action<LootEntry> OnCollected;
        //字段
        [SerializeField] private float rotateSpeed = 180f;
        [SerializeField] private LootEntry lootEntry;
        [SerializeField] private float survivalTime = 60f;
        [SerializeField] private float flightDuration = 0.4f;// 飞行时长
        [SerializeField] private float arcHeight = 2f;// 抛物线峰值高度
        private float survivalTimer = 0f;
        private bool isCollected;
        private Coroutine flightRoutine;

        private Collider itemCollider;
        private InventorySystem inventorySystem;
        
        //对象池
        private ObjectPool pool;
        public void SetPool(ObjectPool p) => pool = p;

        //视觉组件
        private GameObject _visualModel;//Loot模型
        private Renderer modelRb;
        

        private void Awake()
        {
            //创建简易Loot模型
            _visualModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _visualModel.transform.SetParent(transform);//绑定模型的transform与掉落物的父子关系
            _visualModel.transform.localScale = Vector3.one * 0.4f;
            _visualModel.transform.localPosition = Vector3.zero;

            //移除模型碰撞器避免干扰射线检测 (本身不需要物理碰撞)
            Destroy(_visualModel.GetComponent<Collider>());

            //添加简单材质以便视觉识别
            modelRb = _visualModel.GetComponent<Renderer>();
            if (modelRb != null)
            {
                modelRb.material.color = Color.yellow;
            }
            //缓存根物体碰撞器
            itemCollider = GetComponent<Collider>();
            inventorySystem = FindAnyObjectByType<InventorySystem>();
        }


        private void Update()
        {
            transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
            survivalTimer += Time.deltaTime;
            if (survivalTimer >= survivalTime)
            {
                Recycle();
            }
        }

        public void Initialize(LootEntry lootEntry)
        {
            this.lootEntry = lootEntry;
            switch (lootEntry.rarity)
            {
                case Rarity.Common:
                    modelRb.material.color = Color.white;                 // 白
                    break;
                case Rarity.Uncommon:
                    modelRb.material.color = Color.green;                 // 绿
                    break;
                case Rarity.Rare:
                    modelRb.material.color = Color.blue;                  // 蓝
                    break;
                case Rarity.Epic:
                    modelRb.material.color = new Color(0.6f, 0.2f, 0.9f); // 紫
                    break;
                case Rarity.Legendary:
                    modelRb.material.color = new Color(1f, 0.84f, 0f);    // 金
                    break;
            }
        }

        //开箱散落动画
        public void PlayScatterFlight(Vector3 from, Vector3 to)
        {
            transform.position = from;
            if (flightRoutine != null)
            {
                StopCoroutine(flightRoutine);
            }
            flightRoutine = StartCoroutine(FlyRoutine(from, to));
        }
        private IEnumerator FlyRoutine(Vector3 from, Vector3 to)
        {
            itemCollider.enabled = false;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / flightDuration;
                if (t > 1f) t = 1f;// 钳制，防最后一帧飞过终点
                Vector3 horizontal = Vector3.Lerp(from, to, t);
                float height = arcHeight * 4f * t * (1f - t);   // 两端为0，t=0.5时峰值
                transform.position = horizontal + Vector3.up * height;
                yield return null;// 挂起到下一帧
            }
            transform.position = to;// 落点归位（消除累计误差）
            itemCollider.enabled = true;
            flightRoutine = null;
        }
        //交互
        public string GetPrompt()
        {
            string prompt = $"按 E 拾取 {lootEntry.id}";
            return prompt;
        }
        public bool Interact()
        {
            if (inventorySystem.CanAccept(lootEntry))
            {
                Collect();
                return true;
            }
            Debug.Log("背包已满");
            return false;
        }

        //回收与收集
        public void Recycle()
        {
            if (pool != null) pool.Return(gameObject);
            else Destroy(gameObject);//（无池兜底）
        }

        public void Collect()                                 
        {
            if(isCollected) return;//幂等守卫
            isCollected = true;
            OnCollected?.Invoke(lootEntry);// 喊话，带上自己的身份
            Recycle();// 然后回池
        }

        public void OnGetFromPool()
        {
            if (flightRoutine != null)
            {
                StopCoroutine(flightRoutine);
                flightRoutine = null;
            }
            survivalTimer = 0f;
            itemCollider.enabled = true;// 防"飞行中被回收"留下的关碰撞器中毒态
            isCollected = false;
        }
        public void OnReturnPool()
        {

        }
    }
}
