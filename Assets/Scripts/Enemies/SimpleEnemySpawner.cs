using System;
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
    private int aliveCount;

    /// <summary>Wird ausgelöst, sobald alle gespawnten Gegner gestorben sind.</summary>
    public event Action OnAllEnemiesDefeated;

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
        aliveCount = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint == null) continue;

            Vector3 spawnPosition = spawnPoint.position + Vector3.up * spawnHeightOffset;
            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, spawnPoint.rotation);
            spawnedEnemies.Add(enemyInstance);

            // Fix: An Health.OnDeath hängen, um Arena-Clear zu erkennen.
            // Fail-Case: Prefab ohne Health-Komponente würde sonst nie mitgezählt werden.
            Health health = enemyInstance.GetComponent<Health>();
            if (health == null)
            {
                Debug.LogWarning($"[SimpleEnemySpawner] '{enemyInstance.name}' hat keine Health-Komponente — " +
                                  "wird bei Clear-Check nicht berücksichtigt.");
                continue;
            }

            aliveCount++;
            health.OnDeath += HandleEnemyDeath;
        }

        Debug.Log($"[SimpleEnemySpawner] {aliveCount} Gegner gespawnt.");
    }

    private void HandleEnemyDeath()
    {
        aliveCount--;
        Debug.Log($"[SimpleEnemySpawner] Gegner gestorben. Verbleibend: {aliveCount}");

        if (aliveCount <= 0)
        {
            Debug.Log("[SimpleEnemySpawner] ✔ Alle Gegner besiegt.");
            OnAllEnemiesDefeated?.Invoke();
        }
    }

    public void ClearEnemies()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null)
            {
                // Fail-Case: Event-Listener entfernen, bevor zerstört wird,
                // sonst könnte ein verspäteter OnDeath-Call auf ein zerstörtes Objekt zeigen.
                Health health = spawnedEnemies[i].GetComponent<Health>();
                if (health != null)
                    health.OnDeath -= HandleEnemyDeath;

                Destroy(spawnedEnemies[i]);
            }
        }

        spawnedEnemies.Clear();
        aliveCount = 0;
    }

    private void OnDestroy()
    {
        // Fail-Case: Falls das Spawner-Objekt selbst zerstört wird
        // (z.B. beim additiven Unload der Szene), Listener aufräumen.
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy == null) continue;
            Health health = enemy.GetComponent<Health>();
            if (health != null)
                health.OnDeath -= HandleEnemyDeath;
        }
    }
}