using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRMapInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float                maxRayDistance = 5f;
    [SerializeField] private LayerMask            mapLayer;
    [SerializeField] private Transform            rayOrigin;

    [Header("Input")]
    [SerializeField] private InputActionReference selectAction;

    [Header("Hand State")]
    [SerializeField] private HandItemTracker      handTracker;

    [Header("Visual")]
    [SerializeField] private LineRenderer         laserLine;
    [SerializeField] private Color                laserIdleColor  = new Color(0.3f, 0.8f, 1f,  0.6f);
    [SerializeField] private Color                laserHoverColor = new Color(1f,   1f,   0.2f, 1f);

    private MapNodeButton currentHovered;
    private bool          selectPressed;

    private void Awake()
    {
        if (rayOrigin   == null) rayOrigin   = transform;
        if (handTracker == null) handTracker = GetComponent<HandItemTracker>();
        SetupLaser();
        SetLaserActive(false); // Start: immer aus
    }

    private void OnEnable()  => selectAction?.action.Enable();
    private void OnDisable()
    {
        selectAction?.action.Disable();
        ClearHover();
        SetLaserActive(false);
    }

    private void Update()
    {
        // Hand hält Waffe → alles aus
        if (handTracker != null && handTracker.IsOccupied)
        {
            ClearHover();
            SetLaserActive(false);
            return;
        }

        CastRay();
        CheckSelectInput();
    }

    // ── Ray Cast ──────────────────────────────────────────────

    private void CastRay()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, mapLayer))
        {
            // Treffer auf Map → Laser an
            MapNodeButton btn = hit.collider.GetComponentInParent<MapNodeButton>();

            if (btn != currentHovered)
            {
                ClearHover();
                currentHovered = btn;
                currentHovered?.OnHoverEnter();
            }

            SetLaserActive(true);
            SetLaserColor(currentHovered != null ? laserHoverColor : laserIdleColor);
            UpdateLaser(ray.origin, hit.point);
        }
        else
        {
            // Kein Treffer → Laser aus
            ClearHover();
            SetLaserActive(false);
        }
    }

    private void CheckSelectInput()
    {
        if (selectAction?.action == null) return;

        bool pressing = selectAction.action.IsPressed();

        if (pressing && !selectPressed)
        {
            selectPressed = true;
            currentHovered?.OnActivate();
        }
        else if (!pressing)
        {
            selectPressed = false;
        }
    }

    // ── Hover ─────────────────────────────────────────────────

    private void ClearHover()
    {
        if (currentHovered == null) return;
        currentHovered.OnHoverExit();
        currentHovered = null;
    }

    // ── Laser ─────────────────────────────────────────────────

    private void SetupLaser()
    {
        if (laserLine == null) return;
        laserLine.positionCount     = 2;
        laserLine.startWidth        = 0.004f;
        laserLine.endWidth          = 0.001f;
        laserLine.useWorldSpace     = true;
        laserLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laserLine.receiveShadows    = false;
        SetLaserColor(laserIdleColor);
    }

    private void UpdateLaser(Vector3 start, Vector3 end)
    {
        if (laserLine == null) return;
        laserLine.SetPosition(0, start);
        laserLine.SetPosition(1, end);
    }

    private void SetLaserColor(Color c)
    {
        if (laserLine == null) return;
        laserLine.startColor = c;
        laserLine.endColor   = new Color(c.r, c.g, c.b, 0f);
    }

    private void SetLaserActive(bool active)
    {
        if (laserLine != null) laserLine.enabled = active;
    }

    // ── Für MapUI Open/Close ───────────────────────────────────
    public void SetMapVisible(bool visible)
    {
        if (!visible)
        {
            ClearHover();
            SetLaserActive(false);
        }
    }
}