using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponSwingDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform trackedWeaponTransform;
    [SerializeField] private WeaponSweepDamage weaponSweepDamage;
    [SerializeField] private WeaponThrowAssist weaponThrowAssist;
    [SerializeField] private WeaponCombatEvents combatEvents;

    [Header("Swing Detection")]
    [SerializeField, Min(0f)] private float swingSpeedThreshold = 1f;
    [SerializeField, Min(0f)] private float resetSpeedThreshold = 0.25f;
    [SerializeField, Min(0f)] private float minTimeBetweenSwings = 0.3f;
    [SerializeField, Min(0f)] private float minSwingDuration = 0.14f;

    private XRGrabInteractable grabInteractable;

    private Vector3 lastPosition;
    private Vector3 currentVelocity;

    private bool swingActive;
    private bool isInSocket;

    private float lastSwingStartTime = float.NegativeInfinity;
    private float lastSwingTime = float.NegativeInfinity;

    private int swingId;

    public bool IsSwinging => swingActive;
    public float CurrentSwingSpeed => currentVelocity.magnitude;
    public int CurrentSwingId => swingId;

    public Vector3 CurrentSwingDirection =>
        currentVelocity.sqrMagnitude > 0.000001f
            ? currentVelocity.normalized
            : Vector3.zero;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (weaponSweepDamage == null)
            weaponSweepDamage = GetComponent<WeaponSweepDamage>();

        if (weaponThrowAssist == null)
            weaponThrowAssist = GetComponent<WeaponThrowAssist>();

        if (combatEvents == null)
            combatEvents = GetComponent<WeaponCombatEvents>();

        if (trackedWeaponTransform == null)
            trackedWeaponTransform = transform;

        if (combatEvents == null)
        {
            Debug.LogWarning(
                $"[WeaponSwingDetector] '{name}' hat keine WeaponCombatEvents-Komponente. " +
                "Swing-Erkennung funktioniert, aber ON_SWING wird nicht veröffentlicht.");
        }
    }

    private void Start()
    {
        lastPosition = trackedWeaponTransform.position;
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
        grabInteractable.selectExited.RemoveListener(OnSelectExited);

        ForceEndSwing();
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        isInSocket = args.interactorObject is XRSocketInteractor;

        if (isInSocket)
            ForceEndSwing();

        lastPosition = trackedWeaponTransform.position;
        currentVelocity = Vector3.zero;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isInSocket = false;
        ForceEndSwing();

        lastPosition = trackedWeaponTransform.position;
        currentVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (trackedWeaponTransform == null)
            return;

        bool weaponIsHeld = grabInteractable.isSelected && !isInSocket;
        bool weaponIsFlying =
            (weaponThrowAssist != null && weaponThrowAssist.IsThrown) ||
            (weaponSweepDamage != null && weaponSweepDamage.isFlying);

        if (!weaponIsHeld || weaponIsFlying)
        {
            ForceEndSwing();
            currentVelocity = Vector3.zero;
            lastPosition = trackedWeaponTransform.position;
            return;
        }

        Vector3 currentPosition = trackedWeaponTransform.position;

        currentVelocity = (currentPosition - lastPosition) /
                          Mathf.Max(Time.deltaTime, 0.0001f);

        float currentSpeed = currentVelocity.magnitude;

        if (!swingActive &&
            currentSpeed >= swingSpeedThreshold &&
            Time.time >= lastSwingTime + minTimeBetweenSwings)
        {
            BeginSwing(currentSpeed);
        }
        else if (swingActive &&
                 Time.time >= lastSwingStartTime + minSwingDuration &&
                 currentSpeed <= resetSpeedThreshold)
        {
            ForceEndSwing();
        }

        lastPosition = currentPosition;
    }

    private void BeginSwing(float swingSpeed)
    {
        swingActive = true;
        lastSwingStartTime = Time.time;
        lastSwingTime = Time.time;
        swingId++;

        weaponSweepDamage?.BeginAttackWindow();

        bool isCharged = false;

        WeaponChargeSystem chargeSystem = GetComponent<WeaponChargeSystem>();
        if (chargeSystem != null)
            isCharged = chargeSystem.IsCharged;

        combatEvents?.RaiseSwing(
            CurrentSwingDirection,
            swingSpeed,
            isCharged,
            swingId);
    }

    private void ForceEndSwing()
    {
        if (!swingActive)
            return;

        swingActive = false;
        weaponSweepDamage?.EndAttackWindow();
    }
}