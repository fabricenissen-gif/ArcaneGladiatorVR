using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponSwingDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform         trackedWeaponTransform;
    [SerializeField] private WeaponSweepDamage weaponSweepDamage;
    [SerializeField] private WeaponThrowAssist weaponThrowAssist;

    [Header("Swing Detection")]
    [SerializeField] private float swingSpeedThreshold  = 1.0f;
    [SerializeField] private float resetSpeedThreshold  = 0.25f;
    [SerializeField] private float minTimeBetweenSwings = 0.30f;
    [SerializeField] private float minSwingDuration     = 0.14f;

    private Vector3 lastPosition;
    private Vector3 currentVelocity;
    private bool    swingActive;
    private float   lastSwingStartTime = -999f;
    private float   lastSwingTime      = -999f;
    private int     swingId;

    private XRGrabInteractable grabInteractable;
    private bool               isInSocket = false;

    public bool    IsSwinging            => swingActive;
    public float   CurrentSwingSpeed     => currentVelocity.magnitude;
    public int     CurrentSwingId        => swingId;
    public Vector3 CurrentSwingDirection => currentVelocity.sqrMagnitude > 0.000001f
        ? currentVelocity.normalized : Vector3.zero;

    // ── Lifecycle ─────────────────────────────────────────────

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (weaponThrowAssist == null)
            weaponThrowAssist = GetComponent<WeaponThrowAssist>();
    }

    private void Start()
    {
        if (trackedWeaponTransform == null)
            trackedWeaponTransform = transform;
        lastPosition = trackedWeaponTransform.position;
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    // ── XR Events ─────────────────────────────────────────────

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor)
        {
            isInSocket = true;
            ForceEndSwing();
        }
        else
        {
            isInSocket = false;
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isInSocket = false;
        ForceEndSwing();
    }

    // ── Update ────────────────────────────────────────────────

    private void Update()
    {
        if (isInSocket) return;

        // Schwert fliegt — absolut kein Swing möglich
        bool flying = (weaponThrowAssist != null && weaponThrowAssist.IsThrown)
                   || (weaponSweepDamage != null && weaponSweepDamage.isFlying);

        if (flying)
        {
            if (swingActive) ForceEndSwing();
            lastPosition = trackedWeaponTransform.position;
            return;
        }

        Vector3 currentPosition = trackedWeaponTransform.position;
        Vector3 frameDelta      = currentPosition - lastPosition;
        currentVelocity         = frameDelta / Mathf.Max(Time.deltaTime, 0.0001f);
        float   speed           = currentVelocity.magnitude;

        if (!swingActive &&
            speed >= swingSpeedThreshold &&
            Time.time >= lastSwingTime + minTimeBetweenSwings)
        {
            swingActive        = true;
            lastSwingStartTime = Time.time;
            lastSwingTime      = Time.time;
            swingId++;

            if (weaponSweepDamage != null)
                weaponSweepDamage.BeginAttackWindow();

            Debug.Log($"[WeaponSwingDetector] Swing started. ID:{swingId} Speed:{speed:F2}");
        }
        else if (swingActive &&
                 Time.time >= lastSwingStartTime + minSwingDuration &&
                 speed <= resetSpeedThreshold)
        {
            ForceEndSwing();
        }

        lastPosition = currentPosition;
    }

    // ── Helpers ───────────────────────────────────────────────

    private void ForceEndSwing()
    {
        if (!swingActive) return;
        swingActive = false;
        if (weaponSweepDamage != null)
            weaponSweepDamage.EndAttackWindow();
        Debug.Log("[WeaponSwingDetector] Swing force-ended.");
    }
}