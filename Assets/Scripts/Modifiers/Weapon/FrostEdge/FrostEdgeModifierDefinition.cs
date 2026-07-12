using UnityEngine;

/// <summary>
/// Weapon-Modifier-Asset für Frost Edge.
///
/// Jeder bestätigte Nahkampf- oder Wurftreffer appliziert oder refresht
/// FROST auf dem getroffenen, noch lebenden Ziel.
/// </summary>
[CreateAssetMenu(
    fileName = "FrostEdgeModifier",
    menuName = "SpellDelver/Modifiers/Weapon/Frost Edge")]
public sealed class FrostEdgeModifierDefinition : WeaponModifierDefinition
{
    [Header("Frost Edge")]
    [Tooltip("Dauer des applizierten oder refreshten FROST-Tags.")]
    [SerializeField, Min(0.05f)] private float frostDuration = 3f;

    [Header("Trigger")]
    [Tooltip(
        "Wenn aktiv, appliziert ein bestätigter Wurftreffer ebenfalls FROST.")]
    [SerializeField] private bool applyOnThrowHit = true;

    [Header("Debug")]
    [Tooltip(
        "Nur während der Integration aktiv lassen. " +
        "Danach deaktivieren, damit die Console sauber bleibt.")]
    [SerializeField] private bool logApplications = true;

    public float FrostDuration => frostDuration;
    public bool ApplyOnThrowHit => applyOnThrowHit;
    public bool LogApplications => logApplications;

    protected override WeaponModifierRuntime CreateWeaponRuntime()
    {
        return new FrostEdgeModifierRuntime(this);
    }

    private void OnValidate()
    {
        frostDuration = Mathf.Max(0.05f, frostDuration);
    }
}

/// <summary>
/// Runtime für Frost Edge.
///
/// Reagiert auf bestätigte ON_HIT-Events und optional auf
/// ON_THROW_HIT-Events. Die zentrale TagHandler-API kontrolliert
/// das Refresh-Verhalten und löst bestehende Tag-Reaktionen aus.
/// </summary>
public sealed class FrostEdgeModifierRuntime
    : TriggeredWeaponModifierRuntime
{
    private readonly FrostEdgeModifierDefinition definition;

    public FrostEdgeModifierRuntime(
        FrostEdgeModifierDefinition definition)
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
            $"[FrostEdge] Activated '{ModifierId}' | " +
            $"Duration:{definition.FrostDuration:F2}s | " +
            $"ThrowHit:{definition.ApplyOnThrowHit}");
    }

    protected override void OnWeaponTriggersDeactivating()
    {
        if (definition.LogApplications)
            Debug.Log($"[FrostEdge] Deactivated '{ModifierId}'.");
    }

    protected override void OnHit(
        WeaponCombatEvents.EventData eventData)
    {
        ApplyFrost(eventData, "ON_HIT");
    }

    protected override void OnThrowHit(
        WeaponCombatEvents.EventData eventData)
    {
        ApplyFrost(eventData, "ON_THROW_HIT");
    }

    private void ApplyFrost(
        WeaponCombatEvents.EventData eventData,
        string triggerName)
    {
        Health targetHealth = eventData.TargetHealth;

        // Schutz gegen zerstörte, ungültige oder durch den Basistreffer
        // bereits getötete Gegner.
        if (targetHealth == null || targetHealth.IsDead)
            return;

        TagHandler tagHandler = targetHealth.GetComponent<TagHandler>();

        if (tagHandler == null)
        {
            if (definition.LogApplications)
            {
                Debug.LogWarning(
                    $"[FrostEdge] {triggerName} | " +
                    $"Target '{targetHealth.name}' has no TagHandler. " +
                    "FROST was not applied.");
            }

            return;
        }

        TagApplicationContext context =
            TagApplicationContext.FromModifier(
                Owner,
                $"{ModifierId}:{triggerName}");

        tagHandler.ApplyTag(
            TagType.FROST,
            definition.FrostDuration,
            context);

        if (!definition.LogApplications)
            return;

        TagInstance frostTag = tagHandler.GetTag(TagType.FROST);

        float remainingTime = frostTag != null
            ? frostTag.RemainingTime
            : 0f;

        string sourceText = frostTag != null
            ? $"{frostTag.SourceCategory}:{frostTag.SourceId}"
            : "None";

        Debug.Log(
            $"[FrostEdge] {triggerName} | " +
            $"Target:{targetHealth.name} | " +
            $"Duration:{definition.FrostDuration:F2}s | " +
            $"Remaining:{remainingTime:F2}s | " +
            $"Source:{sourceText} | " +
            $"Damage:{eventData.Damage:F1} | " +
            $"Speed:{eventData.Speed:F2} | " +
            $"Charged:{eventData.WasCharged} | " +
            $"Swing:{eventData.SwingId} | " +
            $"Frame:{eventData.Frame}");
    }
}