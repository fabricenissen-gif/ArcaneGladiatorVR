using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

    [Header("General Flight Settings")]
    [Tooltip("Ein leeres Child-Objekt, dessen Z-Achse in Richtung der Schwertspitze zeigt")]
    [SerializeField] private Transform flightDirectionRef;

    [Header("Dart Style Settings")]
    [Tooltip("Wie schnell dreht sich das Schwert linear in Flugrichtung?")]
    [SerializeField] private float dartAlignmentSpeed = 20f;
    [Tooltip("Wie stark zieht die Schwerkraft beim Fliegen? (1 = normal, 0.5 = halbe Schwerkraft, gleitet weiter)")]
    [Range(0f, 2f)]
    [SerializeField] private float dartGravityScale = 0.6f;
    [Tooltip("Verschiebt den Schwerpunkt von der Spitze (1) in Richtung Griff (0)")]
    [Range(0f, 1f)]
    [SerializeField] private float dartCenterOfMassOffset = 0.4f;

    [Header("Chaotic Style Settings")]
    [Tooltip("Kraft, die das Schwert in Richtung drückt (führt zu Überkorrektur/Wobbeln)")]
    [SerializeField] private float chaoticAlignmentForce = 50f;

    [Header("Sticking (Steckenbleiben)")]
    [Tooltip("Welche Layer dürfen durchstochen werden?")]
    [SerializeField] private LayerMask stickableLayers;
    [Tooltip("Mindestgeschwindigkeit, damit das Schwert stecken bleibt")]
    [SerializeField] private float minVelocityToStick = 3f;
    [Tooltip("Wie tief (in Metern) soll das Schwert beim Einschlag ins Material rutschen?")]
    [SerializeField] private float penetrationDepth = 0.15f;
    
    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private bool isThrown = false;
    private bool isStuck = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
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
        if (!(args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))
        {
            if (rb.linearVelocity.magnitude > 0.5f)
            {
                isThrown = true;
                isStuck = false;
                
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
                    if(flightDirectionRef != null) rb.centerOfMass = transform.InverseTransformPoint(flightDirectionRef.position);
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
        
        if (isStuck)
        {
            ForceUnstick();
        }
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
            {
                StickToSurface(collision);
            }
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

        // BERECHNUNG ANGEPASST: Wir nutzen die Klingen-Ausrichtung statt der verfälschten Velocity
        Vector3 penetrationDirection = flightDirectionRef != null ? flightDirectionRef.forward : transform.forward;
        transform.position += penetrationDirection * penetrationDepth;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
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
    }
}