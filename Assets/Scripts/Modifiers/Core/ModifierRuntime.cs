using System;

/// <summary>
/// Laufzeit-Instanz eines ausgerüsteten Modifiers.
/// Kein MonoBehaviour: Jede Instanz besitzt einen expliziten,
/// kontrollierten Activate-/Deactivate-Lifecycle.
/// </summary>
public class ModifierRuntime
{
    public ModifierDefinition Definition { get; }
    public ModifierController Owner { get; private set; }
    public bool IsActive { get; private set; }

    public string ModifierId => Definition != null
        ? Definition.ModifierId
        : string.Empty;

    public ModifierRuntime(ModifierDefinition definition)
    {
        Definition = definition;
    }

    public void Activate(ModifierController owner)
    {
        if (IsActive)
            return;

        if (owner == null)
            throw new ArgumentNullException(nameof(owner));

        if (Definition == null)
            throw new InvalidOperationException(
                "ModifierRuntime cannot activate without a definition.");

        Owner = owner;
        IsActive = true;

        try
        {
            OnActivated();
        }
        catch
        {
            IsActive = false;
            Owner = null;
            throw;
        }
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        try
        {
            OnDeactivated();
        }
        finally
        {
            IsActive = false;
            Owner = null;
        }
    }

    /// <summary>
    /// Spezialisierte Runtimes abonnieren hier ihre Events.
    /// Beispiel später: WeaponCombatEvents.Hit += OnHit.
    /// </summary>
    protected virtual void OnActivated()
    {
    }

    /// <summary>
    /// Spezialisierte Runtimes melden hier garantiert alle Events ab.
    /// </summary>
    protected virtual void OnDeactivated()
    {
    }
}