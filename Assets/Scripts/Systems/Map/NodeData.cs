using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NodeData
{
    public string       nodeId;
    public NodeType     type;
    public int          column;      // X-Position in der Map (0 = Start)
    public int          row;         // Y-Position innerhalb der Spalte
    public List<string> nextNodeIds; // Verbindungen zu nächsten Nodes
    public bool         isCompleted;
    public bool         isAccessible;
    public bool         isLocked;

    public NodeData(string id, NodeType t, int col, int row)
    {
        nodeId       = id;
        type         = t;
        column       = col;
        this.row     = row;
        nextNodeIds  = new List<string>();
        isCompleted  = false;
        isAccessible = false;
        isLocked     = true;
    }
}