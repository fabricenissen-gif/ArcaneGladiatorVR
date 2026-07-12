using UnityEngine;

/// <summary>
/// Weapon-Modifier-Asset für Venom Strike.
///
/// Jeder bestätigte Nahkampf- oder Wurftreffer appliziert POISON.
/// Weitere Treffer während der Laufzeit erhöhen die POISON-Stacks bis
/// zum zentralen Limit im TagHandler und refreshen die Tag-Dauer.
/// </summary>
[CreateAssetMenu(
    fileName = "VenomStrikeModifier",
    menuName = "SpellDelver/Modifiers/Weapon/Venom Strike")]
public sealed class VenomStrikeModifierDefinition : WeaponModifierDefinition
{
    [Header("Venom Strike")]
    [Tooltip("Dauer des applizierten oder refreshten POISON-Tags.")]
    [SerializeField, Min(0.05f)] private float poisonDuration = 5f;

    [Header("Trigger")]
    [Tooltip(
        "Wenn aktiv, appliziert ein bestätigter Wurftreffer ebenfalls POISON.")]
    [SerializeField] private bool applyOnThrowHit = true;

    [Header("Debug")]
    [Tooltip(
        "Während der Integration aktiv lassen. " +
        "Danach deaktivieren, damit die Console sauber bleibt.")]
    [SerializeField] private bool logApplications = true;

    public float PoisonDuration => poisonDuration;
    public bool ApplyOnThrowHit => applyOnThrowHit;
    public bool LogApplications => logApplications;

    protected override WeaponModifierRuntime CreateWeaponRuntime()
    {
        return new VenomStrikeModifierRuntime(this);
    }

    private void OnValidate()
    {
        poisonDuration = Mathf.Max(0.05f, poisonDuration);
    }
}

/// <summary>
/// Runtime für Venom Strike.
///
/// Verwendet den gemeinsamen TriggeredWeaponModifierRuntime-Pfad.
/// Die Stack-Regel liegt ausschließlich im TagHandler:
/// POISON stackt bis maxPoisonStacks und refresht pro Anwendung die Dauer.
/// </summary>
public sealed class VenomStrikeModifierRuntime
    : TriggeredWeaponModifierRuntime
{
    private readonly VenomStrikeModifierDefinition definition;

    public VenomStrikeModifierRuntime(
        VenomStrikeModifierDefinition definition)
        : base(definition)
    {
        this.definition = definition;
    }

    protected override bool ListenOnHit => true;

    protected override bool ListenOnThrowHit =>
        definition.ApplyOnThrowHit;

    protected override void OnWeaponTriggersActivated()
    {
        if (!definition.LogApplications)
            return;

        Debug.Log(
            $"[VenomStrike] Activated '{ModifierId}' | " +
            $"Duration:{definition.PoisonDuration:F2}s | " +
            $"ThrowHit:{definition.ApplyOnThrowHit}");
    }

    protected override void OnWeaponTriggersDeactivating()
    {
        if (definition.LogApplications)
            Debug.Log($"[VenomStrike] Deactivated '{ModifierId}'.");
    }

    protected override void OnHit(
        WeaponCombatEvents.EventData eventData)
    {
        ApplyPoison(eventData, "ON_HIT");
    }

    protected override void OnThrowHit(
        WeaponCombatEvents.EventData eventData)
    {
        ApplyPoison(eventData, "ON_THROW_HIT");
    }

    private void ApplyPoison(
        WeaponCombatEvents.EventData eventData,
        string triggerName)
    {
        Health targetHealth = eventData.TargetHealth;

        if (targetHealth == null || targetHealth.IsDead)
            return;

        TagHandler tagHandler = targetHealth.GetComponent<TagHandler>();

        if (tagHandler == null)
        {
            if (definition.LogApplications)
            {
                Debug.LogWarning(
                    $"[VenomStrike] {triggerName} | " +
                    $"Target '{targetHealth.name}' has no TagHandler. " +
                    "POISON was not applied.");
            }

            return;
        }

        TagApplicationContext context =
            TagApplicationContext.FromModifier(
                Owner,
                $"{ModifierId}:{triggerName}");

        tagHandler.ApplyTag(
            TagType.POISON,
            definition.PoisonDuration,
            context);

        if (!definition.LogApplications)
            return;

        TagInstance poisonTag = tagHandler.GetTag(TagType.POISON);

        int stackCount = poisonTag != null
            ? poisonTag.StackCount
            : 0;

        float remainingTime = poisonTag != null
            ? poisonTag.RemainingTime
            : 0f;

        string sourceText = poisonTag != null
            ? $"{poisonTag.SourceCategory}:{poisonTag.SourceId}"
            : "None";

        Debug.Log(
            $"[VenomStrike] {triggerName} | " +
            $"Target:{targetHealth.name} | " +
            $"POISON stacks:{stackCount} | " +
            $"Duration:{definition.PoisonDuration:F2}s | " +
            $"Remaining:{remainingTime:F2}s | " +
            $"Source:{sourceText} | " +
            $"Damage:{eventData.Damage:F1} | " +
            $"Speed:{eventData.Speed:F2} | " +
            $"Charged:{eventData.WasCharged} | " +
            $"Swing:{eventData.SwingId} | " +
            $"Frame:{eventData.Frame}");
    }
}