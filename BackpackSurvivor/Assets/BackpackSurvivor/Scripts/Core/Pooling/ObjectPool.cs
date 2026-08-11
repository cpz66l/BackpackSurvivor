using System.Collections.Generic;
using UnityEngine;

namespace BS.Core
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int prewarmCount = 30;

        private Queue<GameObject> idle = new Queue<GameObject>();
        private HashSet<GameObject> idleSet = new HashSet<GameObject>();

        private void Start()
        {
            Init();
        }

        /// <summary>
        /// 初始化预热（可在 Start 或外部手动调用）
        /// </summary>
        private void Init()
        {
            if(prefab == null)
            {
                Debug.LogError("GameObjectPool: prefab 未设置！");
                return;
            }

            for (int i = 0; i < prewarmCount; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                idle.Enqueue(obj);
                idleSet.Add(obj);
            }
        }

        /// <summary>
        /// 从池中获取一个对象（弹性扩容）
        /// </summary>
        /// <param name="pos">初始世界位置</param>
        /// <returns>可用的 GameObject</returns>
        
        public GameObject Get(Vector3 pos)
        {
            GameObject obj;

            if (idle.Count == 0)
            {
                // 池空 → 扩容：实例化新对象（不经过 idle 队列）
                obj = Instantiate(prefab);
            }
            else 
            { 
                //池非空->出队
                obj = idle.Dequeue();
                idleSet.Remove(obj);
            }

            obj.transform.position = pos;
            obj.SetActive(true);

            //调用IPoolable回调血量，注册表登记等功能
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.SetPool(this);
                //先认领this池子的地址，方便后续Return()
                //注意:新实例化的对象也拿到pool的地址了
                poolable.OnGetFromPool();//重置状态
            }

            return obj;
        }

        /// <summary>
        /// 归还对象到池中
        /// </summary>
        /// <param name="obj">要归还的对象</param>
        public void Return(GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("GameObjectPool: 尝试归还 null 对象");
                return;
            }

            // 防御：防止同一对象重复入队（池污染）
            if (idleSet.Contains(obj))
            {
                Debug.LogWarning($"GameObjectPool: 对象 {obj.name} 已在池中，忽略重复归还");
                return;
            }

            //调用 IPoolable 进行归还的回调
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnReturnPool();
            }

            // 停用并入队
            obj.SetActive(false);
            idle.Enqueue(obj);
            idleSet.Add(obj);
        }
    }
}
