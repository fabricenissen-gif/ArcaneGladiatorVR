using UnityEngine;

/// <summary>
/// Reiner Entwicklungsmodifier.
///
/// Verändert keinen Schaden und wendet keine Tags an.
/// Er beweist nur, dass Weapon-Events sauber bei Modifier-Runtimes
/// ankommen und beim Unequip wieder gestoppt werden.
/// </summary>
[CreateAssetMenu(
    fileName = "WeaponModifierProbe",
    menuName = "SpellDelver/Modifiers/Weapon Modifier Probe")]
public sealed class WeaponModifierProbeDefinition : WeaponModifierDefinition
{
    [Header("Probe Settings")]
    [SerializeField] private bool listenOnSwing = true;
    [SerializeField] private bool listenOnHit = true;
    [SerializeField] private bool listenOnThrowHit = true;

    public bool ListenOnSwing => listenOnSwing;
    public bool ListenOnHit => listenOnHit;
    public bool ListenOnThrowHit => listenOnThrowHit;

    protected override WeaponModifierRuntime CreateWeaponRuntime()
    {
        return new WeaponModifierProbeRuntime(this);
    }
}

/// <summary>
/// Test-Runtime für die bereits funktionierenden Weapon-Events:
/// ON_SWING, ON_HIT und ON_THROW_HIT.
///
/// ON_THROW_RELEASE wird bewusst erst im nächsten Schritt ergänzt,
/// nachdem WeaponThrowAssist das Event ausschließlich beim wirklich
/// bestätigten Wurfstart publiziert.
/// </summary>
public sealed class WeaponModifierProbeRuntime : WeaponModifierRuntime
{
    private readonly WeaponModifierProbeDefinition definition;

    public WeaponModifierProbeRuntime(
        WeaponModifierProbeDefinition definition)
        : base(definition)
    {
        this.definition = definition;
    }

    protected override void OnWeaponModifierActivated()
    {
        if (definition.ListenOnSwing)
            CombatEvents.Swing += OnSwing;

        if (definition.ListenOnHit)
            CombatEvents.Hit += OnHit;

        if (definition.ListenOnThrowHit)
            CombatEvents.ThrowHit += OnThrowHit;

        Debug.Log(
            $"[WeaponModifierProbe] Activated '{ModifierId}' | " +
            $"Swing:{definition.ListenOnSwing} | " +
            $"Hit:{definition.ListenOnHit} | " +
            $"ThrowHit:{definition.ListenOnThrowHit}");
    }

    protected override void OnWeaponModifierDeactivated()
    {
        if (CombatEvents != null)
        {
            CombatEvents.Swing -= OnSwing;
            CombatEvents.Hit -= OnHit;
            CombatEvents.ThrowHit -= OnThrowHit;
        }

        Debug.Log(
            $"[WeaponModifierProbe] Deactivated '{ModifierId}'.");
    }

    private void OnSwing(WeaponCombatEvents.EventData eventData)
    {
        string sourceName = eventData.Source != null
            ? eventData.Source.name
            : "None";

        Debug.Log(
            $"[WeaponModifierProbe] ON_SWING | " +
            $"Source:{sourceName} | " +
            $"Speed:{eventData.Speed:F2} | " +
            $"Charged:{eventData.WasCharged} | " +
            $"Swing:{eventData.SwingId} | " +
            $"Frame:{eventData.Frame}");
    }

    private void OnHit(WeaponCombatEvents.EventData eventData)
    {
        string targetName = eventData.TargetHealth != null
            ? eventData.TargetHealth.name
            : "None";

        Debug.Log(
            $"[WeaponModifierProbe] ON_HIT | " +
            $"Target:{targetName} | " +
            $"Damage:{eventData.Damage:F1} | " +
            $"Speed:{eventData.Speed:F2} | " +
            $"Charged:{eventData.WasCharged} | " +
            $"Swing:{eventData.SwingId} | " +
            $"Frame:{eventData.Frame}");
    }

    private void OnThrowHit(WeaponCombatEvents.EventData eventData)
    {
        string targetName = eventData.TargetHealth != null
            ? eventData.TargetHealth.name
            : "None";

        Debug.Log(
            $"[WeaponModifierProbe] ON_THROW_HIT | " +
            $"Target:{targetName} | " +
            $"Damage:{eventData.Damage:F1} | " +
            $"Speed:{eventData.Speed:F2} | " +
            $"Charged:{eventData.WasCharged} | " +
            $"Frame:{eventData.Frame}");
    }
}