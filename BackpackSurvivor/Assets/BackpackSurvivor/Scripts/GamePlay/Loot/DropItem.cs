using BS.Core;
using BS.Data;
using System;
using UnityEngine;
using static BS.Data.LootTableData;

namespace BS.GamePlay.Loot
{
    public class DropItem : MonoBehaviour, IPoolable
    {
        [SerializeField] private float rotateSpeed = 180f;
        [SerializeField] private float survivalTime = 10f;
        [SerializeField] private LootEntry lootEntry;

        private float survivalTimer = 0f;
        //对象池
        private ObjectPool pool;
        public void SetPool(ObjectPool p) => pool = p;
        //引用
        private PickUpMagnet pum;
        //视觉组件
        private GameObject _visualModel;//Loot模型
        private Renderer rd;
        //事件声明
        public static event Action<LootEntry> OnCollected;

        private void Awake()
        {
            //创建简易Loot模型
            _visualModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _visualModel.transform.SetParent(transform);//绑定模型的transform与掉落物的父子关系
            _visualModel.transform.localScale = Vector3.one * 0.4f;
            _visualModel.transform.localPosition = Vector3.zero;

            //移除碰撞器避免干扰射线检测 (本身不需要物理碰撞)
            Destroy(_visualModel.GetComponent<Collider>());

            //添加简单材质以便视觉识别
            rd = _visualModel.GetComponent<Renderer>();
            if (rd != null)
            {
                rd.material.color = Color.yellow;
            }

            pum = GetComponent<PickUpMagnet>();
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
                    rd.material.color = Color.white;                 // 白
                    break;
                case Rarity.Uncommon:
                    rd.material.color = Color.green;                 // 绿
                    break;
                case Rarity.Rare:
                    rd.material.color = Color.blue;                  // 蓝
                    break;
                case Rarity.Epic:
                    rd.material.color = new Color(0.6f, 0.2f, 0.9f); // 紫
                    break;
                case Rarity.Legendary:
                    rd.material.color = new Color(1f, 0.84f, 0f);    // 金
                    break;
            }
        }

        public void Recycle()
        {
            if (pool != null) pool.Return(gameObject);
            else Destroy(gameObject);//（无池兜底）
        }

        public void Collect()                                 
        {
            OnCollected?.Invoke(lootEntry);// 喊话，带上自己的身份
            Recycle();// 然后回池
        }

        public void OnGetFromPool()
        {
            survivalTimer = 0f;
            pum.StateReset();
        }
        public void OnReturnPool()
        {

        }
    }
}
