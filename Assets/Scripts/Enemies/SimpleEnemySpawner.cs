using System.Collections.Generic;
using UnityEngine;

public class SimpleEnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int enemiesToSpawn = 3;
    [SerializeField] private float spawnHeightOffset = 0.5f;
    [SerializeField] private bool triggerReset;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Start()
    {
        SpawnEnemies();
    }

    private void Update()
    {
        if (triggerReset)
        {
            triggerReset = false;
            ResetArena();
        }
    }

    public void ResetArena()
    {
        ClearEnemies();
        SpawnEnemies();
        Debug.Log("Arena reset.");
    }

    public void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("SimpleEnemySpawner: enemyPrefab is null.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("SimpleEnemySpawner: no spawn points assigned.");
            return;
        }

        int spawnCount = Mathf.Min(enemiesToSpawn, spawnPoints.Length);

        for (int i = 0; i < spawnCount; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint == null) continue;

            Vector3 spawnPosition = spawnPoint.position + Vector3.up * spawnHeightOffset;
            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, spawnPoint.rotation);
            spawnedEnemies.Add(enemyInstance);
        }
    }

    public void ClearEnemies()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null)
            {
                Destroy(spawnedEnemies[i]);
            }
        }

        spawnedEnemies.Clear();
    }
}