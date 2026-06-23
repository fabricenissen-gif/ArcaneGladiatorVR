using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponThrowAssist : MonoBehaviour
{
    public enum ThrowStyle { Dart, Chaotic }

    [Header("Throw Style Settings")]
    public ThrowStyle currentThrowStyle = ThrowStyle.Dart;

    [Header("Charge Integration")]
    [SerializeField] private WeaponChargeSystem chargeSystem;
    [SerializeField] private float chargedThrowSpeedMultiplier = 3.0f;

    [Header("General Flight Settings")]
    [SerializeField] private Transform flightDirectionRef;
    [SerializeField] private float throwSpeedMultiplier = 1.3f;

    [Header("Dart Style Settings")]
    [SerializeField] private float dartAlignmentSpeed = 20f;
    [Range(0f, 2f)]
    [SerializeField] private float dartGravityScale = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float dartCenterOfMassOffset = 0.4f;

    [Header("Chaotic Style Settings")]
    [SerializeField] private float chaoticAlignmentForce = 50f;

    [Header("Sticking")]
    [SerializeField] private LayerMask stickableLayers;
    [SerializeField] private float minVelocityToStick = 3f;
    [SerializeField] private float penetrationDepth = 0.15f;

    [Header("Hit Detection (Flug)")]
    [SerializeField] private float flightCastRadius = 0.12f;
    [SerializeField] private LayerMask flightHitLayers;

    private Rigidbody          rb;
    private XRGrabInteractable grabInteractable;
    private WeaponSweepDamage  sweepDamage;

    private bool    isThrown             = false;
    private bool    isStuck              = false;
    private Vector3 lastPosition;
    private bool    damageDealtThisThrow = false;

    // Charge-Status beim Release einfrieren
    private bool wasChargedAtRelease = false;

    private Vector3[] velocitySamples   = new Vector3[5];
    private int       velocitySampleIdx = 0;
    private bool      isHeld            = false;

    public bool IsThrown => isThrown;

    // ── Lifecycle ─────────────────────────────────────────────

    private void Awake()
    {
        rb               = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        sweepDamage      = GetComponent<WeaponSweepDamage>();
        if (chargeSystem == null)
            chargeSystem = GetComponent<WeaponChargeSystem>();

        Debug.Log($"[WeaponThrowAssist] Awake — sweepDamage:{sweepDamage != null} " +
                  $"chargeSystem:{chargeSystem != null}");
    }

    private void OnEnable()
    {
        grabInteractable.selectExited.AddListener(OnReleased);
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        grabInteractable.selectExited.RemoveListener(OnReleased);
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    // ── FixedUpdate: Sampling + Flug ──────────────────────────

    private void FixedUpdate()
    {
        if (isHeld)
        {
            velocitySamples[velocitySampleIdx % velocitySamples.Length] = rb.linearVelocity;
            velocitySampleIdx++;
            return;
        }

        if (!isThrown || isStuck) return;

        Vector3 currentPos = transform.position;
        Vector3 delta      = currentPos - lastPosition;
        float   distance   = delta.magnitude;

        bool hitFound = false;

        // SphereCast für Bewegungs-Delta
        if (distance > 0.005f)
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                lastPosition, flightCastRadius,
                delta.normalized, distance,
                flightHitLayers, QueryTriggerInteraction.Collide);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform.IsChildOf(transform)) continue;

                hitFound = true;
                if (ProcessFlightHit(hit)) return;
            }
        }

        // OverlapSphere als Fallback — fängt "starts inside collider" ab
        if (!hitFound && !damageDealtThisThrow)
        {
            Collider[] overlaps = Physics.OverlapSphere(
                currentPos, flightCastRadius,
                flightHitLayers, QueryTriggerInteraction.Collide);

            foreach (Collider col in overlaps)
            {
                if (col == null) continue;
                if (col.transform.IsChildOf(transform)) continue;

                Debug.Log($"[WeaponThrowAssist] OverlapFallback Hit — " +
                          $"obj:'{col.gameObject.name}' " +
                          $"layer:{LayerMask.LayerToName(col.gameObject.layer)}");

                RaycastHit fakeHit = new RaycastHit();
                // ClosestPoint als Trefferpunkt
                Vector3 closest = col.ClosestPoint(currentPos);

                int  layer       = col.gameObject.layer;
                bool isStickable = (stickableLayers.value & (1 << layer)) > 0;

                if (isStickable && rb.linearVelocity.magnitude >= minVelocityToStick)
                {
                    // Für Overlap-Stick: manuellen Hit bauen
                    StickToCollider(col, closest);
                    return;
                }

                if (!damageDealtThisThrow && sweepDamage != null)
                {
                    damageDealtThisThrow = true;
                    sweepDamage.isFlying = false;
                    sweepDamage.HandleThrowHitDirect(col, closest,
                        rb.linearVelocity.normalized, wasChargedAtRelease, chargeSystem);
                }
                break;
            }
        }

        // Flug-Alignment
        if (rb.linearVelocity.sqrMagnitude > 1f && flightDirectionRef != null)
        {
            Vector3 flightDir       = rb.linearVelocity.normalized;
            Vector3 currentSpikeDir = flightDirectionRef.forward;

            if (currentThrowStyle == ThrowStyle.Dart)
            {
                rb.AddForce(Physics.gravity * dartGravityScale, ForceMode.Acceleration);
                Quaternion targetRotForRef        = Quaternion.LookRotation(flightDir, flightDirectionRef.up);
                Quaternion targetRotationOfParent = targetRotForRef * Quaternion.Inverse(flightDirectionRef.localRotation);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotationOfParent,
                    dartAlignmentSpeed * Time.fixedDeltaTime));
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero,
                    Time.fixedDeltaTime * 15f);
            }
            else if (currentThrowStyle == ThrowStyle.Chaotic)
            {
                Vector3 rotAxis = Vector3.Cross(currentSpikeDir, flightDir);
                float   angle   = Vector3.Angle(currentSpikeDir, flightDir);
                rb.AddTorque(rotAxis.normalized * (angle * chaoticAlignmentForce * Time.fixedDeltaTime));
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero,
                    Time.fixedDeltaTime * 2f);
            }
        }

        lastPosition = currentPos;
    }

    // Gibt true zurück wenn gesteckt (Loop abbrechen)
    private bool ProcessFlightHit(RaycastHit hit)
    {
        int  layer       = hit.collider.gameObject.layer;
        bool isStickable = (stickableLayers.value & (1 << layer)) > 0;

        Debug.Log($"[WeaponThrowAssist] FlightCast Hit — " +
                  $"obj:'{hit.collider.gameObject.name}' " +
                  $"layer:{LayerMask.LayerToName(layer)}({layer}) " +
                  $"stickable:{isStickable} " +
                  $"vel:{rb.linearVelocity.magnitude:F2} " +
                  $"wasCharged:{wasChargedAtRelease}");

        if (isStickable && rb.linearVelocity.magnitude >= minVelocityToStick)
        {
            StickToPoint(hit);
            return true;
        }

        if (!damageDealtThisThrow && sweepDamage != null)
        {
            damageDealtThisThrow = true;
            sweepDamage.isFlying = false;
            sweepDamage.HandleThrowHit(hit, wasChargedAtRelease, chargeSystem);
        }
        return false;
    }

    // ── XR Events ─────────────────────────────────────────────

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log("[WeaponThrowAssist] OnGrabbed.");
        isHeld               = true;
        isThrown             = false;
        isStuck              = false;
        damageDealtThisThrow = false;
        wasChargedAtRelease  = false;
        velocitySampleIdx    = 0;
        for (int i = 0; i < velocitySamples.Length; i++) velocitySamples[i] = Vector3.zero;
        if (sweepDamage != null) sweepDamage.isFlying = false;
        ResetPhysics();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;

        isHeld = false;

        // Charge-Status JETZT einfrieren — bevor ExpendCharge ihn löscht
        wasChargedAtRelease = chargeSystem != null && chargeSystem.IsCharged;

        // isFlying SOFORT — bevor SwingDetector feuert
        if (sweepDamage != null) sweepDamage.isFlying = true;

        Vector3 avgVelocity = Vector3.zero;
        int     validCount  = 0;
        foreach (Vector3 s in velocitySamples)
        {
            if (s.sqrMagnitude > 0.001f) { avgVelocity += s; validCount++; }
        }
        if (validCount > 0) avgVelocity /= validCount;

        float speed = avgVelocity.magnitude;
        Debug.Log($"[WeaponThrowAssist] OnReleased — speed:{speed:F2} " +
                  $"wasCharged:{wasChargedAtRelease} rb.isKinematic:{rb.isKinematic}");

        if (speed > 0.5f)
            StartCoroutine(BeginThrowAfterXRI(avgVelocity));
        else
        {
            if (sweepDamage != null) sweepDamage.isFlying = false;
            Debug.Log($"[WeaponThrowAssist] Zu langsam ({speed:F2}) — kein Throw.");
        }
    }

    private IEnumerator BeginThrowAfterXRI(Vector3 releaseVelocity)
    {
        int maxWait = 10;
        while (rb.isKinematic && maxWait-- > 0)
        {
            Debug.Log($"[WeaponThrowAssist] Warte auf isKinematic=false... ({maxWait})");
            yield return null;
        }

        if (rb.isKinematic)
        {
            Debug.LogWarning("[WeaponThrowAssist] isKinematic manuell erzwungen!");
            rb.isKinematic = false;
            yield return null;
        }

        float multiplier = wasChargedAtRelease ? chargedThrowSpeedMultiplier : throwSpeedMultiplier;
        Debug.Log($"[WeaponThrowAssist] Throw gestartet — " +
                  $"multiplier:{multiplier} wasCharged:{wasChargedAtRelease}");

        rb.linearVelocity    = releaseVelocity * multiplier;
        isThrown             = true;
        isStuck              = false;
        damageDealtThisThrow = false;
        lastPosition         = transform.position;
        if (sweepDamage != null) sweepDamage.isFlying = true;

        Debug.Log($"[WeaponThrowAssist] velocity:{rb.linearVelocity} " +
                  $"speed:{rb.linearVelocity.magnitude:F2}");

        if (currentThrowStyle == ThrowStyle.Dart)
        {
            if (flightDirectionRef != null)
            {
                Vector3 originalCOM = rb.centerOfMass;
                Vector3 spikeCOM    = transform.InverseTransformPoint(flightDirectionRef.position);
                rb.centerOfMass     = Vector3.Lerp(originalCOM, spikeCOM, dartCenterOfMassOffset);
            }
            rb.angularVelocity = Vector3.zero;
            rb.useGravity      = false;
        }
        else if (currentThrowStyle == ThrowStyle.Chaotic)
        {
            if (flightDirectionRef != null)
                rb.centerOfMass = transform.InverseTransformPoint(flightDirectionRef.position);
            rb.useGravity = true;
        }
    }

    // ── Stick ─────────────────────────────────────────────────

    private void StickToPoint(RaycastHit hit)
    {
        StickToCollider(hit.collider, hit.point);
    }

    private void StickToCollider(Collider col, Vector3 hitPoint)
    {
        isStuck  = true;
        isThrown = false;
        if (sweepDamage != null) sweepDamage.isFlying = false;

        Vector3 dir = flightDirectionRef != null ? flightDirectionRef.forward : transform.forward;
        transform.position = hitPoint + dir * penetrationDepth;

        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic     = true;
        rb.ResetCenterOfMass();

        Vector3 worldScale = transform.lossyScale;
        transform.SetParent(col.transform, true);
        Vector3 ps = col.transform.lossyScale;
        transform.localScale = new Vector3(
            worldScale.x / ps.x,
            worldScale.y / ps.y,
            worldScale.z / ps.z);

        Debug.Log($"[WeaponThrowAssist] Stuck to '{col.gameObject.name}'");

        if (chargeSystem != null) chargeSystem.ExpendCharge();
    }

    public void ForceUnstick()
    {
        if (!isStuck && transform.parent == null) return;

        Vector3 worldScale = transform.lossyScale;
        transform.SetParent(null, true);
        transform.localScale = worldScale;

        isStuck  = false;
        isThrown = false;
        if (sweepDamage != null) sweepDamage.isFlying = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            ResetPhysics();
        }

        Debug.Log("[WeaponThrowAssist] ForceUnstick.");
    }

    // ── Helpers ───────────────────────────────────────────────

    private void ResetPhysics()
    {
        rb.useGravity  = true;
        rb.isKinematic = false;
        rb.ResetCenterOfMass();
    }
}