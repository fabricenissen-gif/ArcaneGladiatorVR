using UnityEngine;
using TMPro;

public class MapNodeTooltip : MonoBehaviour
{
    [SerializeField] private GameObject   tooltipPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    private NodeData node;

    public void SetNode(NodeData n)
    {
        node = n;
        if (titleText != null)
            titleText.text = GetNodeName(n.type);
        if (subtitleText != null)
            subtitleText.text = n.isCompleted ? "Abgeschlossen" :
                                n.isLocked    ? "Gesperrt"      : "Verfügbar";
    }

    // Wird von XR Hover-Events aufgerufen
    public void ShowTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    private string GetNodeName(NodeType type) => type switch
    {
        NodeType.Combat   => "Kampf",
        NodeType.Elite    => "Elite-Kampf",
        NodeType.Event    => "Ereignis",
        NodeType.Shop     => "Shop",
        NodeType.Forge    => "Schmiede",
        NodeType.Mystery  => "Mysterium",
        NodeType.MiniBoss => "Mini-Boss",
        NodeType.Boss     => "Boss",
        _                 => "Unbekannt"
    };
}