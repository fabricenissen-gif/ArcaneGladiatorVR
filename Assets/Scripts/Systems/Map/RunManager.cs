using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private MapData      mapData;
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private MapUI        mapUI;

    [Header("Scene Names")]
    [SerializeField] private string hubSceneName      = "HubRoom";
    [SerializeField] private string combatSceneName   = "Arena_Combat";
    [SerializeField] private string eliteSceneName    = "Arena_Elite";
    [SerializeField] private string eventSceneName    = "Scene_Event";
    [SerializeField] private string shopSceneName     = "Scene_Shop";
    [SerializeField] private string forgeSceneName    = "Scene_Forge";
    [SerializeField] private string mysterySceneName  = "Scene_Mystery";
    [SerializeField] private string miniBossSceneName = "Arena_MiniBoss";
    [SerializeField] private string bossSceneName     = "Arena_Boss";

    public NodeData ActiveNode   { get; private set; }
    public int      CurrentFloor { get { return mapData != null ? mapData.currentFloor : 0; } }

    private void OnEnable()
    {
        Debug.Log("[RunManager] OnEnable");
    }

    private void Awake()
    {
        Debug.Log("[RunManager] Awake");

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log("[RunManager] Start | mapData null:" + (mapData == null) +
                  " | nodes:" + (mapData != null ? mapData.nodes.Count : -1));

        if (mapData == null)
        {
            Debug.LogError("[RunManager] Kein MapData zugewiesen!");
            return;
        }

        StartNewRun();
    }

    public void StartNewRun()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("[RunManager] Kein MapGenerator zugewiesen!");
            return;
        }

        mapGenerator.GenerateFloor(0);

        if (mapUI != null)
            mapUI.RenderMap(mapData);
        else
            Debug.LogWarning("[RunManager] Kein MapUI zugewiesen.");

        Debug.Log("[RunManager] Neuer Run gestartet.");
    }

    public void SelectNode(string nodeId)
    {
        NodeData node = mapData.GetNode(nodeId);

        if (node == null)
        {
            Debug.LogWarning("[RunManager] Node nicht gefunden: " + nodeId);
            return;
        }

        if (!node.isAccessible || node.isLocked || node.isCompleted)
        {
            Debug.LogWarning("[RunManager] Node nicht wählbar: " + nodeId);
            return;
        }

        ActiveNode           = node;
        mapData.activeNodeId = nodeId;

        string scene = GetSceneForNodeType(node.type);
        Debug.Log("[RunManager] Node gewählt: " + nodeId + " (" + node.type + ") -> Scene: " + scene);
        SceneManager.LoadScene(scene);
    }

    public void CompleteCurrentNode()
    {
        if (ActiveNode == null)
        {
            Debug.LogWarning("[RunManager] CompleteCurrentNode: Kein aktiver Node.");
            return;
        }

        ActiveNode.isCompleted = true;

        foreach (string nextId in ActiveNode.nextNodeIds)
        {
            NodeData next = mapData.GetNode(nextId);
            if (next != null)
            {
                next.isAccessible = true;
                next.isLocked     = false;
            }
        }

        Debug.Log("[RunManager] Node abgeschlossen: " + ActiveNode.nodeId);
        SceneManager.LoadScene(hubSceneName);
    }

    public void AdvanceToNextFloor()
    {
        int nextFloor = mapData.currentFloor + 1;
        if (nextFloor > 2)
        {
            Debug.Log("[RunManager] Run abgeschlossen!");
            return;
        }
        mapGenerator.GenerateFloor(nextFloor);
        if (mapUI != null) mapUI.RenderMap(mapData);
        SceneManager.LoadScene(hubSceneName);
    }

    private string GetSceneForNodeType(NodeType type)
    {
        switch (type)
        {
            case NodeType.Combat:   return combatSceneName;
            case NodeType.Elite:    return eliteSceneName;
            case NodeType.Event:    return eventSceneName;
            case NodeType.Shop:     return shopSceneName;
            case NodeType.Forge:    return forgeSceneName;
            case NodeType.Mystery:  return mysterySceneName;
            case NodeType.MiniBoss: return miniBossSceneName;
            case NodeType.Boss:     return bossSceneName;
            default:                return combatSceneName;
        }
    }
}