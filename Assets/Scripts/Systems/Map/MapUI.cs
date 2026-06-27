using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas          mapCanvas;
    [SerializeField] private RectTransform   nodesContainer;
    [SerializeField] private RectTransform   linesContainer;
    [SerializeField] private GameObject      nodeButtonPrefab;
    [SerializeField] private GameObject      linePrefab;

    [Header("Layout")]
    [SerializeField] private float columnSpacing = 120f;
    [SerializeField] private float rowSpacing    = 130f;
    [SerializeField] private float nodeSize      = 80f;

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

    private MapData                           currentMap;
    private Dictionary<string, RectTransform> nodePositions = new Dictionary<string, RectTransform>();

    public void RenderMap(MapData data)
    {
        if (data == null) return;

        currentMap = data;
        ClearMap();
        nodePositions.Clear();

        foreach (NodeData node in data.nodes)
            SpawnNode(node, GetNodePosition(node));

        foreach (NodeData node in data.nodes)
        {
            foreach (string nextId in node.nextNodeIds)
            {
                NodeData next = data.GetNode(nextId);
                if (next != null)
                    DrawLine(GetNodePosition(node), GetNodePosition(next), node.isCompleted);
            }
        }
    }

    private void SpawnNode(NodeData node, Vector2 pos)
    {
        if (nodeButtonPrefab == null) return;

        GameObject    go = Instantiate(nodeButtonPrefab, nodesContainer);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition        = pos;
        rt.sizeDelta               = Vector2.one * nodeSize;
        nodePositions[node.nodeId] = rt;

        TextMeshProUGUI label = go.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = GetNodeIcon(node.type);

        Image img = go.GetComponent<Image>();
        if (img != null) img.color = GetNodeColor(node);

        Button btn = go.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = node.isAccessible && !node.isCompleted && !node.isLocked;
            string id = node.nodeId;
            btn.onClick.AddListener(() =>
            {
                if (RunManager.Instance != null)
                    RunManager.Instance.SelectNode(id);
            });
        }

        MapNodeTooltip tooltip = go.GetComponent<MapNodeTooltip>();
        if (tooltip != null) tooltip.SetNode(node);
    }

    private void DrawLine(Vector2 from, Vector2 to, bool completed)
    {
        if (linePrefab == null) return;

        GameObject    go  = Instantiate(linePrefab, linesContainer);
        RectTransform rt  = go.GetComponent<RectTransform>();
        Image         img = go.GetComponent<Image>();

        Vector2 dir    = to - from;
        float   length = dir.magnitude;
        float   angle  = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = from + dir * 0.5f;
        rt.sizeDelta        = new Vector2(length, 3f);
        rt.localRotation    = Quaternion.Euler(0f, 0f, angle);

        if (img != null)
            img.color = completed
                ? new Color(0.6f, 0.6f, 0.6f, 0.5f)
                : new Color(1f,   1f,   1f,   0.3f);
    }

    private Vector2 GetNodePosition(NodeData node)
    {
        float x =  node.column * columnSpacing;
        float y = -node.row    * rowSpacing;          // ← negativ!
        if (node.column % 2 == 1) y -= rowSpacing * 0.5f;  // ← minus statt plus
        return new Vector2(x, y);
    }

    private Color GetNodeColor(NodeData node)
    {
        if (node.isCompleted) return colorDone;
        if (node.isLocked)    return colorLocked;

        switch (node.type)
        {
            case NodeType.Combat:   return colorCombat;
            case NodeType.Elite:    return colorElite;
            case NodeType.Event:    return colorEvent;
            case NodeType.Shop:     return colorShop;
            case NodeType.Forge:    return colorForge;
            case NodeType.Mystery:  return colorMystery;
            case NodeType.MiniBoss: return colorMiniBoss;
            case NodeType.Boss:     return colorBoss;
            default:                return colorMystery;
        }
    }

    private string GetNodeIcon(NodeType type)
    {
        switch (type)
        {
            case NodeType.Combat:   return "K";
            case NodeType.Elite:    return "E";
            case NodeType.Event:    return "?";
            case NodeType.Shop:     return "$";
            case NodeType.Forge:    return "S";
            case NodeType.Mystery:  return "!";
            case NodeType.MiniBoss: return "M";
            case NodeType.Boss:     return "B";
            default:                return "?";
        }
    }

    private void ClearMap()
    {
        foreach (Transform child in nodesContainer) Destroy(child.gameObject);
        foreach (Transform child in linesContainer) Destroy(child.gameObject);
    }
}