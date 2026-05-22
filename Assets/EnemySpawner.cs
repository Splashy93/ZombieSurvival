using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public float spawnInterval = 3f;

    void Start()
    {
        // Spawner'ı başlat
        StartCoroutine(SpawnEnemyRoutine());
    }

    private IEnumerator SpawnEnemyRoutine()
    {
        while (true)
        {
            // Belirlenen süre kadar bekle
            yield return new WaitForSeconds(spawnInterval);

            // Düşman prefab'ı ve spawn noktaları tanımlanmışsa işlemi gerçekleştir
            if (enemyPrefab != null && spawnPoints != null && spawnPoints.Length > 0)
            {
                // Rastgele bir spawn noktası seç
                int randomIndex = Random.Range(0, spawnPoints.Length);
                Transform randomSpawnPoint = spawnPoints[randomIndex];

                // Düşmanı instantiate et
                Instantiate(enemyPrefab, randomSpawnPoint.position, randomSpawnPoint.rotation);
            }
        }
    }
}
