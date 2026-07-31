using BS.Core;
using System;
using UnityEngine;
using static BS.Data.LootTableData;
namespace BS.GamePlay.Loot
{
    public class XpOrb : MonoBehaviour ,IPoolable ,ICollectable
    {
        //静态事件
        public static event Action<LootEntry> OnCollected;
        //字段
        [SerializeField] private float survivalTime = 15f;
        private float survivalTimer = 0f;
        private bool isCollected = false;
        //对象池
        private ObjectPool pool;
        public void SetPool(ObjectPool p) => pool = p;
        public void OnGetFromPool()
        {
            pum.StateReset();
            survivalTimer = 0f;
            isCollected = false;
        }
        public void OnReturnPool()
        {
        }

        //声明或引用
        private PickUpMagnet pum;
        private LootEntry lootEntry;
        private int xpValue = 10;

        //视觉组件
        private GameObject _visualModel;//经验模型
        private Renderer rd;

        private void Awake()
        {
            //创建简易Loot模型
            _visualModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _visualModel.transform.SetParent(transform);//绑定模型的transform与掉落物的父子关系
            _visualModel.transform.localScale = Vector3.one * 0.1f;
            _visualModel.transform.localPosition = Vector3.zero;

            //移除碰撞器避免干扰射线检测 (本身不需要物理碰撞)
            Destroy(_visualModel.GetComponent<Collider>());

            //添加简单材质以便视觉识别
            rd = _visualModel.GetComponent<Renderer>();
            if (rd != null)
            {
                rd.material.color = Color.black;
            }

            pum = GetComponent<PickUpMagnet>();
        }

        private void Update()
        {
            survivalTimer += Time.deltaTime;
            if (survivalTimer >= survivalTime)
            {
                Recycle();
            }
        }

        public void Initialize(LootEntry entry)
        {
            lootEntry = entry;
            xpValue = entry.amount;

        }
        public void Collect()
        {
            if (isCollected) return;   // 幂等守卫
            isCollected = true;
            OnCollected?.Invoke(lootEntry);// 喊话，带上自己的身份
            Recycle();// 然后回池
        }
        public void Recycle()
        {
            if (pool != null) pool.Return(gameObject);
            else Destroy(gameObject);//（无池兜底）
        }
    }
}
