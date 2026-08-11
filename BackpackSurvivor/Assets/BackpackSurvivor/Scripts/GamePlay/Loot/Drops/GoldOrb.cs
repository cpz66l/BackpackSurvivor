using BS.Core;
using System;
using System.Collections;
using UnityEngine;
using static BS.Data.LootTableData;

namespace BS.GamePlay.Loot
{
    [RequireComponent(typeof(PickUpMagnet))]
    public class GoldOrb : MonoBehaviour, BS.Core.IPoolable, ICollectable
    {
        //静态事件
        public static event Action<LootEntry> OnCollected;
        //字段
        [SerializeField] private float rotateSpeed = 180f;
        [SerializeField] private float survivalTime = 15f;
        private float survivalTimer = 0f;
        private bool isCollected = false;

        //声明或引用
        private PickUpMagnet pum;
        private LootEntry lootEntry;

        //散落协程
        [SerializeField] private float flightDuration = 0.35f;
        [SerializeField] private float arcHeight = 1.2f;
        private Coroutine flightRoutine;

        //对象池
        private ObjectPool pool;
        public void SetPool(ObjectPool p) => pool = p;
        public void OnGetFromPool()
        {
            //重置状态
            if (flightRoutine != null)
            {
                StopCoroutine(flightRoutine);
                flightRoutine = null;
            }
            pum.enabled = true;
            pum.StateReset();

            survivalTimer = 0f;
            isCollected = false;
        }
        public void OnReturnPool()
        {
        }

        private void Awake()
        {
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

        public void Initialize(LootEntry entry)
        {
            lootEntry = entry;

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

        public void PlayScatterFlight(Vector3 from, Vector3 to)
        {
            transform.position = from;

            if (flightRoutine != null)
                StopCoroutine(flightRoutine);

            flightRoutine = StartCoroutine(FlyRoutine(from, to));
        }

        private IEnumerator FlyRoutine(Vector3 from, Vector3 to)
        {
            // 这里先禁用磁吸/碰撞，落地再恢复
            pum.enabled = false;
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
            pum.enabled = true;
            pum.StateReset();
            flightRoutine = null;
        }
    }
}
