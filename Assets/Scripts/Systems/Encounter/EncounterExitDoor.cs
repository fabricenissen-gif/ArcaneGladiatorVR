using UnityEngine;

/// <summary>
/// Sitzt auf der Exit-Zone einer Encounter-Szene (Arena, Shop, Event).
/// Ruft EncounterManager.ReturnToHub() auf, sobald der Spieler die Zone betritt —
/// aber nur wenn isLocked == false. Combat-Szenen setzen isLocked erst über
/// Unlock() auf false, sobald die Sieg-Bedingung erfüllt ist (siehe ArenaController,
/// Phase 5). Shop/Event können Unlock() direkt in Start() aufrufen.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EncounterExitDoor : MonoBehaviour
{
    [Header("State")]
    [Tooltip("Solange true, reagiert die Tür nicht auf den Spieler.")]
    [SerializeField] private bool isLocked = true;

    [Header("Optional Visual")]
    [Tooltip("Wird bei Unlock() aktiv/inaktiv geschaltet, z.B. eine geschlossene vs. offene Tür-Mesh-Variante.")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;

    private bool hasTriggeredReturn;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col == null || !col.isTrigger)
            Debug.LogWarning($"[EncounterExitDoor] '{name}': Collider fehlt oder ist kein Trigger! " +
                              "Is Trigger muss angehakt sein, sonst löst die Zone nicht aus.");

        RefreshVisual();
    }

    // ── Public API ────────────────────────────────────────────

    /// <summary>Wird von ArenaController (Combat) oder Shop/Event-Skripten aufgerufen.</summary>
    public void Unlock()
    {
        if (!isLocked) return;
        isLocked = false;
        RefreshVisual();
        Debug.Log($"[EncounterExitDoor] '{name}' entsperrt.");
    }

    public void Lock()
    {
        isLocked = true;
        hasTriggeredReturn = false;
        RefreshVisual();
    }

    // ── Trigger ───────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggeredReturn) return;

        if (!other.CompareTag("Player"))
        {
            Debug.Log($"[EncounterExitDoor] Trigger von '{other.name}' ignoriert (kein Player-Tag).");
            return;
        }

        if (isLocked)
        {
            Debug.Log("[EncounterExitDoor] Spieler an Tür, aber noch gesperrt.");
            return;
        }

        if (EncounterManager.Instance == null)
        {
            Debug.LogError("[EncounterExitDoor] EncounterManager.Instance ist null! Rückkehr nicht möglich.");
            return;
        }

        hasTriggeredReturn = true;
        Debug.Log($"[EncounterExitDoor] Spieler verlässt Encounter über '{name}'.");
        EncounterManager.Instance.ReturnToHub();
    }

    // ── Visuals ───────────────────────────────────────────────

    private void RefreshVisual()
    {
        if (lockedVisual != null) lockedVisual.SetActive(isLocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(!isLocked);
    }
}