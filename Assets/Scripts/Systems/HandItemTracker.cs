using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals; // ← korrigiert für XRI 3.x

public class HandItemTracker : MonoBehaviour
{
    public bool IsOccupied { get; private set; } = false;

    private readonly List<XRBaseInteractor> xrInteractors = new();

    [Header("UI Ray Visibility")]
    [Tooltip("Wird automatisch gesucht, kann aber auch manuell zugewiesen werden.")]
    [SerializeField] private XRInteractorLineVisual lineVisual;
    [SerializeField] private NearFarInteractor       nearFarInteractor;

    private bool wasOccupied;

    private void Awake()
    {
        GetComponentsInChildren(true, xrInteractors);

        if (xrInteractors.Count == 0)
            Debug.LogError($"[HandItemTracker] Kein XRBaseInteractor unter '{name}' gefunden! " +
                            "Near-Far/Poke Interactor Setup prüfen.");
        else
            Debug.Log($"[HandItemTracker] {xrInteractors.Count} Interactor(en) gefunden: " +
                      string.Join(", ", xrInteractors.ConvertAll(i => i.name)));

        if (lineVisual == null)
            lineVisual = GetComponentInChildren<XRInteractorLineVisual>(true);

        if (nearFarInteractor == null)
            nearFarInteractor = GetComponentInChildren<NearFarInteractor>(true);

        if (lineVisual == null)
            Debug.LogWarning($"[HandItemTracker] Kein XRInteractorLineVisual unter '{name}' gefunden — " +
                              "Strahl kann beim Waffe-Halten nicht ausgeblendet werden.");
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
            if (interactor == null) continue;
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
        RefreshRayVisibility();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor) return;

        IsOccupied = false;
        Debug.Log($"[HandItemTracker] '{args.interactableObject.transform.name}' losgelassen → Hand frei.");
        RefreshRayVisibility();
    }

    private void RefreshRayVisibility()
    {
        if (wasOccupied == IsOccupied) return;
        wasOccupied = IsOccupied;

        if (lineVisual != null)
            lineVisual.enabled = !IsOccupied;

        if (nearFarInteractor != null)
            nearFarInteractor.enableUIInteraction = !IsOccupied;
    }
}