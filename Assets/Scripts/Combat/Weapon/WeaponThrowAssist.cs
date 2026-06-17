using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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
    [Tooltip("Referenz zum Charge System (optional)")]
    [SerializeField] private WeaponChargeSystem chargeSystem;
    [Tooltip("Wie viel schneller fliegt das Schwert, wenn es MAX CHARGED ist?")]
    [SerializeField] private float chargedThrowSpeedMultiplier = 3.0f;

    [Header("General Flight Settings")]
    [Tooltip("Ein leeres Child-Objekt, dessen Z-Achse in Richtung der Schwertspitze zeigt")]
    [SerializeField] private Transform flightDirectionRef;
    [Tooltip("Gibt dem Schwert beim Loslassen einen Extra-Schubs")]
    [SerializeField] private float throwSpeedMultiplier = 1.3f;

    [Header("Dart Style Settings")]
    [SerializeField] private float dartAlignmentSpeed = 20f;
    [Range(0f, 2f)]
    [SerializeField] private float dartGravityScale = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float dartCenterOfMassOffset = 0.4f;

    [Header("Chaotic Style Settings")]
    [SerializeField] private float chaoticAlignmentForce = 50f;

    [Header("Sticking (Steckenbleiben)")]
    [SerializeField] private LayerMask stickableLayers;
    [SerializeField] private float minVelocityToStick = 3f;
    [SerializeField] private float penetrationDepth = 0.15f;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private bool isThrown = false;
    private bool isStuck = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (chargeSystem == null) chargeSystem = GetComponent<WeaponChargeSystem>();
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

    private void OnReleased(SelectExitEventArgs args)
    {
        if (!(args.interactorObject is XRSocketInteractor))
        {
            if (rb.linearVelocity.magnitude > 0.5f)
            {
                isThrown = true;
                isStuck = false;

                float currentMultiplier = throwSpeedMultiplier;
                if (chargeSystem != null && chargeSystem.IsCharged)
                {
                    currentMultiplier = chargedThrowSpeedMultiplier;
                    Debug.Log("CHARGED THROW!");
                }

                rb.linearVelocity *= currentMultiplier;

                if (currentThrowStyle == ThrowStyle.Dart)
                {
                    if (flightDirectionRef != null)
                    {
                        Vector3 originalCOM = rb.centerOfMass;
                        Vector3 spikeCOM = transform.InverseTransformPoint(flightDirectionRef.position);
                        rb.centerOfMass = Vector3.Lerp(originalCOM, spikeCOM, dartCenterOfMassOffset);
                    }
                    rb.angularVelocity *= 0.1f;
                    rb.useGravity = false;
                }
                else if (currentThrowStyle == ThrowStyle.Chaotic)
                {
                    if (flightDirectionRef != null) rb.centerOfMass = transform.InverseTransformPoint(flightDirectionRef.position);
                    rb.useGravity = true;
                }
            }
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isThrown = false;
        rb.ResetCenterOfMass();
        rb.useGravity = true;

        if (isStuck) ForceUnstick();
    }

    private void FixedUpdate()
    {
        if (isThrown && !isStuck && flightDirectionRef != null && rb.linearVelocity.sqrMagnitude > 1f)
        {
            Vector3 flightDir = rb.linearVelocity.normalized;
            Vector3 currentSpikeDir = flightDirectionRef.forward;

            if (currentThrowStyle == ThrowStyle.Dart)
            {
                rb.AddForce(Physics.gravity * dartGravityScale, ForceMode.Acceleration);

                Quaternion targetRotForRef = Quaternion.LookRotation(flightDir, flightDirectionRef.up);
                Quaternion targetRotationOfParent = targetRotForRef * Quaternion.Inverse(flightDirectionRef.localRotation);

                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotationOfParent, dartAlignmentSpeed * Time.fixedDeltaTime));
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 15f);
            }
            else if (currentThrowStyle == ThrowStyle.Chaotic)
            {
                Vector3 rotationAxis = Vector3.Cross(currentSpikeDir, flightDir);
                float angle = Vector3.Angle(currentSpikeDir, flightDir);

                rb.AddTorque(rotationAxis.normalized * (angle * chaoticAlignmentForce * Time.fixedDeltaTime));
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isThrown && !isStuck && rb.linearVelocity.magnitude >= minVelocityToStick)
        {
            if ((stickableLayers.value & (1 << collision.gameObject.layer)) > 0)
                StickToSurface(collision);
            else
            {
                isThrown = false;
                rb.ResetCenterOfMass();
                rb.useGravity = true;
            }
        }
    }

    private void StickToSurface(Collision collision)
    {
        isStuck = true;
        isThrown = false;

        rb.ResetCenterOfMass();
        rb.useGravity = true;

        Vector3 penetrationDirection = flightDirectionRef != null ? flightDirectionRef.forward : transform.forward;
        transform.position += penetrationDirection * penetrationDepth;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.SetParent(collision.transform, true);

        if (chargeSystem != null) chargeSystem.ExpendCharge();
    }

    public void ForceUnstick()
    {
        isStuck = false;
        isThrown = false;
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.ResetCenterOfMass();
            rb.useGravity = true;
        }
        transform.SetParent(null, true);
    }
}