using UnityEngine;

/// <summary>
/// Weapon-Modifier-Asset für Scorch Touch.
///
/// Jeder bestätigte Nahkampf- oder Wurftreffer appliziert FIRE.
/// Weitere Treffer während FIRE aktiv ist refreshen nur dessen Dauer;
/// FIRE stackt im aktuellen TagHandler nicht.
/// </summary>
[CreateAssetMenu(
    fileName = "ScorchTouchModifier",
    menuName = "SpellDelver/Modifiers/Weapon/Scorch Touch")]
public sealed class ScorchTouchModifierDefinition : WeaponModifierDefinition
{
    [Header("Scorch Touch")]
    [Tooltip("Dauer des applizierten oder refreshten FIRE-Tags.")]
    [SerializeField, Min(0.05f)] private float fireDuration = 4f;

    [Header("Trigger")]
    [Tooltip(
        "Wenn aktiv, appliziert ein bestätigter Wurftreffer ebenfalls FIRE.")]
    [SerializeField] private bool applyOnThrowHit = true;

    [Header("Debug")]
    [Tooltip(
        "Während der Integration aktiv lassen. " +
        "Danach deaktivieren, damit die Console sauber bleibt.")]
    [SerializeField] private bool logApplications = true;

    public float FireDuration => fireDuration;
    public bool ApplyOnThrowHit => applyOnThrowHit;
    public bool LogApplications => logApplications;

    protected override WeaponModifierRuntime CreateWeaponRuntime()
    {
        return new ScorchTouchModifierRuntime(this);
    }

    private void OnValidate()
    {
        fireDuration = Mathf.Max(0.05f, fireDuration);
    }
}

/// <summary>
/// Runtime für Scorch Touch.
///
/// Nutzt nur die zentrale WeaponCombatEvents-Pipeline. Dadurch existiert
/// keine eigene Treffererkennung und keine Doppel-Auslösung durch
/// Child-Collider oder Sweep-Samples.
/// </summary>
public sealed class ScorchTouchModifierRuntime
    : TriggeredWeaponModifierRuntime
{
    private readonly ScorchTouchModifierDefinition definition;

    public ScorchTouchModifierRuntime(
        ScorchTouchModifierDefinition definition)
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
            $"[ScorchTouch] Activated '{ModifierId}' | " +
            $"Duration:{definition.FireDuration:F2}s | " +
            $"ThrowHit:{definition.ApplyOnThrowHit}");
    }

    protected override void OnWeaponTriggersDeactivating()
    {
        if (definition.LogApplications)
            Debug.Log($"[ScorchTouch] Deactivated '{ModifierId}'.");
    }

    protected override void OnHit(
        WeaponCombatEvents.EventData eventData)
    {
        ApplyFire(eventData, "ON_HIT");
    }

    protected override void OnThrowHit(
        WeaponCombatEvents.EventData eventData)
    {
        ApplyFire(eventData, "ON_THROW_HIT");
    }

    private void ApplyFire(
        WeaponCombatEvents.EventData eventData,
        string triggerName)
    {
        Health targetHealth = eventData.TargetHealth;

        if (targetHealth == null || targetHealth.IsDead)
            return;

        TagHandler tagHandler =
            targetHealth.GetComponent<TagHandler>();

        if (tagHandler == null)
        {
            if (definition.LogApplications)
            {
                Debug.LogWarning(
                    $"[ScorchTouch] {triggerName} | " +
                    $"Target '{targetHealth.name}' has no TagHandler. " +
                    "FIRE was not applied.");
            }

            return;
        }

        TagApplicationContext context =
            TagApplicationContext.FromModifier(
                Owner,
                $"{ModifierId}:{triggerName}");

        tagHandler.ApplyTag(
            TagType.FIRE,
            definition.FireDuration,
            context);

        if (!definition.LogApplications)
            return;

        TagInstance fireTag = tagHandler.GetTag(TagType.FIRE);

        if (fireTag == null)
        {
            Debug.Log(
                $"[ScorchTouch] {triggerName} | " +
                $"Target:{targetHealth.name} | " +
                "FIRE applied and consumed immediately by a reaction | " +
                $"Duration:{definition.FireDuration:F2}s | " +
                $"Damage:{eventData.Damage:F1} | " +
                $"Speed:{eventData.Speed:F2} | " +
                $"Charged:{eventData.WasCharged} | " +
                $"Swing:{eventData.SwingId} | " +
                $"Frame:{eventData.Frame}");

            return;
        }

        Debug.Log(
            $"[ScorchTouch] {triggerName} | " +
            $"Target:{targetHealth.name} | " +
            $"FIRE stacks:{fireTag.StackCount} | " +
            $"Duration:{definition.FireDuration:F2}s | " +
            $"Remaining:{fireTag.RemainingTime:F2}s | " +
            $"Source:{fireTag.SourceCategory}:{fireTag.SourceId} | " +
            $"Damage:{eventData.Damage:F1} | " +
            $"Speed:{eventData.Speed:F2} | " +
            $"Charged:{eventData.WasCharged} | " +
            $"Swing:{eventData.SwingId} | " +
            $"Frame:{eventData.Frame}");
    }
}