using UnityEngine;

/// <summary>
/// Weapon-Modifier-Asset für Bloodletter.
///
/// Jeder bestätigte Nahkampftreffer wendet BLEED auf das noch lebende
/// Ziel an. Wurftreffer können optional ebenfalls BLEED anwenden.
/// </summary>
[CreateAssetMenu(
    fileName = "BloodletterModifier",
    menuName = "SpellDelver/Modifiers/Weapon/Bloodletter")]
public sealed class BloodletterModifierDefinition : WeaponModifierDefinition
{
    [Header("Bloodletter")]
    [Tooltip("Dauer eines neu angewandten oder refreshten BLEED-Tags.")]
    [SerializeField, Min(0.05f)] private float bleedDuration = 4f;

    [Header("Trigger")]
    [Tooltip(
        "Wenn aktiv, wendet ein erfolgreicher Schwertwurf ebenfalls " +
        "BLEED an.")]
    [SerializeField] private bool applyOnThrowHit = true;

    [Header("Debug")]
    [Tooltip(
        "Nur während der Integration aktiv lassen. " +
        "Danach deaktivieren, damit die Console sauber bleibt.")]
    [SerializeField] private bool logApplications = true;

    public float BleedDuration => bleedDuration;
    public bool ApplyOnThrowHit => applyOnThrowHit;
    public bool LogApplications => logApplications;

    protected override WeaponModifierRuntime CreateWeaponRuntime()
    {
        return new BloodletterModifierRuntime(this);
    }

    private void OnValidate()
    {
        bleedDuration = Mathf.Max(0.05f, bleedDuration);
    }
}

/// <summary>
/// Runtime für Bloodletter.
///
/// Verwendet TriggeredWeaponModifierRuntime, daher keine manuelle
/// Event-Subscription- oder Unsubscription-Logik mehr in diesem Modifier.
/// </summary>
public sealed class BloodletterModifierRuntime
    : TriggeredWeaponModifierRuntime
{
    private readonly BloodletterModifierDefinition definition;

    public BloodletterModifierRuntime(
        BloodletterModifierDefinition definition)
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
            $"[Bloodletter] Activated '{ModifierId}' | " +
            $"Duration:{definition.BleedDuration:F2}s | " +
            $"ThrowHit:{definition.ApplyOnThrowHit}");
    }

    protected override void OnWeaponTriggersDeactivating()
    {
        if (definition.LogApplications)
            Debug.Log($"[Bloodletter] Deactivated '{ModifierId}'.");
    }

    protected override void OnHit(
        WeaponCombatEvents.EventData eventData)
    {
        ApplyBleed(eventData, "ON_HIT");
    }

    protected override void OnThrowHit(
        WeaponCombatEvents.EventData eventData)
    {
        ApplyBleed(eventData, "ON_THROW_HIT");
    }

    private void ApplyBleed(
        WeaponCombatEvents.EventData eventData,
        string triggerName)
    {
        Health targetHealth = eventData.TargetHealth;

        // Verhindert Statusanwendungen auf zerstörten, ungültigen
        // oder durch den Basis-Hit bereits getöteten Gegnern.
        if (targetHealth == null || targetHealth.IsDead)
            return;

        TagHandler tagHandler = targetHealth.GetComponent<TagHandler>();

        if (tagHandler == null)
        {
            if (definition.LogApplications)
            {
                Debug.LogWarning(
                    $"[Bloodletter] {triggerName} | " +
                    $"Target '{targetHealth.name}' has no TagHandler. " +
                    "BLEED was not applied.");
            }

            return;
        }

        TagApplicationContext context =
            TagApplicationContext.FromModifier(
                Owner,
                $"{ModifierId}:{triggerName}");

        tagHandler.ApplyTag(
            TagType.BLEED,
            definition.BleedDuration,
            context);

        if (!definition.LogApplications)
            return;

        TagInstance bleedTag = tagHandler.GetTag(TagType.BLEED);

        int stackCount = bleedTag != null
            ? bleedTag.StackCount
            : 0;

        string sourceText = bleedTag != null
            ? $"{bleedTag.SourceCategory}:{bleedTag.SourceId}"
            : "None";

        Debug.Log(
            $"[Bloodletter] {triggerName} | " +
            $"Target:{targetHealth.name} | " +
            $"BLEED stacks:{stackCount} | " +
            $"Duration:{definition.BleedDuration:F2}s | " +
            $"Source:{sourceText} | " +
            $"Damage:{eventData.Damage:F1} | " +
            $"Speed:{eventData.Speed:F2} | " +
            $"Charged:{eventData.WasCharged} | " +
            $"Swing:{eventData.SwingId} | " +
            $"Frame:{eventData.Frame}");
    }
}