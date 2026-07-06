using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRMapInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float                maxRayDistance   = 5f;
    [SerializeField] private float                sphereCastRadius = 0.015f; // Toleranz gegen Zitter-Bewegung
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

    [Header("Grace Period")]
    [SerializeField] private float                hoverGraceTime = 0.15f; // Sekunden bis Hover als "verloren" gilt

    private MapNodeButton currentHovered;
    private MapNodeButton lastHovered;      // bleibt kurz erhalten falls Hover kurz verloren geht
    private float         lastHoverTime;
    private bool          selectPressed;

    private void Awake()
    {
        if (rayOrigin   == null) rayOrigin   = transform;
        if (handTracker == null) handTracker = GetComponent<HandItemTracker>();

        if (selectAction == null || selectAction.action == null)
            Debug.LogError("[XRMapInteractor] selectAction ist NICHT zugewiesen!");

        SetupLaser();
        SetLaserActive(false);
    }

    private void OnEnable()
    {
        if (selectAction?.action == null) return;
        selectAction.action.Enable();
    }

    private void OnDisable()
    {
        selectAction?.action.Disable();
        ClearHover();
        SetLaserActive(false);
    }

    private void Update()
    {
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

        // SphereCast statt Raycast: verzeiht kleine Zitter-Bewegungen beim Trigger-Druck
        bool didHit = Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, maxRayDistance, mapLayer);

        if (didHit)
        {
            MapNodeButton btn = hit.collider.GetComponentInParent<MapNodeButton>();

            if (btn != currentHovered)
            {
                ClearHover();
                currentHovered = btn;
                currentHovered?.OnHoverEnter();
            }

            if (currentHovered != null)
            {
                lastHovered   = currentHovered;
                lastHoverTime = Time.time;
            }

            SetLaserActive(true);
            SetLaserColor(currentHovered != null ? laserHoverColor : laserIdleColor);
            UpdateLaser(ray.origin, hit.point);
        }
        else
        {
            ClearHover();
            SetLaserActive(false);
        }
    }

    private void CheckSelectInput()
    {
        if (selectAction?.action == null) return;

        bool pressedThisFrame = selectAction.action.WasPressedThisFrame();
        if (!pressedThisFrame) return;

        // Zuerst: aktueller Hover
        if (currentHovered != null)
        {
            Debug.Log($"[XRMapInteractor] Select gedrückt (direkt). Node: {currentHovered.name}");
            currentHovered.OnActivate();
            return;
        }

        // Fallback: Grace Period — Hover ist gerade in diesem Frame verloren gegangen
        // (typisch: Mikro-Bewegung durch Trigger-Fingerdruck), aber war kurz vorher aktiv
        if (lastHovered != null && Time.time - lastHoverTime <= hoverGraceTime)
        {
            Debug.Log($"[XRMapInteractor] Select gedrückt (Grace-Period). Node: {lastHovered.name}");
            lastHovered.OnActivate();
            return;
        }

        Debug.LogWarning("[XRMapInteractor] Trigger gedrückt aber kein Node gehovert (auch nicht innerhalb Grace Period)!");
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

    public void SetMapVisible(bool visible)
    {
        if (!visible)
        {
            ClearHover();
            SetLaserActive(false);
        }
    }
}