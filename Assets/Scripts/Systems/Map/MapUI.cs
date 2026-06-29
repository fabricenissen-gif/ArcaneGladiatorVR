using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform mapPanel;
    [SerializeField] private GameObject    nodeButtonPrefab;
    [SerializeField] private GameObject    linePrefab;

    [Header("Layout")]
    [SerializeField] private float padding       = 60f;
    [SerializeField] private float nodeSize      = 70f;
    [SerializeField] private float columnSpacing = 100f;
    [SerializeField] private float rowSpacing    = 100f;

    [Header("Node Colors")]
    [SerializeField] private Color colorCombat   = new Color(0.85f, 0.25f, 0.25f);
    [SerializeField] private Color colorElite    = new Color(0.9f,  0.5f,  0.1f);
    [SerializeField] private Color colorEvent    = new Color(0.3f,  0.7f,  0.9f);
    [SerializeField] private Color colorShop     = new Color(0.9f,  0.85f, 0.2f);
    [SerializeField] private Color colorForge    = new Color(0.6f,  0.4f,  0.9f);
    [SerializeField] private Color colorMystery  = new Color(0.5f,  0.5f,  0.5f);
    [SerializeField] private Color colorMiniBoss = new Color(0.9f,  0.3f,  0.6f);
    [SerializeField] private Color colorBoss     = new Color(0.7f,  0.1f,  0.1f);
    [SerializeField] private Color colorLocked   = new Color(0.3f,  0.3f,  0.3f);
    [SerializeField] private Color colorDone     = new Color(0.4f,  0.4f,  0.4f);

    private RectTransform linesContainer;
    private RectTransform nodesContainer;
    private MapData       currentMap;
    private Dictionary<string, Vector2> nodePositions = new();
    private float panelW;
    private float panelH;

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
        BuildContainers();
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
        var go        = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(mapPanel, false);
        var rt        = go.GetComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
        rt.pivot      = new Vector2(0f, 1f);
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
                    nodePositions.TryGetValue(next.nodeId,  out Vector2 to))
                    DrawLine(from, to, node.isCompleted);
            }

        // Nodes danach → vor Linien
        foreach (NodeData node in data.nodes)
            SpawnNode(node, nodePositions[node.nodeId]);
    }

    // ── AutoScale ─────────────────────────────────────────────

    private void AutoScaleSpacing(MapData data)
    {
        int maxCol = 0, maxRow = 0;
        foreach (NodeData n in data.nodes)
        {
            if (n.column > maxCol) maxCol = n.column;
            if (n.row    > maxRow) maxRow = n.row;
        }

        float usableW = panelW - padding * 2f;
        float usableH = panelH - padding * 2f;

        columnSpacing = maxCol > 0 ? usableW / maxCol : usableW;
        rowSpacing    = maxRow > 0 ? usableH / maxRow : usableH;
        nodeSize      = Mathf.Clamp(Mathf.Min(columnSpacing, rowSpacing) * 0.65f, 30f, 90f);

        Debug.Log($"[MapUI] Panel:{panelW:F0}x{panelH:F0} | Col:{columnSpacing:F0} Row:{rowSpacing:F0} Node:{nodeSize:F0}");
    }

    // ── Position ──────────────────────────────────────────────

    private Vector2 GetNodePosition(NodeData node)
    {
        float x =  padding + node.column * columnSpacing;
        float y = -(padding + node.row   * rowSpacing);
        return new Vector2(x, y);
    }

    // ── Spawn Node ────────────────────────────────────────────

    private void SpawnNode(NodeData node, Vector2 pos)
    {
        if (nodeButtonPrefab == null) return;

        GameObject    go = Instantiate(nodeButtonPrefab, nodesContainer);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = Vector2.one * nodeSize;
        rt.localScale       = Vector3.one;

        var label = go.GetComponentInChildren<TextMeshProUGUI>();
        if (label) label.text = GetNodeIcon(node.type);

        var img = go.GetComponent<Image>();
        if (img) img.color = GetNodeColor(node);

        var btn = go.GetComponent<Button>();
        if (btn)
        {
            btn.interactable = node.isAccessible && !node.isCompleted && !node.isLocked;
            string id = node.nodeId;
            btn.onClick.AddListener(() => RunManager.Instance?.SelectNode(id));
        }

        var tooltip = go.GetComponent<MapNodeTooltip>();
        if (tooltip) tooltip.SetNode(node);
    }

    // ── Draw Line ─────────────────────────────────────────────

    private void DrawLine(Vector2 from, Vector2 to, bool completed)
    {
        if (linePrefab == null) return;

        Vector2 dir    = to - from;
        float   length = dir.magnitude;
        if (length < 1f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject    go = Instantiate(linePrefab, linesContainer);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = from + dir * 0.5f;
        rt.sizeDelta        = new Vector2(length, 4f);
        rt.localRotation    = Quaternion.Euler(0f, 0f, angle);
        rt.localScale       = Vector3.one;

        var img = go.GetComponent<Image>();
        if (img)
            img.color = completed
                ? new Color(0.6f, 0.6f, 0.6f, 0.9f)
                : new Color(1f,   1f,   1f,   0.5f);
    }

    // ── Cleanup ───────────────────────────────────────────────

    private void ClearMap()
    {
        if (linesContainer) foreach (Transform c in linesContainer) Destroy(c.gameObject);
        if (nodesContainer) foreach (Transform c in nodesContainer) Destroy(c.gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────

    private Color GetNodeColor(NodeData node)
    {
        if (node.isCompleted) return colorDone;
        if (node.isLocked)    return colorLocked;
        return node.type switch
        {
            NodeType.Combat   => colorCombat,
            NodeType.Elite    => colorElite,
            NodeType.Event    => colorEvent,
            NodeType.Shop     => colorShop,
            NodeType.Forge    => colorForge,
            NodeType.Mystery  => colorMystery,
            NodeType.MiniBoss => colorMiniBoss,
            NodeType.Boss     => colorBoss,
            _                 => colorMystery
        };
    }

    private string GetNodeIcon(NodeType type)
    {
        return type switch
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
}