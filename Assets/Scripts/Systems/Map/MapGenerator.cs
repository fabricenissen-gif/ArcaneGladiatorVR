using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Shape")]
    [SerializeField] private int   columnsPerFloor = 12;
    [SerializeField] private int   minRows         = 2;
    [SerializeField] private int   maxRows         = 4;

    [Header("Node Type Weights — Normal Columns")]
    [Range(0, 100)] [SerializeField] private int weightCombat  = 45;
    [Range(0, 100)] [SerializeField] private int weightElite   = 15;
    [Range(0, 100)] [SerializeField] private int weightEvent   = 20;
    [Range(0, 100)] [SerializeField] private int weightShop    = 10;
    [Range(0, 100)] [SerializeField] private int weightForge   = 5;
    [Range(0, 100)] [SerializeField] private int weightMystery = 5;

    [Header("References")]
    [SerializeField] private MapData mapData;

    // Shop/Forge garantiert pro Ebene
    private const int GuaranteedShopColumn  = 4;
    private const int GuaranteedForgeColumn = 8;

    public void GenerateFloor(int floor)
    {
        if (mapData == null)
        {
            Debug.LogError("[MapGenerator] Kein MapData zugewiesen!");
            return;
        }

        mapData.Reset();
        mapData.currentFloor = floor;

        var grid = new Dictionary<int, List<NodeData>>(); // col → nodes

        // ── Spalten generieren ────────────────────────────────

        for (int col = 0; col < columnsPerFloor; col++)
        {
            int rowCount = Random.Range(minRows, maxRows + 1);
            grid[col]    = new List<NodeData>();

            for (int row = 0; row < rowCount; row++)
            {
                NodeType type = GetNodeTypeForColumn(col, floor);
                string   id   = $"n_{col}_{row}";
                var      node = new NodeData(id, type, col, row);
                grid[col].Add(node);
                mapData.nodes.Add(node);
            }
        }

        // ── Mini-Boss Spalte (col = columnsPerFloor - 2) ──────
        int miniBossCol = columnsPerFloor - 2;
        grid[miniBossCol].Clear();
        mapData.nodes.RemoveAll(n => n.column == miniBossCol);
        var miniBossNode = new NodeData($"n_{miniBossCol}_0", NodeType.MiniBoss, miniBossCol, 0);
        grid[miniBossCol].Add(miniBossNode);
        mapData.nodes.Add(miniBossNode);

        // ── Boss Spalte (col = columnsPerFloor - 1) ───────────
        int bossCol = columnsPerFloor - 1;
        grid[bossCol].Clear();
        mapData.nodes.RemoveAll(n => n.column == bossCol);
        var bossNode = new NodeData($"n_{bossCol}_0", NodeType.Boss, bossCol, 0);
        grid[bossCol].Add(bossNode);
        mapData.nodes.Add(bossNode);

        // ── Verbindungen generieren ───────────────────────────
        ConnectNodes(grid);

        // ── Start-Node zugänglich machen ─────────────────────
        foreach (var startNode in grid[0])
        {
            startNode.isAccessible = true;
            startNode.isLocked     = false;
        }

        Debug.Log($"[MapGenerator] Ebene {floor + 1} generiert | " +
                  $"{mapData.nodes.Count} Nodes | {columnsPerFloor} Spalten");
    }

    private void ConnectNodes(Dictionary<int, List<NodeData>> grid)
    {
        for (int col = 0; col < columnsPerFloor - 1; col++)
        {
            if (!grid.ContainsKey(col) || !grid.ContainsKey(col + 1)) continue;

            var currentCol = grid[col];
            var nextCol    = grid[col + 1];

            // Jeder Node verbindet sich mit 1-2 Nodes der nächsten Spalte
            foreach (var node in currentCol)
            {
                // Nächstgelegenen Node immer verbinden
                NodeData closest = GetClosestNode(node, nextCol);
                if (closest != null && !node.nextNodeIds.Contains(closest.nodeId))
                    node.nextNodeIds.Add(closest.nodeId);

                // 40% Chance auf zweite Verbindung für Verzweigung
                if (Random.value < 0.4f && nextCol.Count > 1)
                {
                    NodeData second = nextCol[Random.Range(0, nextCol.Count)];
                    if (second != closest && !node.nextNodeIds.Contains(second.nodeId))
                        node.nextNodeIds.Add(second.nodeId);
                }
            }

            // Sicherstellen dass jeder Next-Node mindestens eine eingehende Verbindung hat
            foreach (var nextNode in nextCol)
            {
                bool hasIncoming = false;
                foreach (var node in currentCol)
                    if (node.nextNodeIds.Contains(nextNode.nodeId))
                    { hasIncoming = true; break; }

                if (!hasIncoming)
                {
                    NodeData fallback = currentCol[Random.Range(0, currentCol.Count)];
                    if (!fallback.nextNodeIds.Contains(nextNode.nodeId))
                        fallback.nextNodeIds.Add(nextNode.nodeId);
                }
            }
        }
    }

    private NodeData GetClosestNode(NodeData from, List<NodeData> candidates)
    {
        if (candidates.Count == 0) return null;
        NodeData best     = candidates[0];
        int      bestDiff = Mathf.Abs(from.row - best.row);
        foreach (var c in candidates)
        {
            int diff = Mathf.Abs(from.row - c.row);
            if (diff < bestDiff) { best = c; bestDiff = diff; }
        }
        return best;
    }

    private NodeType GetNodeTypeForColumn(int col, int floor)
    {
        // Garantierte Spalten
        if (col == GuaranteedShopColumn)  return NodeType.Shop;
        if (col == GuaranteedForgeColumn) return NodeType.Forge;

        // Erste Spalte immer Kampf
        if (col == 0) return NodeType.Combat;

        // Gewichteter Zufalls-Pick
        int total   = weightCombat + weightElite + weightEvent +
                      weightShop  + weightForge  + weightMystery;
        int roll    = Random.Range(0, total);
        int running = 0;

        running += weightCombat;  if (roll < running) return NodeType.Combat;
        running += weightElite;   if (roll < running) return NodeType.Elite;
        running += weightEvent;   if (roll < running) return NodeType.Event;
        running += weightShop;    if (roll < running) return NodeType.Shop;
        running += weightForge;   if (roll < running) return NodeType.Forge;

        return NodeType.Mystery;
    }
}