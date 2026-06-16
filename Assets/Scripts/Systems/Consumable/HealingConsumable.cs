using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class HealingConsumable : MonoBehaviour
{
    [Header("Consume Settings")]
    [Tooltip("Wie viel Leben stellt der Burger her?")]
    [SerializeField] private int healAmount = 25;
    [Tooltip("Abstand zum Kopf in Metern, ab dem gegessen wird")]
    [SerializeField] private float consumeDistance = 0.15f; 

    private Transform headTransform;
    private XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;

    private PlayerHealth playerHealth; 

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (Camera.main != null) headTransform = Camera.main.transform;
        
        // Findet dein Health-Skript automatisch in der Szene
        playerHealth = FindAnyObjectByType<PlayerHealth>();
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
    }

    private void OnGrabbed(SelectEnterEventArgs args) => isGrabbed = true;
    private void OnReleased(SelectExitEventArgs args) => isGrabbed = false;

    private void Update()
    {
        if (isGrabbed && headTransform != null)
        {
            float distance = Vector3.Distance(transform.position, headTransform.position);
            
            if (distance <= consumeDistance)
            {
                TryConsume();
            }
        }
    }

    private void TryConsume()
    {
        if (playerHealth == null) return;
        
        // Nur essen, wenn das aktuelle Leben kleiner als das maximale Leben ist
        if (playerHealth.CurrentHealth < playerHealth.MaxHealth)
        {
            playerHealth.Heal(healAmount);
            
            Debug.Log($"Mampf! Burger gegessen! Heilt um {healAmount} HP.");
            
            // Haptisches Feedback (leichtes Vibrieren beim Essen)
            if (grabInteractable.firstInteractorSelecting is XRBaseInputInteractor interactor)
            {
                interactor.SendHapticImpulse(0.5f, 0.2f);
            }

            Destroy(gameObject);
        }
        else
        {
            // Optional: Du könntest hier ein kleines Audio-Feedback einbauen "Ich bin satt!"
            // Damit er das nicht jeden Frame spamt, lassen wir die Logik einfach still scheitern.
        }
    }
}