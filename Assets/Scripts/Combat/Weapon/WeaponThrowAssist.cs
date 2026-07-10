using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponThrowAssist : MonoBehaviour
{
    public enum ThrowStyle
    {
        Dart,
        Chaotic
    }

    [Header("Throw Style Settings")]
    public ThrowStyle currentThrowStyle = ThrowStyle.Dart;

    [Header("Charge Integration")]
    [SerializeField] private WeaponChargeSystem chargeSystem;
    [SerializeField, Min(0f)] private float chargedThrowSpeedMultiplier = 3f;

    [Header("General Flight Settings")]
    [SerializeField] private Transform flightDirectionRef;
    [SerializeField, Min(0f)] private float throwSpeedMultiplier = 1.3f;
    [SerializeField, Min(0f)] private float minimumThrowSpeed = 0.5f;

    [Header("Dart Style Settings")]
    [SerializeField, Min(0f)] private float dartAlignmentSpeed = 20f;

    [Range(0f, 2f)]
    [SerializeField] private float dartGravityScale = 0.6f;

    [Range(0f, 1f)]
    [SerializeField] private float dartCenterOfMassOffset = 0.4f;

    [Header("Chaotic Style Settings")]
    [SerializeField, Min(0f)] private float chaoticAlignmentForce = 50f;

    [Header("Sticking")]
    [SerializeField] private LayerMask stickableLayers;
    [SerializeField, Min(0f)] private float minVelocityToStick = 3f;
    [SerializeField, Min(0f)] private float penetrationDepth = 0.15f;

    [Header("Flight Hit Detection")]
    [SerializeField, Min(0.01f)] private float flightCastRadius = 0.12f;
    [SerializeField] private LayerMask flightHitLayers;

    private readonly Vector3[] velocitySamples = new Vector3[5];

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private WeaponSweepDamage sweepDamage;
    private WeaponCombatEvents combatEvents;

    private Coroutine pendingThrowRoutine;

    private Vector3 lastPosition;
    private int velocitySampleIndex;

    private bool isHeld;
    private bool isThrown;
    private bool isStuck;
    private bool damageDealtThisThrow;
    private bool wasChargedAtRelease;

    public bool IsThrown => isThrown;
    public bool IsHeld => isHeld;
    public bool IsStuck => isStuck;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        sweepDamage = GetComponent<WeaponSweepDamage>();
        combatEvents = GetComponent<WeaponCombatEvents>();

        if (chargeSystem == null)
            chargeSystem = GetComponent<WeaponChargeSystem>();

        if (combatEvents == null)
        {
            Debug.LogWarning(
                $"[WeaponThrowAssist] '{name}' has no WeaponCombatEvents component. " +
                "Throw behaviour still works, but ON_THROW_RELEASE is not published.");
        }
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);

        StopPendingThrow();
    }

    private void FixedUpdate()
    {
        if (isHeld)
        {
            SampleHeldVelocity();
            return;
        }

        if (!isThrown || isStuck)
            return;

        ProcessFlight();
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        StopPendingThrow();

        isHeld = true;
        isThrown = false;
        isStuck = false;
        damageDealtThisThrow = false;
        wasChargedAtRelease = false;

        ResetVelocitySamples();
        ResetPhysicsForHeldWeapon();

        if (sweepDamage != null)
            sweepDamage.isFlying = false;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor)
            return;

        isHeld = false;
        wasChargedAtRelease = chargeSystem != null && chargeSystem.IsCharged;

        Vector3 releaseVelocity = GetAverageReleaseVelocity();

        if (releaseVelocity.magnitude < minimumThrowSpeed)
        {
            isThrown = false;

            if (sweepDamage != null)
                sweepDamage.isFlying = false;

            return;
        }

        if (sweepDamage != null)
            sweepDamage.isFlying = true;

        StopPendingThrow();
        pendingThrowRoutine = StartCoroutine(
            BeginThrowAfterXriRelease(releaseVelocity));
    }

    private IEnumerator BeginThrowAfterXriRelease(Vector3 releaseVelocity)
    {
        const int maxFramesToWait = 10;

        for (int i = 0; i < maxFramesToWait && rb.isKinematic; i++)
            yield return null;

        if (rb.isKinematic)
            rb.isKinematic = false;

        float speedMultiplier = wasChargedAtRelease
            ? chargedThrowSpeedMultiplier
            : throwSpeedMultiplier;

        Vector3 finalVelocity = releaseVelocity * speedMultiplier;

        rb.linearVelocity = finalVelocity;
        rb.angularVelocity = Vector3.zero;

        isThrown = true;
        isStuck = false;
        damageDealtThisThrow = false;
        lastPosition = transform.position;

        if (sweepDamage != null)
            sweepDamage.isFlying = true;

        ConfigureFlightPhysics();

        Vector3 throwDirection = finalVelocity.sqrMagnitude > 0.0001f
            ? finalVelocity.normalized
            : transform.forward;

        combatEvents?.RaiseThrowReleased(
            throwDirection,
            finalVelocity.magnitude,
            wasChargedAtRelease);

        pendingThrowRoutine = null;
    }

    private void SampleHeldVelocity()
    {
        velocitySamples[velocitySampleIndex % velocitySamples.Length] =
            rb.linearVelocity;

        velocitySampleIndex++;
    }

    private Vector3 GetAverageReleaseVelocity()
    {
        Vector3 totalVelocity = Vector3.zero;
        int validSampleCount = 0;

        foreach (Vector3 sample in velocitySamples)
        {
            if (sample.sqrMagnitude <= 0.001f)
                continue;

            totalVelocity += sample;
            validSampleCount++;
        }

        return validSampleCount > 0
            ? totalVelocity / validSampleCount
            : Vector3.zero;
    }

    private void ProcessFlight()
    {
        Vector3 currentPosition = transform.position;
        Vector3 delta = currentPosition - lastPosition;
        float distance = delta.magnitude;
        bool castHitFound = false;

        if (distance > 0.005f)
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                lastPosition,
                flightCastRadius,
                delta.normalized,
                distance,
                flightHitLayers,
                QueryTriggerInteraction.Collide);

            foreach (RaycastHit hit in hits)
            {
                if (!IsValidFlightHit(hit.collider))
                    continue;

                castHitFound = true;

                if (ProcessFlightHit(hit.collider, hit.point))
                    return;
            }
        }

        if (!castHitFound && !damageDealtThisThrow)
            ProcessFlightOverlap(currentPosition);

        ApplyFlightAlignment();
        lastPosition = currentPosition;
    }

    private void ProcessFlightOverlap(Vector3 position)
    {
        Collider[] overlaps = Physics.OverlapSphere(
            position,
            flightCastRadius,
            flightHitLayers,
            QueryTriggerInteraction.Collide);

        foreach (Collider collider in overlaps)
        {
            if (!IsValidFlightHit(collider))
                continue;

            Vector3 hitPoint = collider.ClosestPoint(position);

            if (ProcessFlightHit(collider, hitPoint))
                return;

            break;
        }
    }

    private bool ProcessFlightHit(Collider hitCollider, Vector3 hitPoint)
    {
        if (IsStickable(hitCollider) &&
            rb.linearVelocity.magnitude >= minVelocityToStick)
        {
            StickToCollider(hitCollider, hitPoint);
            return true;
        }

        if (damageDealtThisThrow || sweepDamage == null)
            return false;

        damageDealtThisThrow = true;
        sweepDamage.isFlying = false;

        Vector3 hitDirection = rb.linearVelocity.sqrMagnitude > 0.0001f
            ? rb.linearVelocity.normalized
            : transform.forward;

        sweepDamage.HandleThrowHitDirect(
            hitCollider,
            hitPoint,
            hitDirection,
            wasChargedAtRelease,
            chargeSystem);

        return false;
    }

    private bool IsValidFlightHit(Collider collider)
    {
        return collider != null &&
               !collider.transform.IsChildOf(transform);
    }

    private bool IsStickable(Collider collider)
    {
        return (stickableLayers.value & (1 << collider.gameObject.layer)) != 0;
    }

    private void ConfigureFlightPhysics()
    {
        rb.ResetCenterOfMass();

        if (flightDirectionRef != null)
        {
            Vector3 tipCenterOfMass = transform.InverseTransformPoint(
                flightDirectionRef.position);

            rb.centerOfMass = Vector3.Lerp(
                rb.centerOfMass,
                tipCenterOfMass,
                dartCenterOfMassOffset);
        }

        if (currentThrowStyle == ThrowStyle.Dart)
        {
            rb.useGravity = false;
            return;
        }

        rb.useGravity = true;
    }

    private void ApplyFlightAlignment()
    {
        if (rb.linearVelocity.sqrMagnitude <= 1f || flightDirectionRef == null)
            return;

        Vector3 flightDirection = rb.linearVelocity.normalized;

        if (currentThrowStyle == ThrowStyle.Dart)
        {
            rb.AddForce(
                Physics.gravity * dartGravityScale,
                ForceMode.Acceleration);

            Quaternion targetTipRotation = Quaternion.LookRotation(
                flightDirection,
                flightDirectionRef.up);

            Quaternion targetWeaponRotation =
                targetTipRotation *
                Quaternion.Inverse(flightDirectionRef.localRotation);

            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation,
                targetWeaponRotation,
                dartAlignmentSpeed * Time.fixedDeltaTime));

            rb.angularVelocity = Vector3.Lerp(
                rb.angularVelocity,
                Vector3.zero,
                15f * Time.fixedDeltaTime);

            return;
        }

        Vector3 rotationAxis = Vector3.Cross(
            flightDirectionRef.forward,
            flightDirection);

        if (rotationAxis.sqrMagnitude > 0.0001f)
        {
            float angle = Vector3.Angle(
                flightDirectionRef.forward,
                flightDirection);

            rb.AddTorque(
                rotationAxis.normalized *
                angle *
                chaoticAlignmentForce *
                Time.fixedDeltaTime);
        }

        rb.angularVelocity = Vector3.Lerp(
            rb.angularVelocity,
            Vector3.zero,
            2f * Time.fixedDeltaTime);
    }

    private void StickToCollider(Collider hitCollider, Vector3 hitPoint)
    {
        StopPendingThrow();

        isHeld = false;
        isThrown = false;
        isStuck = true;

        if (sweepDamage != null)
            sweepDamage.isFlying = false;

        Vector3 direction = flightDirectionRef != null
            ? flightDirectionRef.forward
            : transform.forward;

        transform.position = hitPoint + direction * penetrationDepth;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.ResetCenterOfMass();

        Vector3 worldScale = transform.lossyScale;

        transform.SetParent(hitCollider.transform, true);

        Vector3 parentScale = hitCollider.transform.lossyScale;
        transform.localScale = new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z));

        if (wasChargedAtRelease && chargeSystem != null)
            chargeSystem.ExpendCharge();
    }

    public void ForceUnstick()
    {
        if (!isStuck && transform.parent == null)
            return;

        StopPendingThrow();

        Vector3 worldScale = transform.lossyScale;

        transform.SetParent(null, true);
        transform.localScale = worldScale;

        isHeld = false;
        isThrown = false;
        isStuck = false;
        damageDealtThisThrow = false;
        wasChargedAtRelease = false;

        if (sweepDamage != null)
            sweepDamage.isFlying = false;

        ResetPhysicsForHeldWeapon();
    }

    private void ResetVelocitySamples()
    {
        velocitySampleIndex = 0;

        for (int i = 0; i < velocitySamples.Length; i++)
            velocitySamples[i] = Vector3.zero;
    }

    private void ResetPhysicsForHeldWeapon()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.ResetCenterOfMass();
    }

    private void StopPendingThrow()
    {
        if (pendingThrowRoutine == null)
            return;

        StopCoroutine(pendingThrowRoutine);
        pendingThrowRoutine = null;
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 0.0001f
            ? value / divisor
            : value;
    }
}