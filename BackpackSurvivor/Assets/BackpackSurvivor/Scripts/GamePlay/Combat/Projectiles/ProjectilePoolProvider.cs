using BS.Core;
using UnityEngine;

namespace BS.GamePlay.Combat
{
    public class ProjectilePoolProvider : MonoBehaviour
    {
        public static ProjectilePoolProvider Instance { get; private set; } //实现单例

        [SerializeField] private ObjectPool projectilePool;

        public ObjectPool ProjectilePool => projectilePool;

        public static ObjectPool FindProjectilePool()
        {
            if (Instance != null && Instance.projectilePool != null)
                return Instance.projectilePool;

            ProjectilePoolProvider provider = FindAnyObjectByType<ProjectilePoolProvider>();
            if (provider != null)
            {
                Instance = provider;
                return provider.projectilePool;
            }

            return null;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
