using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sitzt auf jedem Map-Node-Button.
/// Verwaltet visuelles Feedback (Hover, Selected, Locked, Past) und leitet Klicks weiter.
/// Registriert den onClick-Listener selbst, damit die Klick-Logik nicht von der
/// Spawn-Reihenfolge in MapUI abhängt (Bug: onClick.AddListener lief vorher nur
/// im Fallback-Zweig, wenn KEIN MapNodeButton vorhanden war — dadurch feuerte
/// Select bei existierendem MapNodeButton nie).
/// </summary>
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
public class MapNodeButton : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Image bgImage;
    [SerializeField] private Image borderImage; // separates Border-Child-Objekt
    [SerializeField] private TextMeshProUGUI label;

    private static readonly Color ColNormal = new Color(1f, 1f, 1f, 0.9f);
    private static readonly Color ColHover = new Color(1f, 1f, 0.5f, 1f);
    private static readonly Color ColSelected = new Color(0.3f, 1f, 0.5f, 1f);
    private static readonly Color ColReachable = new Color(1f, 0.85f, 0.3f, 1f);
    private static readonly Color ColPast = new Color(0.35f, 0.35f, 0.35f, 0.6f);
    private static readonly Color ColLocked = new Color(0.2f, 0.2f, 0.2f, 0.4f);

    private NodeData nodeData;
    private bool isHovered;
    private Button button;

    // ── Init ──────────────────────────────────────────────────

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"[MapNodeButton] Kein Button auf '{name}' gefunden!");
            return;
        }
        // Fix: Listener hier registrieren statt in MapUI, damit er immer läuft.
        button.onClick.AddListener(OnActivate);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnActivate);
    }

    public void SetNode(NodeData data)
    {
        nodeData = data;
        if (bgImage == null) bgImage = GetComponent<Image>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
        RefreshVisual();
    }

    public NodeData GetNodeData() => nodeData;

    // ── Interaction ───────────────────────────────────────────

    /// <summary>Wird vom nativen Button (onClick) aufgerufen.</summary>
    public void OnActivate()
    {
        if (nodeData == null) return;
        if (!nodeData.isAccessible || nodeData.isLocked || nodeData.isCompleted)
        {
            Debug.Log($"[MapNodeButton] Nicht auswählbar: {nodeData.nodeId} " +
                      $"(accessible={nodeData.isAccessible} locked={nodeData.isLocked} done={nodeData.isCompleted})");
            return;
        }
        Debug.Log($"[MapNodeButton] Aktiviert: [{nodeData.type}] {nodeData.nodeId}");
        RunManager.Instance?.SelectNode(nodeData.nodeId);

        // Fail-Case: EncounterManager fehlt in der Szene → lauter Fehler statt stiller ?.-Skip
        if (EncounterManager.Instance == null)
        {
            Debug.LogError("[MapNodeButton] EncounterManager.Instance ist null! " +
                            "Existiert ein GameObject mit EncounterManager-Komponente in der Szene?");
            return;
        }
        EncounterManager.Instance.LoadEncounter(nodeData);
    }

    /// <summary>Laser-Hover beginnt.</summary>
    public void OnHoverEnter()
    {
        if (nodeData == null || !nodeData.isAccessible) return;
        isHovered = true;
        RefreshVisual();
        Debug.Log($"[MapNodeButton] Hover: [{nodeData.type}] {nodeData.nodeId}");
    }

    /// <summary>Laser-Hover endet.</summary>
    public void OnHoverExit()
    {
        isHovered = false;
        RefreshVisual();
    }

    // ── Visuals ───────────────────────────────────────────────

    public void RefreshVisual()
    {
        if (nodeData == null || bgImage == null) return;

        Color bg = ColNormal;
        float scale = 1f;
        bool border = false;

        bool isCurrent = nodeData.nodeId == RunManager.Instance?.CurrentNode?.nodeId;

        if (isCurrent)
        {
            bg = ColSelected;
            scale = 1.15f;
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
            bg = isHovered ? ColHover : ColReachable;
            scale = isHovered ? 1.2f : 1.1f;
        }
        else
        {
            bg = ColNormal;
        }

        bgImage.color = bg;
        transform.localScale = Vector3.one * scale;

        if (borderImage != null)
            borderImage.gameObject.SetActive(border);

        if (label != null)
            label.text = GetIcon(nodeData.type);

        // Fix: Button.interactable synchron mit dem Node-Zustand halten,
        // damit gesperrte/erledigte Nodes keine Klicks mehr auslösen können.
        if (button != null)
            button.interactable = nodeData.isAccessible && !nodeData.isCompleted && !nodeData.isLocked;
    }

    private static string GetIcon(NodeType type) => type switch
    {
        NodeType.Combat => "K",
        NodeType.Elite => "E",
        NodeType.Event => "?",
        NodeType.Shop => "$",
        NodeType.Forge => "S",
        NodeType.Mystery => "!",
        NodeType.MiniBoss => "M",
        NodeType.Boss => "B",
        _ => "?"
    };
}