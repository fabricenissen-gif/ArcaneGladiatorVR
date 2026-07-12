/// <summary>
/// Gemeinsame Runtime-Basis für Weapon-Modifier mit Combat-Triggern.
///
/// Ein abgeleiteter Modifier aktiviert ausschließlich die benötigten
/// Trigger über die ListenOn... Properties und überschreibt die
/// passenden On...-Methoden.
///
/// Die Subscriptions werden zentral und symmetrisch verwaltet.
/// Dadurch können keine Event-Listener vergessen werden, wenn ein
/// Modifier über ModifierController deaktiviert oder unequipped wird.
/// </summary>
public abstract class TriggeredWeaponModifierRuntime : WeaponModifierRuntime
{
    protected TriggeredWeaponModifierRuntime(
        WeaponModifierDefinition definition)
        : base(definition)
    {
    }

    /// <summary>
    /// Aktiviert den ON_SWING-Listener.
    /// </summary>
    protected virtual bool ListenOnSwing => false;

    /// <summary>
    /// Aktiviert den ON_HIT-Listener.
    /// </summary>
    protected virtual bool ListenOnHit => false;

    /// <summary>
    /// Aktiviert den ON_THROW_RELEASE-Listener.
    /// </summary>
    protected virtual bool ListenOnThrowRelease => false;

    /// <summary>
    /// Aktiviert den ON_THROW_HIT-Listener.
    /// </summary>
    protected virtual bool ListenOnThrowHit => false;

    /// <summary>
    /// Wird nach dem Abonnieren aller aktivierten Trigger aufgerufen.
    /// Eignet sich für Runtime-Setup oder Debug-Logs.
    /// </summary>
    protected virtual void OnWeaponTriggersActivated()
    {
    }

    /// <summary>
    /// Wird vor dem Abmelden aller Trigger aufgerufen.
    /// Eignet sich für Cleanup oder Debug-Logs.
    /// </summary>
    protected virtual void OnWeaponTriggersDeactivating()
    {
    }

    protected sealed override void OnWeaponModifierActivated()
    {
        if (ListenOnSwing)
            CombatEvents.Swing += HandleSwing;

        if (ListenOnHit)
            CombatEvents.Hit += HandleHit;

        if (ListenOnThrowRelease)
            CombatEvents.ThrowReleased += HandleThrowRelease;

        if (ListenOnThrowHit)
            CombatEvents.ThrowHit += HandleThrowHit;

        OnWeaponTriggersActivated();
    }

    protected sealed override void OnWeaponModifierDeactivated()
    {
        try
        {
            OnWeaponTriggersDeactivating();
        }
        finally
        {
            // Kein return in finally: C# verbietet, den finally-Block
            // vorzeitig zu verlassen. CombatEvents kann beim Shutdown
            // oder bei unvollständiger Aktivierung null sein.
            if (CombatEvents != null)
            {
                CombatEvents.Swing -= HandleSwing;
                CombatEvents.Hit -= HandleHit;
                CombatEvents.ThrowReleased -= HandleThrowRelease;
                CombatEvents.ThrowHit -= HandleThrowHit;
            }
        }
    }

    private void HandleSwing(WeaponCombatEvents.EventData eventData)
    {
        OnSwing(eventData);
    }

    private void HandleHit(WeaponCombatEvents.EventData eventData)
    {
        OnHit(eventData);
    }

    private void HandleThrowRelease(
        WeaponCombatEvents.EventData eventData)
    {
        OnThrowRelease(eventData);
    }

    private void HandleThrowHit(
        WeaponCombatEvents.EventData eventData)
    {
        OnThrowHit(eventData);
    }

    protected virtual void OnSwing(
        WeaponCombatEvents.EventData eventData)
    {
    }

    protected virtual void OnHit(
        WeaponCombatEvents.EventData eventData)
    {
    }

    protected virtual void OnThrowRelease(
        WeaponCombatEvents.EventData eventData)
    {
    }

    protected virtual void OnThrowHit(
        WeaponCombatEvents.EventData eventData)
    {
    }
}