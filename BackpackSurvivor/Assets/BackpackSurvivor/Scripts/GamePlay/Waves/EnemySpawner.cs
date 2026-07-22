using BS.GamePlay.Combat;
using UnityEngine;
using BS.Core;

namespace BS.GamePlay.Waves
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private float maxAlive = 10f;
        [SerializeField] private float spawnOutsideRadius = 15f;
        [SerializeField] private float spawnInsideRadius = 10f;
        [SerializeField] private Transform playerTf;
        [SerializeField] private ObjectPool enemyPool;

        private float spawnTimer = 0f;

        private void Start()
        {
            //如果没拖拽，直接通过找脚本获得Player
            if (playerTf == null) playerTf = FindAnyObjectByType<PlayerController>().transform;
        }

        private void Update()
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer > spawnInterval && TargetRegistry.Count < maxAlive) 
            {
                float radius = Random.Range(spawnInsideRadius, spawnOutsideRadius);
                float angle = Random.Range(0f,360f) * Mathf.Deg2Rad;
                float z = Mathf.Sin(angle) * radius;
                float x = Mathf.Cos(angle) * radius;
                Vector3 spawnPos = playerTf.position + new Vector3 (x,0,z);
                spawnPos.y = 1f;//如果敌人换体型了，得再改改
                enemyPool.Get(spawnPos);
                spawnTimer = 0f;
            }
        }

    }
}
