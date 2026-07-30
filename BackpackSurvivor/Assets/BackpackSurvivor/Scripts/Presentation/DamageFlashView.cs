using BS.GamePlay.Combat;
using System.Collections;
using UnityEngine;
namespace BS.Presentation
{
    public class DamageFlashView : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.08f;

        private Color[] originalColors;
        private Coroutine flashRoutine; //获得当前的协程，用于连续受击时停掉

        //renderers与originalColors都用数组的是因为复杂一点的prefab不止一个Renderer
        //所以需要依次获取子模型的Renderer，依次缓存，改变，重设颜色。确保受击效果的统一
        private void Awake()
        {
            if (health == null)
                health = GetComponentInParent<Health>();

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();

            CacheOriginalColors();
        }

        private void OnEnable()
        {
            if (health != null)
                health.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (health != null)
                health.OnDamaged -= HandleDamaged;

            if (flashRoutine != null) //防止回收时协程还在跑
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            RestoreColors();
        }
        private void CacheOriginalColors()
        {
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            SetColor(flashColor);

            yield return new WaitForSeconds(Mathf.Max(0.01f, flashDuration));

            RestoreColors();
            flashRoutine = null;
        }

        private void SetColor(Color color)
        {
            for (int i = 0;i < renderers.Length;i++)
            {
                var renderer = renderers[i];
                if(renderer == null) continue;
                renderer.material.color = color;
            }
        }

        private void RestoreColors()
        {
            if (renderers == null || originalColors == null ||
                renderers.Length == 0 || originalColors.Length ==0) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if(renderer == null) continue ;
                renderer.material.color = originalColors[i];
            }
        }
    }
}
