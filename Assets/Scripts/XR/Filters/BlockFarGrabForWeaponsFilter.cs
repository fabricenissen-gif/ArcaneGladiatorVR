using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[CreateAssetMenu(
    fileName = "BlockFarGrabForWeaponsFilter",
    menuName = "SpellDelver/XR/Block Far Grab For Weapons Filter")]
public class BlockFarGrabForWeaponsFilter : ScriptableObject, IXRSelectFilter
{
    [Header("Weapon Detection")]
    [Tooltip("Ein XRGrabInteractable mit mindestens einer dieser XRI Interaction Layers " +
             "gilt als Waffe und darf nicht per Far-Caster selektiert werden.")]
    [SerializeField] private InteractionLayerMask weaponInteractionLayer;

    [Header("Component Detection")]
    [Tooltip("Zusätzliche Absicherung: Ein Objekt mit WeaponThrowAssist gilt immer als Waffe.")]
    [SerializeField] private bool detectWeaponThrowAssist = true;

    public bool canProcess => true;

    public bool Process(
        IXRSelectInteractor interactor,
        IXRSelectInteractable interactable)
    {
        // Dieses Filter darf ausschließlich Near-Far Interactors beeinflussen.
        if (interactor is not NearFarInteractor nearFarInteractor)
            return true;

        // Der konkrete Enum-Wert liegt in der bindable Variable .Value.
        // Near-Grab ist für Waffen gültig. Nur Far-Grab wird blockiert.
        if (nearFarInteractor.selectionRegion.Value != NearFarInteractor.Region.Far)
            return true;

        bool hasWeaponLayer =
            (interactable.interactionLayers.value & weaponInteractionLayer.value) != 0;

        bool hasWeaponThrowAssist =
            detectWeaponThrowAssist &&
            interactable.transform.GetComponentInParent<WeaponThrowAssist>() != null;

        bool isWeapon = hasWeaponLayer || hasWeaponThrowAssist;

        // Map, UI, Loot und alle anderen Interactables werden unverändert erlaubt.
        // Ausschließlich Waffen können nicht durch den Far-Caster selektiert werden.
        return !isWeapon;
    }
}