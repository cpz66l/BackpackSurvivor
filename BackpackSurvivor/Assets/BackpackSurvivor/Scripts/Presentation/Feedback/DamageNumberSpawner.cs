using BS.Core;
using BS.GamePlay.Combat;
using UnityEngine;

namespace BS.Presentation
{
    public class DamageNumberSpawner : MonoBehaviour
    {
        [SerializeField] private Health health;
        private ObjectPool damageNumberPool;
        [SerializeField] private Transform spawnAnchor;
        [SerializeField] private SfxPlayer sfx;

        private Vector3 offset = new Vector3(0f, 1.5f, 0f);

        private void Awake()
        {
            if(health ==null)
                health = GetComponentInParent<Health>();
            if (spawnAnchor == null)
                spawnAnchor = GetComponent<Transform>();
            if(sfx == null)
                sfx = FindAnyObjectByType<SfxPlayer>();
        }
        private void Start()
        {
            DamageNumberPoolProvider provider = FindAnyObjectByType<DamageNumberPoolProvider>();
            if (provider != null)
                damageNumberPool = provider.DamageNumberPool;
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
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (damageNumberPool == null) return;

            Vector3 spawnPos = info.hitPoint;
            if (spawnPos == Vector3.zero && health != null)
                spawnPos = health.Position;

            GameObject obj = damageNumberPool.Get(spawnPos + offset);
            DamageNumberView view = obj.GetComponent<DamageNumberView>();

            if (view == null) return;
            view.Play(info.damage);
            sfx?.PlayHit();
        }

    }
}
