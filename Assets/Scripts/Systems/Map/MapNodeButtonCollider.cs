using UnityEngine;

/// <summary>
/// Synchronisiert den BoxCollider automatisch mit der RectTransform-Größe.
/// Notwendig weil AutoScaleSpacing in MapUI die Node-Größe zur Laufzeit
/// dynamisch berechnet (30–90px je nach Kartenlayout) — ein fest im
/// Prefab eingestellter Collider würde sonst nicht mehr passen.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(BoxCollider))]
public class MapNodeButtonCollider : MonoBehaviour
{
    [SerializeField] private float colliderDepth = 0.02f; // Dicke in Z für den Raycast-Treffer

    private RectTransform rt;
    private BoxCollider   col;

    private void Awake()
    {
        rt  = GetComponent<RectTransform>();
        col = GetComponent<BoxCollider>();
        Sync();
    }

    private void OnEnable() => Sync();

    // Öffentlich, damit MapNodeButton nach SetNode() / RefreshVisual() erneut synchen kann
    public void Sync()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        if (col == null) col = GetComponent<BoxCollider>();
        if (rt == null || col == null) return;

        Vector2 size = rt.rect.size;

        if (size.x <= 0f || size.y <= 0f)
        {
            // Fail-Case: Layout noch nicht berechnet (z.B. Frame 0 vor Canvas-Rebuild)
            Debug.LogWarning($"[MapNodeButtonCollider] '{name}': RectTransform-Größe ist 0 — " +
                              "Collider-Sync verschoben auf nächsten LateUpdate.");
            return;
        }

        col.size   = new Vector3(size.x, size.y, colliderDepth);
        col.center = Vector3.zero;
        col.isTrigger = true; // wichtig: kein physisches Blockieren, nur Raycast-Ziel
    }

    // Fail-Case Fallback: falls Sync() in Awake zu früh war (Canvas-Rebuild noch nicht fertig)
    private void LateUpdate()
    {
        if (col != null && (col.size.x <= 0f || col.size.y <= 0f))
            Sync();
        enabled = col == null || col.size.x <= 0f; // sich selbst deaktivieren sobald synced
    }
}