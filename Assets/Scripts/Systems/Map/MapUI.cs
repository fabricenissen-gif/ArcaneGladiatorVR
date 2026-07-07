using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform mapPanel;
    [SerializeField] private GameObject nodeButtonPrefab;
    [SerializeField] private GameObject linePrefab;

    [Header("Layout")]
    [SerializeField] private float padding = 60f;
    [SerializeField] private float nodeSize = 70f;
    [SerializeField] private float columnSpacing = 100f;
    [SerializeField] private float rowSpacing = 100f;

    [Header("Line Colors")]
    [SerializeField] private Color lineColorActive = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color lineColorDone = new Color(0.6f, 0.6f, 0.6f, 0.9f);

    [Header("Hand State (optional)")]
    [Tooltip("Solange eine dieser Hände eine Waffe hält, wird die Karte für UI-Klicks gesperrt.")]
    [SerializeField] private HandItemTracker rightHandTracker;
    [SerializeField] private HandItemTracker leftHandTracker;

    private RectTransform linesContainer;
    private RectTransform nodesContainer;
    private MapData currentMap;
    private Dictionary<string, Vector2> nodePositions = new();
    private float panelW;
    private float panelH;
    private CanvasGroup canvasGroup;

    // ── Awake ─────────────────────────────────────────────────

    private void Awake()
    {
        if (mapPanel == null)
            mapPanel = GetComponent<RectTransform>();

        if (mapPanel == null)
        {
            Debug.LogError("[MapUI] Kein RectTransform gefunden!");
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        BuildContainers();
    }

    private void Update()
    {
        if (canvasGroup == null) return;

        bool handOccupied = (rightHandTracker != null && rightHandTracker.IsOccupied)
            || (leftHandTracker != null && leftHandTracker.IsOccupied);

        // Sperrt UI-Raycasts komplett solange eine Waffe gehalten wird —
        // ersetzt separates Blocker-Skript am Controller.
        canvasGroup.blocksRaycasts = !handOccupied;
    }

    // ── Container ─────────────────────────────────────────────

    private void BuildContainers()
    {
        if (mapPanel == null) return;

        var ol = mapPanel.Find("LinesContainer");
        var on = mapPanel.Find("NodesContainer");
        if (ol) Destroy(ol.gameObject);
        if (on) Destroy(on.gameObject);

        linesContainer = MakeContainer("LinesContainer");
        nodesContainer = MakeContainer("NodesContainer");
    }

    private RectTransform MakeContainer(string n)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(mapPanel, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 1f);
        rt.localScale = Vector3.one;
        return rt;
    }

    // ── Public API ────────────────────────────────────────────

    public void RenderMap(MapData data)
    {
        if (data == null) return;

        if (mapPanel == null) mapPanel = GetComponent<RectTransform>();
        if (mapPanel == null) { Debug.LogError("[MapUI] mapPanel fehlt!"); return; }
        if (linesContainer == null || nodesContainer == null) BuildContainers();

        currentMap = data;
        ClearMap();
        nodePositions.Clear();

        Canvas.ForceUpdateCanvases();
        panelW = mapPanel.rect.width;
        panelH = mapPanel.rect.height;

        if (panelW <= 0f || panelH <= 0f)
        {
            Debug.LogWarning($"[MapUI] Panel Größe ungültig: {panelW}x{panelH}");
            return;
        }

        AutoScaleSpacing(data);

        foreach (NodeData node in data.nodes)
            nodePositions[node.nodeId] = GetNodePosition(node);

        // Linien zuerst → hinter Nodes
        foreach (NodeData node in data.nodes)
            foreach (string nextId in node.nextNodeIds)
            {
                NodeData next = data.GetNode(nextId);
                if (next == null) continue;
                if (nodePositions.TryGetValue(node.nodeId, out Vector2 from) &&
                    nodePositions.TryGetValue(next.nodeId, out Vector2 to))
                    DrawLine(from, to, node.isCompleted);
            }

        // Nodes danach → vor Linien
        foreach (NodeData node in data.nodes)
            SpawnNode(node, nodePositions[node.nodeId]);
    }

    public void HideMap()
    {
        ClearMap();
    }

    // ── AutoScale ─────────────────────────────────────────────

    private void AutoScaleSpacing(MapData data)
    {
        int maxCol = 0, maxRow = 0;
        foreach (NodeData n in data.nodes)
        {
            if (n.column > maxCol) maxCol = n.column;
            if (n.row > maxRow) maxRow = n.row;
        }

        float usableW = panelW - padding * 2f;
        float usableH = panelH - padding * 2f;

        columnSpacing = maxCol > 0 ? usableW / maxCol : usableW;
        rowSpacing = maxRow > 0 ? usableH / maxRow : usableH;
        nodeSize = Mathf.Clamp(Mathf.Min(columnSpacing, rowSpacing) * 0.65f, 30f, 90f);

        Debug.Log($"[MapUI] Panel:{panelW:F0}x{panelH:F0} | Col:{columnSpacing:F0} Row:{rowSpacing:F0} Node:{nodeSize:F0}");
    }

    // ── Position ──────────────────────────────────────────────

    private Vector2 GetNodePosition(NodeData node)
    {
        float x = padding + node.column * columnSpacing;
        float y = -(padding + node.row * rowSpacing);
        return new Vector2(x, y);
    }

    // ── Spawn Node ────────────────────────────────────────────

    private void SpawnNode(NodeData node, Vector2 pos)
    {
        if (nodeButtonPrefab == null) return;

        GameObject go = Instantiate(nodeButtonPrefab, nodesContainer);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = Vector2.one * nodeSize;
        rt.localScale = Vector3.one;

        // Fix: MapNodeButton kümmert sich jetzt komplett selbst um Button/onClick
        // (siehe MapNodeButton.Awake()). MapUI muss sich darum nicht mehr kümmern.
        var nodeBtn = go.GetComponent<MapNodeButton>();
        if (nodeBtn != null)
        {
            nodeBtn.SetNode(node);
        }
        else
        {
            // Fallback falls kein MapNodeButton auf dem Prefab sitzt
            var img = go.GetComponent<Image>();
            if (img) img.color = GetNodeColor(node);
            var lbl = go.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) lbl.text = GetNodeIcon(node.type);

            var btn = go.GetComponent<Button>();
            if (btn)
            {
                btn.interactable = node.isAccessible && !node.isCompleted && !node.isLocked;
                string id = node.nodeId;
                btn.onClick.AddListener(() => RunManager.Instance?.SelectNode(id));
            }
        }
    }

    // ── Draw Line ─────────────────────────────────────────────

    private void DrawLine(Vector2 from, Vector2 to, bool completed)
    {
        if (linePrefab == null) return;

        Vector2 dir = to - from;
        float length = dir.magnitude;
        if (length < 1f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject go = Instantiate(linePrefab, linesContainer);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = from + dir * 0.5f;
        rt.sizeDelta = new Vector2(length, 4f);
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        rt.localScale = Vector3.one;

        var img = go.GetComponent<Image>();
        if (img)
            img.color = completed ? lineColorDone : lineColorActive;
    }

    // ── Cleanup ───────────────────────────────────────────────

    private void ClearMap()
    {
        if (linesContainer) foreach (Transform c in linesContainer) Destroy(c.gameObject);
        if (nodesContainer) foreach (Transform c in nodesContainer) Destroy(c.gameObject);
    }

    // ── Fallback Helpers (nur wenn kein MapNodeButton) ────────

    private Color GetNodeColor(NodeData node)
    {
        if (node.isCompleted) return new Color(0.4f, 0.4f, 0.4f);
        if (node.isLocked) return new Color(0.2f, 0.2f, 0.2f);
        if (node.isAccessible) return new Color(1f, 0.85f, 0.3f);
        return new Color(0.6f, 0.6f, 0.6f);
    }

    private string GetNodeIcon(NodeType type) => type switch
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