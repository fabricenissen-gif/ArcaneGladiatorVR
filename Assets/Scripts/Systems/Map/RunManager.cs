using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private MapUI mapUI;
    [SerializeField] private MapData mapData;

    // Fix: UnityEvent<T> statt UnityEvent, da Invoke(node) / Invoke(mapData)
    // einen Parameter übergibt. Klassen müssen als [System.Serializable]
    // markiert sein, damit sie im Inspector sichtbar sind.
    public NodeSelectedEvent OnNodeSelected = new();
    public MapUpdatedEvent OnMapUpdated = new();

    public NodeData CurrentNode { get; private set; }
    public int CurrentFloor { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => StartRun();

    // ── Public API ────────────────────────────────────────────

    public void StartRun()
    {
        CurrentFloor = 0;
        CurrentNode = null;
        GenerateAndShowFloor();
    }

    public void SelectNode(string nodeId)
    {
        NodeData node = mapData.GetNode(nodeId);
        if (node == null)
        {
            Debug.LogWarning($"[RunManager] Node {nodeId} nicht gefunden.");
            return;
        }

        // Fail-Case: Re-Klick auf den bereits aktiven Node soll keine
        // erneute State-Änderung auslösen (verhinderte vorher versehentlich
        // isCompleted=true auf dem eigenen, noch nicht abgeschlossenen Node).
        if (CurrentNode != null && CurrentNode.nodeId == nodeId)
        {
            Debug.Log($"[RunManager] Node {nodeId} ist bereits aktiv, ignoriere Re-Klick.");
            return;
        }

        if (!node.isAccessible)
        {
            Debug.LogWarning($"[RunManager] Node {nodeId} ist nicht erreichbar.");
            return;
        }
        if (node.isLocked || node.isCompleted)
        {
            Debug.LogWarning($"[RunManager] Node {nodeId} gesperrt oder abgeschlossen.");
            return;
        }

        if (CurrentNode != null)
        {
            CurrentNode.isCompleted = true;
            CurrentNode.isAccessible = false;
        }

        CurrentNode = node;
        node.isAccessible = true;

        LockSiblingNodes(node);
        UnlockNextNodes(node);
        MarkPastNodes(node.column);

        Debug.Log($"[RunManager] ✔ Node gewählt: [{node.type}] Spalte {node.column} Row {node.row}");

        OnNodeSelected.Invoke(node);
        RefreshMap();
    }

    public void CompleteCurrentNode()
    {
        if (CurrentNode == null) return;
        CurrentNode.isCompleted = true;
        Debug.Log($"[RunManager] Node abgeschlossen: [{CurrentNode.type}]");
        RefreshMap();
    }

    // ── Interne Logik ─────────────────────────────────────────

    private void GenerateAndShowFloor()
    {
        mapGenerator.GenerateFloor(CurrentFloor);
        RefreshMap();
    }

    private void RefreshMap()
    {
        OnMapUpdated.Invoke(mapData);
        mapUI.RenderMap(mapData);
    }

    private void LockSiblingNodes(NodeData chosen)
    {
        foreach (var node in mapData.nodes)
        {
            if (node.nodeId == chosen.nodeId) continue;
            if (node.column == chosen.column)
            {
                node.isLocked = true;
                node.isAccessible = false;
            }
        }
    }

    private void UnlockNextNodes(NodeData current)
    {
        foreach (string nextId in current.nextNodeIds)
        {
            NodeData next = mapData.GetNode(nextId);
            if (next == null) continue;
            next.isAccessible = true;
            next.isLocked = false;
        }
    }

    private void MarkPastNodes(int currentColumn)
    {
        foreach (var node in mapData.nodes)
        {
            if (node.column >= currentColumn) continue;
            node.isCompleted = true;
            node.isAccessible = false;
        }
    }
}

// Eigene UnityEvent-Typen mit Parameter, müssen außerhalb der MonoBehaviour-Klasse
// stehen (oder in eigener Datei), damit Unity sie im Inspector serialisieren kann.
[System.Serializable]
public class NodeSelectedEvent : UnityEvent<NodeData> { }

[System.Serializable]
public class MapUpdatedEvent : UnityEvent<MapData> { }