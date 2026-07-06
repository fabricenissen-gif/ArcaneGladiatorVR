using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Shape")]
    [SerializeField] private int columnsPerFloor = 12;
    [SerializeField] private int rowCount        = 4;
    [SerializeField] private int pathCount       = 6;

    [Header("Node Type Weights")]
    [Range(0,100)] [SerializeField] private int weightCombat  = 45;
    [Range(0,100)] [SerializeField] private int weightElite   = 15;
    [Range(0,100)] [SerializeField] private int weightEvent   = 20;
    [Range(0,100)] [SerializeField] private int weightShop    = 10;
    [Range(0,100)] [SerializeField] private int weightForge   =  5;
    [Range(0,100)] [SerializeField] private int weightMystery =  5;

    [Header("References")]
    [SerializeField] private MapData mapData;

    private int miniBossCol;
    private int bossCol;
    private int totalCols;
    private int midRow;
    private int guaranteedShopCol;
    private int guaranteedForgeCol;

    private List<int>[,] connections;

    // Spalten in denen Pfade noch NICHT zusammenlaufen dürfen
    // (gezählt ab 0 — also die ersten N Spalten sind "isoliert")
    private const int IsolatedStartColumns = 2;

    public void GenerateFloor(int floor)
    {
        if (mapData == null) { Debug.LogError("[MapGenerator] Kein MapData!"); return; }

        // Fix 3: Minimum für columnsPerFloor erzwingen, sonst entstehen entartete Maps
        if (columnsPerFloor < 4)
        {
            Debug.LogWarning($"[MapGenerator] columnsPerFloor={columnsPerFloor} zu klein, setze auf 4.");
            columnsPerFloor = 4;
        }

        if (rowCount < 2) rowCount = 4;
        if (pathCount < 1) pathCount = 6;
        pathCount = Mathf.Min(pathCount, rowCount);

        mapData.Reset();
        mapData.currentFloor = floor;

        int halfNormal = columnsPerFloor / 2;
        miniBossCol = Mathf.Max(1, halfNormal); // Fix 2: miniBossCol darf nie 0 sein
        int postNormal = columnsPerFloor - halfNormal;
        bossCol = miniBossCol + postNormal + 1;
        totalCols = bossCol + 1;
        midRow = (rowCount - 1) / 2;

        guaranteedShopCol  = Mathf.Clamp(Mathf.Max(1, halfNormal / 3), 1, miniBossCol - 1);
        guaranteedForgeCol = Mathf.Clamp(miniBossCol + Mathf.Max(1, postNormal / 2), miniBossCol + 1, bossCol - 1);

        // Fix 1: falls beide auf dieselbe Spalte fallen, Forge einen Schritt verschieben
        if (guaranteedForgeCol == guaranteedShopCol)
            guaranteedForgeCol = Mathf.Clamp(guaranteedForgeCol + 1, miniBossCol + 1, bossCol - 1);

        connections = new List<int>[totalCols, rowCount];
        for (int c = 0; c < totalCols; c++)
            for (int r = 0; r < rowCount; r++)
                connections[c, r] = new List<int>();

        GeneratePaths();
        BuildNodes(floor);

        foreach (var node in mapData.nodes)
            if (node.column == 0) { node.isAccessible = true; node.isLocked = false; }

        Debug.Log($"[MapGenerator] Ebene {floor+1} | {mapData.nodes.Count} Nodes | " +
                $"{pathCount} Pfade | {rowCount} Rows | " +
                $"MiniBoss@{miniBossCol} Boss@{bossCol} Shop@{guaranteedShopCol} Forge@{guaranteedForgeCol}");
    }

    private void GeneratePaths()
    {
        int convSteps    = Mathf.CeilToInt((rowCount - 1) / 2f) + 1;
        int convStartPre = miniBossCol - convSteps;
        int convStartPost= bossCol - convSteps - 1;

        // Strikt unique Startrows: jede Row genau einmal bei pathCount <= rowCount
        var startRows = GetStrictUniqueStartRows(pathCount, rowCount);

        for (int p = 0; p < pathCount; p++)
        {
            int row = startRows[p];

            // ── Erste Hälfte → MiniBoss ────────────────────────
            for (int col = 0; col < miniBossCol; col++)
            {
                int nextRow;
                if (col >= convStartPre)
                    nextRow = MoveTowards(row, midRow, col);
                else
                    nextRow = PickNextRow(row, col, col < IsolatedStartColumns);
                AddConnection(col, row, nextRow);
                row = nextRow;
            }
            AddConnection(miniBossCol - 1, row, midRow);

            // ── MiniBoss → Zweite Hälfte: wieder spreizen ──────
            row = startRows[p]; // zurück zur ursprünglichen Startrow
            AddConnection(miniBossCol, midRow, row);

            // ── Zweite Hälfte → Boss ───────────────────────────
            int postStart = miniBossCol + 1;
            for (int col = postStart; col < bossCol; col++)
            {
                int nextRow;
                if (col >= convStartPost)
                    nextRow = MoveTowards(row, midRow, col);
                else
                {
                    // Auch nach MiniBoss: erste N Spalten isoliert halten
                    bool isolated = (col - postStart) < IsolatedStartColumns;
                    nextRow = PickNextRow(row, col, isolated);
                }
                AddConnection(col, row, nextRow);
                row = nextRow;
            }
            AddConnection(bossCol - 1, row, midRow);
        }
    }

    /// <summary>
    /// Wählt nächste Row.
    /// isolated=true → Pfad MUSS sich bewegen (delta != 0) und darf NICHT auf eine bereits
    /// belegte Row in der nächsten Spalte gehen. Das hält Pfade in den ersten Spalten auseinander.
    /// </summary>
    private int PickNextRow(int currentRow, int col, bool isolated)
    {
        var weighted = new List<int>();

        // Welche Rows sind in col+1 bereits von anderen Pfaden belegt?
        var occupiedNext = new HashSet<int>();
        if (isolated)
            for (int r = 0; r < rowCount; r++)
                foreach (int t in connections[col, r])
                    occupiedNext.Add(t);

        for (int delta = -1; delta <= 1; delta++)
        {
            int r = currentRow + delta;
            if (r < 0 || r >= rowCount) continue;
            if (WouldCross(col, currentRow, r)) continue;

            // Im isolierten Modus: keine Bewegung auf bereits belegte Rows
            if (isolated && occupiedNext.Contains(r)) continue;
            // Im isolierten Modus: gerade nur erlaubt wenn niemand sonst auf currentRow bleibt
            if (isolated && delta == 0) continue;

            if (delta == 0)
            {
                weighted.Add(r);
            }
            else
            {
                weighted.Add(r);
                weighted.Add(r);
                bool awayFromEdge = (currentRow == 0 && delta == 1) ||
                                    (currentRow == rowCount - 1 && delta == -1);
                if (awayFromEdge) weighted.Add(r);
            }
        }

        // Fallback: wenn isoliert und nichts frei → normaler Schritt ohne Isolation
        if (weighted.Count == 0)
        {
            for (int delta = -1; delta <= 1; delta++)
            {
                int r = currentRow + delta;
                if (r < 0 || r >= rowCount) continue;
                if (WouldCross(col, currentRow, r)) continue;
                weighted.Add(r);
            }
        }

        if (weighted.Count == 0) return currentRow;
        return weighted[Random.Range(0, weighted.Count)];
    }

    private int MoveTowards(int currentRow, int targetRow, int col)
    {
        if (currentRow == targetRow) return currentRow;
        int dir       = currentRow < targetRow ? 1 : -1;
        int preferred = currentRow + dir;
        if (preferred >= 0 && preferred < rowCount && !WouldCross(col, currentRow, preferred))
            return preferred;
        if (!WouldCross(col, currentRow, currentRow))
            return currentRow;
        int opp = currentRow - dir;
        if (opp >= 0 && opp < rowCount && !WouldCross(col, currentRow, opp))
            return opp;
        return currentRow;
    }

    private List<int> GetStrictUniqueStartRows(int paths, int rows)
    {
        var available = new List<int>();
        for (int r = 0; r < rows; r++) available.Add(r);
        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }
        var result = new List<int>();
        for (int p = 0; p < paths; p++)
            result.Add(available[p % available.Count]);
        return result;
    }

    private bool WouldCross(int col, int fromRow, int toRow)
    {
        for (int r = 0; r < rowCount; r++)
            foreach (int t in connections[col, r])
                if ((fromRow < r && toRow > t) || (fromRow > r && toRow < t))
                    return true;
        return false;
    }

    private void AddConnection(int col, int fromRow, int toRow)
    {
        if (col < 0 || col >= totalCols - 1) return;
        fromRow = Mathf.Clamp(fromRow, 0, rowCount - 1);
        toRow   = Mathf.Clamp(toRow,   0, rowCount - 1);
        if (!connections[col, fromRow].Contains(toRow))
            connections[col, fromRow].Add(toRow);
    }

    private void BuildNodes(int floor)
    {
        var used = new HashSet<string>();
        for (int col = 0; col < totalCols - 1; col++)
            for (int row = 0; row < rowCount; row++)
                foreach (int nextRow in connections[col, row])
                {
                    used.Add($"{col}_{row}");
                    used.Add($"{col + 1}_{nextRow}");
                }

        var grid = new Dictionary<string, NodeData>();
        foreach (string key in used)
        {
            var p   = key.Split('_');
            int col = int.Parse(p[0]);
            int row = int.Parse(p[1]);
            var nd  = new NodeData($"n_{col}_{row}", GetNodeType(col, floor), col, row);
            grid[key] = nd;
            mapData.nodes.Add(nd);
        }

        for (int col = 0; col < totalCols - 1; col++)
            for (int row = 0; row < rowCount; row++)
            {
                if (!grid.TryGetValue($"{col}_{row}", out var from)) continue;
                foreach (int nextRow in connections[col, row])
                    if (grid.TryGetValue($"{col + 1}_{nextRow}", out var to))
                        if (!from.nextNodeIds.Contains(to.nodeId))
                            from.nextNodeIds.Add(to.nodeId);
            }
    }

    private NodeType GetNodeType(int col, int floor)
    {
        if (col == miniBossCol)        return NodeType.MiniBoss;
        if (col == bossCol)            return NodeType.Boss;
        if (col == guaranteedShopCol)  return NodeType.Shop;
        if (col == guaranteedForgeCol) return NodeType.Forge;
        if (col == 0)                  return NodeType.Combat;

        int total   = weightCombat + weightElite + weightEvent + weightShop + weightForge + weightMystery;
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