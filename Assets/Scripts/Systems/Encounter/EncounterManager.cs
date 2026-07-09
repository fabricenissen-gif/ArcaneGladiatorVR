using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Zentrale Verwaltung für additives Laden/Entladen von Encounter-Szenen
/// (Arena, Shop, Event etc.). Lebt dauerhaft im Hub, ähnlich RunManager.
/// Verantwortlich für: Szenen-Load/Unload, Spieler-Teleport, Ein-/Ausblenden
/// der Hub-Umgebung und Hub-Map-UI während eines aktiven Encounters.
/// </summary>
public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance { get; private set; }

    [System.Serializable]
    public class NodeTypeSceneMapping
    {
        public NodeType nodeType;
        public string sceneName;
    }

    [Header("Node-Type → Szenen-Zuordnung")]
    [Tooltip("Für jeden NodeType die additiv zu ladende Szene eintragen.")]
    [SerializeField] private List<NodeTypeSceneMapping> sceneMappings = new();

    [Header("Player / Teleport")]
    [Tooltip("Der XR Origin (Rig-Root), der bei Encounter-Wechsel bewegt wird.")]
    [SerializeField] private Transform playerRig;

    [Tooltip("Name des GameObjects, das in jeder Encounter-Szene als Spawn-Punkt dient.")]
    [SerializeField] private string spawnPointName = "SpawnPoint";

    [Tooltip("Referenz auf den Hub-eigenen SpawnPoint, für die Rückkehr.")]
    [SerializeField] private Transform hubSpawnPoint;

    [Header("Hub UI (optional)")]
    [Tooltip("Wird beim Encounter-Start deaktiviert, beim Zurückkehren wieder aktiviert.")]
    [SerializeField] private GameObject hubMapCanvas;

    [Header("Hub Environment")]
    [Tooltip("Wird beim Encounter-Start deaktiviert, beim Zurückkehren wieder aktiviert. " +
             "Enthält NUR Umgebungsgeometrie (Boden, Wände, Deko) — niemals XR Rig, EventSystem oder Manager-Skripte.")]
    [SerializeField] private GameObject hubEnvironmentRoot;

    private Dictionary<NodeType, string> sceneLookup;
    private string currentLoadedScene;
    private bool isLoading;

    public bool IsEncounterActive => !string.IsNullOrEmpty(currentLoadedScene);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildLookup();

        if (playerRig == null)
            Debug.LogWarning("[EncounterManager] Kein Player Rig zugewiesen — Teleport wird nicht funktionieren.");
    }

    private void BuildLookup()
    {
        sceneLookup = new Dictionary<NodeType, string>();
        foreach (var entry in sceneMappings)
        {
            if (string.IsNullOrEmpty(entry.sceneName))
            {
                Debug.LogWarning($"[EncounterManager] Leerer Szenenname für NodeType {entry.nodeType} — wird ignoriert.");
                continue;
            }
            if (sceneLookup.ContainsKey(entry.nodeType))
            {
                Debug.LogWarning($"[EncounterManager] Doppelter Eintrag für NodeType {entry.nodeType} — erster Eintrag bleibt gültig.");
                continue;
            }
            sceneLookup[entry.nodeType] = entry.sceneName;
        }
    }

    // ── Public API ────────────────────────────────────────────

    public void LoadEncounter(NodeData node)
    {
        if (node == null)
        {
            Debug.LogError("[EncounterManager] LoadEncounter mit null-Node aufgerufen.");
            return;
        }

        if (isLoading || IsEncounterActive)
        {
            Debug.LogWarning($"[EncounterManager] Es läuft bereits ein Encounter ('{currentLoadedScene}') " +
                              $"oder ein Ladevorgang — Anfrage für '{node.nodeId}' ignoriert.");
            return;
        }

        if (!sceneLookup.TryGetValue(node.type, out string sceneName))
        {
            Debug.LogError($"[EncounterManager] Kein Szenen-Mapping für NodeType '{node.type}' hinterlegt. " +
                            "Bitte im EncounterManager-Inspector eintragen.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private System.Collections.IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"[EncounterManager] Szene '{sceneName}' konnte nicht geladen werden. " +
                            "Ist sie in den Build Settings eingetragen?");
            isLoading = false;
            yield break;
        }

        yield return op;

        currentLoadedScene = sceneName;

        // Hub-Map ausblenden, damit sie nicht mit der Encounter-Szene überlappt.
        if (hubMapCanvas != null)
            hubMapCanvas.SetActive(false);

        // Hub-Umgebung ausblenden, damit sie sich nicht mit der Encounter-Szene überlagert.
        if (hubEnvironmentRoot != null)
            hubEnvironmentRoot.SetActive(false);
        else
            Debug.LogWarning("[EncounterManager] Kein Hub Environment Root zugewiesen — " +
                              "Hub-Geometrie bleibt sichtbar und kann mit der Encounter-Szene überlappen.");

        TeleportPlayerToSpawnPoint(sceneName);

        isLoading = false;
        Debug.Log($"[EncounterManager] ✔ Szene '{sceneName}' geladen.");
    }

    private void TeleportPlayerToSpawnPoint(string sceneName)
    {
        if (playerRig == null)
        {
            Debug.LogError("[EncounterManager] Kein Player Rig zugewiesen — Teleport übersprungen.");
            return;
        }

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (!loadedScene.IsValid())
        {
            Debug.LogError($"[EncounterManager] Geladene Szene '{sceneName}' nicht gefunden — Teleport übersprungen.");
            return;
        }

        Transform spawnPoint = FindInScene(loadedScene, spawnPointName);
        if (spawnPoint == null)
        {
            Debug.LogError($"[EncounterManager] Kein GameObject '{spawnPointName}' in Szene '{sceneName}' gefunden! " +
                            "Bitte SpawnPoint-Objekt in der Szene anlegen.");
            return;
        }

        // Rig auf Spawn-Position/-Rotation setzen (XR Origin, kein Physik-Move nötig).
        playerRig.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
    }

    private Transform FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName) return root.transform;

            Transform found = root.transform.Find(objectName);
            if (found != null) return found;

            // Fallback: rekursive Suche für tiefer verschachtelte SpawnPoints
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName) return child;
            }
        }
        return null;
    }

    // ── Rückkehr ───────────────────────────────────────────────

    public void ReturnToHub()
    {
        if (!IsEncounterActive)
        {
            Debug.LogWarning("[EncounterManager] ReturnToHub aufgerufen, aber kein Encounter aktiv.");
            return;
        }

        StartCoroutine(UnloadSceneRoutine(currentLoadedScene));
    }

    private System.Collections.IEnumerator UnloadSceneRoutine(string sceneName)
    {
        var op = SceneManager.UnloadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[EncounterManager] Szene '{sceneName}' konnte nicht entladen werden.");
            yield break;
        }

        yield return op;

        currentLoadedScene = null;

        if (hubMapCanvas != null)
            hubMapCanvas.SetActive(true);

        if (hubEnvironmentRoot != null)
            hubEnvironmentRoot.SetActive(true);

        if (playerRig != null && hubSpawnPoint != null)
            playerRig.SetPositionAndRotation(hubSpawnPoint.position, hubSpawnPoint.rotation);
        else
            Debug.LogWarning("[EncounterManager] Kein Hub-SpawnPoint zugewiesen — Spieler bleibt an aktueller Position.");

        Debug.Log($"[EncounterManager] ✔ Szene '{sceneName}' entladen, zurück im Hub.");
    }
}