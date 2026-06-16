using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponAutoReturn : MonoBehaviour
{
    [Header("Return Settings")]
    [Tooltip("Der Socket, in den die Waffe zurückkehren soll")]
    [SerializeField] private XRSocketInteractor backSocket;
    [SerializeField] private float timeBeforeReturn = 5f;

    private XRGrabInteractable grabInteractable;
    private Coroutine returnCoroutine;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        // Den ersten Aufruf in Awake kannst du weglassen oder drinlassen. 
        // Besser ist es, ihn wegzulassen, damit das Schwert beim Start nicht direkt teleportiert wird, 
        // falls du es anders spawnen willst. Ich habe ihn hier mal entfernt für einen saubereren Start.
    }

    private void OnEnable()
    {
        // Wir hören zu, wenn das Schwert losgelassen oder gegriffen wird
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
        // Wenn wir es loslassen und es NICHT in einen Socket gesteckt wurde (z.B. auf den Boden geworfen)
        if (!(args.interactorObject is XRSocketInteractor))
        {
            if (returnCoroutine != null)
                StopCoroutine(returnCoroutine);

            returnCoroutine = StartCoroutine(ReturnRoutine());
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Wenn wir es greifen (egal ob von der Hand oder vom Socket), Timer abbrechen
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }

    private IEnumerator ReturnRoutine()
    {
        yield return new WaitForSeconds(timeBeforeReturn);

        // Prüfen, ob der Socket Platz hat
        if (backSocket != null && !backSocket.hasSelection)
        {
            // ---> NEU: Das Wurf-Skript resetten, damit die Physik wieder normal funktioniert <---
            var throwAssist = GetComponent<WeaponThrowAssist>();
            if (throwAssist != null) 
            {
                throwAssist.ForceUnstick();
            }

            // Position/Rotation in die Nähe des Sockets setzen (er fängt es dann ein)
            transform.position = backSocket.transform.position;
            transform.rotation = backSocket.transform.rotation;

            // InteractionManager anweisen, das Objekt in den Socket zu stecken
            var interactionManager = grabInteractable.interactionManager;
            interactionManager.SelectEnter(backSocket, (IXRSelectInteractable)grabInteractable);

            Debug.Log("Schwert ist zum Rücken zurückgekehrt.");
        }
    }
}