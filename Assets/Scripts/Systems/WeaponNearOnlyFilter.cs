using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Blockiert Fern-Greifen (Far-Cast) für dieses Interactable.
/// Nur Near-Interaction (Sphere Caster / direktes Greifen in Reichweite) bleibt erlaubt.
/// Nutzt einen Distanz-Check statt eines internen XRI-State-Felds, da
/// NearFarInteractor.selectionRegion erst NACH erfolgter Selection einen
/// gültigen Wert liefert und daher in Process() (vor der Selection) nicht
/// zuverlässig nutzbar ist.
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public class WeaponNearOnlyFilter : MonoBehaviour, IXRSelectFilter
{
    [Tooltip("Maximale Distanz (Meter) zwischen Hand und Waffe, innerhalb derer Greifen erlaubt ist. " +
             "Sollte ungefähr dem Cast Radius des Sphere Interaction Casters entsprechen.")]
    [SerializeField] private float maxNearDistance = 0.3f;

    public bool canProcess => true;

    private XRBaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable == null)
        {
            Debug.LogError($"[WeaponNearOnlyFilter] Kein XRBaseInteractable auf '{name}' gefunden!");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        interactable?.selectFilters.Add(this);
    }

    private void OnDisable()
    {
        interactable?.selectFilters.Remove(this);
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactableTarget)
    {
        // Nur NearFarInteractor betrifft diese Regel, andere Interactor-Typen unberührt lassen.
        if (interactor is not NearFarInteractor nearFar)
            return true;

        // Fail-Case: falls kein Transform ermittelbar ist, sicherheitshalber erlauben
        // statt das Greifen fälschlich komplett zu blockieren.
        Transform interactorTransform = nearFar.transform;
        Transform targetTransform = interactableTarget.transform;
        if (interactorTransform == null || targetTransform == null)
            return true;

        float distance = Vector3.Distance(interactorTransform.position, targetTransform.position);
        return distance <= maxNearDistance;
    }
}