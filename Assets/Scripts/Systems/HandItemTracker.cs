using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HandItemTracker : MonoBehaviour
{
    public bool IsOccupied { get; private set; } = false;

    private readonly List<XRBaseInteractor> xrInteractors = new();

    private void Awake()
    {
        // Root trägt selbst evtl. keinen Interactor — Near-Far und Poke sitzen als Children.
        // Alle einsammeln, damit egal welcher Interactor greift, IsOccupied korrekt gesetzt wird.
        GetComponentsInChildren(true, xrInteractors);

        if (xrInteractors.Count == 0)
            Debug.LogError($"[HandItemTracker] Kein XRBaseInteractor unter '{name}' gefunden! " +
                            "Near-Far/Poke Interactor Setup prüfen.");
        else
            Debug.Log($"[HandItemTracker] {xrInteractors.Count} Interactor(en) gefunden: " +
                      string.Join(", ", xrInteractors.ConvertAll(i => i.name)));
    }

    private void OnEnable()
    {
        foreach (var interactor in xrInteractors)
        {
            interactor.selectEntered.AddListener(OnSelectEntered);
            interactor.selectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDisable()
    {
        foreach (var interactor in xrInteractors)
        {
            if (interactor == null) continue; // Fail-Case: zerstört während Playmode-Wechsel
            interactor.selectEntered.RemoveListener(OnSelectEntered);
            interactor.selectExited.RemoveListener(OnSelectExited);
        }
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