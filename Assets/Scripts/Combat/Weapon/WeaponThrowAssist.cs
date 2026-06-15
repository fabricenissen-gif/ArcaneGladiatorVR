using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponThrowAssist : MonoBehaviour
{
    [Header("Flight Assist")]
    [Tooltip("Soll sich das Schwert in Flugrichtung ausrichten?")]
    [SerializeField] private bool alignToFlightDirection = true;
    [Tooltip("Wie stark soll die Ausrichtung sein? (höher = dartpfeil-artiger)")]
    [SerializeField] private float flightAlignmentSpeed = 10f;
    [Tooltip("Die lokale Achse, die nach vorne zeigen soll (Z = Klinge bei normalem Setup)")]
    [SerializeField] private Vector3 bladeForwardAxis = Vector3.forward;

    [Header("Sticking (Steckenbleiben)")]
    [Tooltip("Welche Layer dürfen durchstochen werden? (Gegner, Boden, Wände)")]
    [SerializeField] private LayerMask stickableLayers;
    [Tooltip("Mindestgeschwindigkeit, damit das Schwert stecken bleibt")]
    [SerializeField] private float minVelocityToStick = 3f;
    
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
        // Prüfen, ob wir es WIRKLICH geworfen haben (nicht in einen Socket gelegt)
        if (!(args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))
        {
            if (rb.linearVelocity.magnitude > 0.5f) // Nur wenn wir es mit Schwung loslassen
            {
                isThrown = true;
                isStuck = false;
            }
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Wenn wir es wieder greifen (oder es in den Rücken-Socket ploppt), Reset
        isThrown = false;
        
        if (isStuck)
        {
            Unstick();
        }
    }

    private void FixedUpdate()
    {
        // Flug-Ausrichtung anwenden
        if (isThrown && !isStuck && alignToFlightDirection && rb.linearVelocity.sqrMagnitude > 1f)
        {
            Vector3 flightDir = rb.linearVelocity.normalized;
            // Wir drehen die konfigurierte bladeForwardAxis in Richtung der Flugrichtung
            Quaternion targetRotation = Quaternion.FromToRotation(bladeForwardAxis, flightDir);
            
            // Da das Schwert vielleicht eine eigene Grundrotation hat, müssen wir das weich mischen
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * flightAlignmentSpeed));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Wenn wir fliegen, nicht bereits stecken und stark genug geworfen wurden
        if (isThrown && !isStuck && rb.linearVelocity.magnitude >= minVelocityToStick)
        {
            // Prüfen ob das getroffene Objekt auf dem richtigen Layer ist
            if ((stickableLayers.value & (1 << collision.gameObject.layer)) > 0)
            {
                StickToSurface(collision);
            }
            else
            {
                // Wenn wir eine Wand treffen, die nicht stickable ist, prallen wir ab -> Flug beenden
                isThrown = false; 
            }
        }
    }

    private void StickToSurface(Collision collision)
    {
        isStuck = true;
        isThrown = false;

        // Schwert einfrieren
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Optional: An das getroffene Objekt "heften" (wenn es ein Gegner ist, der sich bewegt)
        // transform.SetParent(collision.transform, true);
        
        // ACHTUNG: Hier könntest du auch deinen WeaponSweepDamage aufrufen, 
        // um beim "Einschlag" noch einmal ordentlich Schaden zu verursachen!
        
        Debug.Log("Schwert ist stecken geblieben in: " + collision.gameObject.name);
    }

    private void Unstick()
    {
        isStuck = false;
        rb.isKinematic = false;
        
        // Falls wir es vorher als Parent an einen Gegner gehängt haben, 
        // müssen wir das Parent hier wieder null setzen:
        // transform.SetParent(null, true);
    }
}