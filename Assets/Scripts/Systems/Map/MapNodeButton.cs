using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sitzt auf dem ROOT des Node-Prefabs (trägt BoxCollider für XR-Raycast).
/// Die visuelle Darstellung (Image, Label) liegt auf einem separaten Child "Visual",
/// damit Hover-Skalierung nicht den Collider verzerrt.
/// </summary>
public class MapNodeButton : MonoBehaviour
{
    [Header("Visual References (auf Child 'Visual' zeigen)")]
    [SerializeField] private Transform         visualRoot;   // Child das skaliert wird
    [SerializeField] private Image             bgImage;      // Image auf dem Child
    [SerializeField] private Image             borderImage;  // optionales Border-Child
    [SerializeField] private TextMeshProUGUI   label;

    private static readonly Color ColNormal     = new Color(1f,   1f,   1f,   0.9f);
    private static readonly Color ColHover      = new Color(1f,   1f,   0.5f, 1f);
    private static readonly Color ColSelected   = new Color(0.3f, 1f,   0.5f, 1f);
    private static readonly Color ColReachable  = new Color(1f,   0.85f,0.3f, 1f);
    private static readonly Color ColPast       = new Color(0.35f,0.35f,0.35f,0.6f);
    private static readonly Color ColLocked     = new Color(0.2f, 0.2f, 0.2f, 0.4f);

    private NodeData nodeData;
    private bool     isHovered;

    // ── Init ──────────────────────────────────────────────────

    private void Awake()
    {
        // Fail-Case: Setup unvollständig → deutliche Warnung statt stiller Fehler
        if (visualRoot == null)
        {
            Debug.LogError($"[MapNodeButton] '{name}': visualRoot nicht zugewiesen! " +
                            "Im Prefab-Inspector das 'Visual' Child zuweisen.");
        }
        if (bgImage == null)
        {
            Debug.LogError($"[MapNodeButton] '{name}': bgImage nicht zugewiesen! " +
                            "Im Prefab-Inspector das Image des 'Visual' Child zuweisen.");
        }
    }

    public void SetNode(NodeData data)
    {
        nodeData = data;
        if (label == null && visualRoot != null)
            label = visualRoot.GetComponentInChildren<TextMeshProUGUI>();
        RefreshVisual();
        GetComponent<MapNodeButtonCollider>()?.Sync();
    }

    public NodeData GetNodeData() => nodeData;

    // ── Interaction ───────────────────────────────────────────

    public void OnActivate()
    {
        if (nodeData == null)
        {
            Debug.LogWarning("[MapNodeButton] nodeData ist null! SetNode() wurde nie aufgerufen.");
            return;
        }

        if (!nodeData.isAccessible || nodeData.isLocked || nodeData.isCompleted)
        {
            Debug.Log($"[MapNodeButton] Nicht auswählbar: {nodeData.nodeId} " +
                      $"(accessible={nodeData.isAccessible} locked={nodeData.isLocked} done={nodeData.isCompleted})");
            return;
        }

        Debug.Log($"[MapNodeButton] Aktiviert: [{nodeData.type}] {nodeData.nodeId}");
        RunManager.Instance?.SelectNode(nodeData.nodeId);
    }

    public void OnHoverEnter()
    {
        if (nodeData == null || !nodeData.isAccessible) return;
        isHovered = true;
        RefreshVisual();
    }

    public void OnHoverExit()
    {
        isHovered = false;
        RefreshVisual();
    }

    // ── Visuals ───────────────────────────────────────────────

    public void RefreshVisual()
    {
        if (nodeData == null || bgImage == null) return;

        Color bg     = ColNormal;
        float scale  = 1f;
        bool  border = false;

        if (nodeData.isCompleted && nodeData.nodeId == RunManager.Instance?.CurrentNode?.nodeId)
        {
            bg     = ColSelected;
            scale  = 1.15f;
            border = true;
        }
        else if (nodeData.isCompleted)
        {
            bg = ColPast;
        }
        else if (nodeData.isLocked)
        {
            bg = ColLocked;
        }
        else if (nodeData.isAccessible)
        {
            bg    = isHovered ? ColHover : ColReachable;
            scale = isHovered ? 1.2f : 1.1f;
        }
        else
        {
            bg = ColNormal;
        }

        bgImage.color = bg;

        // Skalierung NUR auf dem visuellen Child — Root/Collider bleibt unverändert
        if (visualRoot != null)
            visualRoot.localScale = Vector3.one * scale;

        if (borderImage != null)
            borderImage.gameObject.SetActive(border);

        if (label != null)
            label.text = GetIcon(nodeData.type);

        GetComponent<MapNodeButtonCollider>()?.Sync();
    }

    private static string GetIcon(NodeType type) => type switch
    {
        NodeType.Combat   => "K",
        NodeType.Elite    => "E",
        NodeType.Event    => "?",
        NodeType.Shop     => "$",
        NodeType.Forge    => "S",
        NodeType.Mystery  => "!",
        NodeType.MiniBoss => "M",
        NodeType.Boss     => "B",
        _                 => "?"
    };
}