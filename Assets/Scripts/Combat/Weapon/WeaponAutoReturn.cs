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

        // Coroutine sauber stoppen wenn das Objekt deaktiviert wird
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // XR feuert OnReleased auch während OnDisable — Guard verhindert den Crash
        if (!gameObject.activeInHierarchy) return;

        if (args.interactorObject is XRSocketInteractor) return;

        if (returnCoroutine != null)
            StopCoroutine(returnCoroutine);

        returnCoroutine = StartCoroutine(ReturnRoutine());
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }

    private IEnumerator ReturnRoutine()
    {
        yield return new WaitForSeconds(timeBeforeReturn);

        // Nochmal prüfen — Schwert könnte zwischenzeitlich zerstört worden sein
        if (this == null || !gameObject.activeInHierarchy) yield break;

        if (backSocket != null && !backSocket.hasSelection)
        {
            var throwAssist = GetComponent<WeaponThrowAssist>();
            if (throwAssist != null)
                throwAssist.ForceUnstick();

            transform.position = backSocket.transform.position;
            transform.rotation = backSocket.transform.rotation;

            var interactionManager = grabInteractable.interactionManager;
            interactionManager.SelectEnter(backSocket, (IXRSelectInteractable)grabInteractable);

            Debug.Log("[WeaponAutoReturn] Schwert ist zum Rücken zurückgekehrt.");
        }

        returnCoroutine = null;
    }
}