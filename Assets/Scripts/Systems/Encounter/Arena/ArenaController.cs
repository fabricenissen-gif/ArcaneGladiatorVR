using UnityEngine;

/// <summary>
/// Sitzt in jeder Combat-Arena-Szene. Verbindet den SimpleEnemySpawner mit
/// der Exit-Tür: Sobald alle gespawnten Gegner besiegt sind, wird die Tür
/// entsperrt.
/// </summary>
public class ArenaController : MonoBehaviour
{
    [Tooltip("Der Spawner, dessen Gegner die Sieg-Bedingung bestimmen.")]
    [SerializeField] private SimpleEnemySpawner enemySpawner;

    [Tooltip("Die Tür, die entsperrt wird, sobald alle Gegner besiegt sind.")]
    [SerializeField] private EncounterExitDoor exitDoor;

    private void Awake()
    {
        if (enemySpawner == null)
            Debug.LogError($"[ArenaController] '{name}': Kein EnemySpawner zugewiesen!");
        if (exitDoor == null)
            Debug.LogError($"[ArenaController] '{name}': Keine Exit-Tür zugewiesen!");
    }

    private void OnEnable()
    {
        if (enemySpawner != null)
            enemySpawner.OnAllEnemiesDefeated += HandleArenaCleared;
    }

    private void OnDisable()
    {
        if (enemySpawner != null)
            enemySpawner.OnAllEnemiesDefeated -= HandleArenaCleared;
    }

    private void HandleArenaCleared()
    {
        Debug.Log("[ArenaController] ✔ Arena cleared — Tür wird entsperrt.");
        exitDoor?.Unlock();
    }
}