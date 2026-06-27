using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "SpellDelver/MapData")]
public class MapData : ScriptableObject
{
    public int            currentFloor = 0;
    public string         activeNodeId = "";
    public List<NodeData> nodes        = new List<NodeData>();

    public NodeData GetNode(string id)
    {
        return nodes.Find(n => n.nodeId == id);
    }

    public List<NodeData> GetAccessibleNodes()
    {
        return nodes.FindAll(n => n.isAccessible && !n.isCompleted && !n.isLocked);
    }

    public void Reset()
    {
        currentFloor = 0;
        activeNodeId = "";
        nodes        = new List<NodeData>();
    }
}