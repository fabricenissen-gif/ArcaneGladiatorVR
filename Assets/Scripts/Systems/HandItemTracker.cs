using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HandItemTracker : MonoBehaviour
{
    public bool IsOccupied { get; private set; } = false;

    private XRBaseInteractor xrInteractor;

    private void Awake()
    {
        xrInteractor = GetComponent<XRBaseInteractor>();
        if (xrInteractor == null)
            Debug.LogWarning("[HandItemTracker] Kein XRBaseInteractor auf diesem GameObject gefunden!");
    }

    private void OnEnable()
    {
        if (xrInteractor == null) return;
        xrInteractor.selectEntered.AddListener(OnSelectEntered);
        xrInteractor.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        if (xrInteractor == null) return;
        xrInteractor.selectEntered.RemoveListener(OnSelectEntered);
        xrInteractor.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;

        GameObject grabbed = args.interactableObject.transform.gameObject;
        bool isWeapon = grabbed.GetComponentInParent<WeaponThrowAssist>() != null
                     || grabbed.GetComponentInParent<WeaponSweepDamage>() != null;

        if (!isWeapon)
        {
            Debug.Log($"[HandItemTracker] '{grabbed.name}' kein Weapon → Hand bleibt frei.");
            return;
        }

        IsOccupied = true;
        Debug.Log($"[HandItemTracker] Weapon gegriffen: '{grabbed.name}' → Hand besetzt.");
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;

        IsOccupied = false;
        Debug.Log($"[HandItemTracker] '{args.interactableObject.transform.name}' losgelassen → Hand frei.");
    }
}