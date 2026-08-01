using BS.GamePlay.Combat;
using System.Collections;
using UnityEngine;
namespace BS.Presentation
{
    public class DamageFlashView : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private float flashDuration = 0.08f;

        [SerializeField] private Material flashMaterial; //直接换材质
        private Material[][] originalMaterials;

        private Coroutine flashRoutine; //获得当前的协程，用于连续受击时停掉

        private void Awake()
        {
            if (health == null)
                health = GetComponentInParent<Health>();

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);

            originalMaterials = new Material[renderers.Length][];

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                originalMaterials[i] = renderers[i].sharedMaterials;
            }
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

            RestoreMaterials();
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            ApplyFlashMaterial();

            yield return new WaitForSeconds(Mathf.Max(0.01f, flashDuration));

            RestoreMaterials();
            flashRoutine = null;
        }

        private void ApplyFlashMaterial()
        {
            if (flashMaterial == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;

                Material[] flashMaterials = new Material[renderer.sharedMaterials.Length];

                for (int j = 0; j < flashMaterials.Length; j++)
                    flashMaterials[j] = flashMaterial;

                renderer.sharedMaterials = flashMaterials;
            }
        }

        //清掉临时覆盖，材质自然回到原本颜色。
        private void RestoreMaterials()
        {
            if (renderers == null || originalMaterials == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (originalMaterials[i] == null) continue;

                renderers[i].sharedMaterials = originalMaterials[i];
            }
        }

    }
}
