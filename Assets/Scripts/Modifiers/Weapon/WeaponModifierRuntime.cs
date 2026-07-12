using UnityEngine;

/// <summary>
/// Runtime-Basis für Weapon-Modifier.
///
/// Abonniert keine Events selbst. Konkrete Modifier entscheiden
/// bewusst, welche Trigger sie benötigen:
/// ON_SWING, ON_HIT, ON_THROW_RELEASE oder ON_THROW_HIT.
/// </summary>
public abstract class WeaponModifierRuntime : ModifierRuntime
{
    protected WeaponCombatEvents CombatEvents { get; private set; }

    protected WeaponModifierRuntime(
        WeaponModifierDefinition definition)
        : base(definition)
    {
    }

    protected sealed override void OnActivated()
    {
        CombatEvents = Owner != null
            ? Owner.GetComponent<WeaponCombatEvents>()
            : null;

        if (CombatEvents == null)
        {
            throw new MissingComponentException(
                $"Weapon modifier '{ModifierId}' requires " +
                $"{nameof(WeaponCombatEvents)} on the same GameObject " +
                "as its ModifierController.");
        }

        OnWeaponModifierActivated();
    }

    protected sealed override void OnDeactivated()
    {
        try
        {
            OnWeaponModifierDeactivated();
        }
        finally
        {
            CombatEvents = null;
        }
    }

    /// <summary>
    /// Events hier abonnieren.
    /// Beispiel:
    /// CombatEvents.Hit += OnWeaponHit;
    /// </summary>
    protected virtual void OnWeaponModifierActivated()
    {
    }

    /// <summary>
    /// Hier alle Events wieder abmelden.
    /// Dieser Schritt ist verpflichtend für jeden Weapon-Modifier.
    /// </summary>
    protected virtual void OnWeaponModifierDeactivated()
    {
    }
}