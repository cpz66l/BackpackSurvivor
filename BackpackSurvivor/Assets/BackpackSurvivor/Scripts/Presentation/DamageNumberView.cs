using BS.Core;
using System.Collections;
using TMPro;
using UnityEngine;

namespace BS.Presentation
{
    public class DamageNumberView : MonoBehaviour, IPoolable
    {
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float lifetime = 0.6f;
        [SerializeField] private float riseDistance = 1.0f;

        private Camera mainCamera;
        private Coroutine playRoutine;
        private Vector3 startPosition;
        //对象池
        private ObjectPool pool;
        public void SetPool(ObjectPool p) => pool = p;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }
        public void Play(float damage)
        {
            if (damageText != null)
                damageText.text = Mathf.RoundToInt(damage).ToString();

            if (playRoutine != null)
                StopCoroutine(playRoutine);

            playRoutine = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            float t = 0f;
            startPosition = transform.position;
            while(t< lifetime)
            {
                t += Time.deltaTime;
                float ratio = Mathf.Clamp01(t / lifetime);

                transform.position = Vector3.Lerp(
                    startPosition,
                    startPosition + Vector3.up * riseDistance,
                    ratio);

                FaceCamera();//每帧面向相机

                if (canvasGroup != null)
                    canvasGroup.alpha = 1f - ratio;
                yield return null;
            }
            
            playRoutine = null;
            //结束协程，回到对象池
            if (pool != null)
                pool.Return(gameObject);
            else
                gameObject.SetActive(false);
        }
        private void FaceCamera()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null) return;

            transform.rotation = mainCamera.transform.rotation;
        }

        public void OnGetFromPool()
        {
            if (playRoutine != null)
                StopCoroutine(playRoutine);
            playRoutine = null;
            if(canvasGroup != null)
                canvasGroup.alpha = 1;
            if(damageText != null)
                damageText.text = "";
            FaceCamera();
        }

        public void OnReturnPool()
        {
            if (playRoutine != null)
                StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }
}
