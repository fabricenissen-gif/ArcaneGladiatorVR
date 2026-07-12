using UnityEngine;

/// <summary>
/// Weapon-Modifier-Asset für Bloodletter.
///
/// Jeder bestätigte Nahkampftreffer wendet BLEED auf das noch lebende
/// Ziel an. Wurftreffer können optional ebenfalls BLEED anwenden.
///
/// Die Laufzeitlogik liegt ausschließlich in BloodletterModifierRuntime.
/// Dadurch ist der Effekt nur aktiv, solange dieses Asset durch einen
/// ModifierController ausgerüstet ist.
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
        "Wenn aktiv, wendet ein erfolgreicher Schwertwurf ebenfalls BLEED an. " +
        "Standardmäßig deaktiviert, damit Bloodletter zunächst nur ON_HIT nutzt.")]
    [SerializeField] private bool applyOnThrowHit;

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
/// Laufzeitlogik für Bloodletter.
///
/// Abonniert ausschließlich bestätigte Treffer-Events.
/// Kein eigener Collider, keine eigene Sweep-Erkennung und keine
/// eigene Schadenslogik.
/// </summary>
public sealed class BloodletterModifierRuntime : WeaponModifierRuntime
{
    private readonly BloodletterModifierDefinition definition;

    public BloodletterModifierRuntime(
        BloodletterModifierDefinition definition)
        : base(definition)
    {
        this.definition = definition;
    }

    protected override void OnWeaponModifierActivated()
    {
        CombatEvents.Hit += OnWeaponHit;

        if (definition.ApplyOnThrowHit)
            CombatEvents.ThrowHit += OnWeaponThrowHit;

        if (definition.LogApplications)
        {
            Debug.Log(
                $"[Bloodletter] Activated '{ModifierId}' | " +
                $"Duration:{definition.BleedDuration:F2}s | " +
                $"ThrowHit:{definition.ApplyOnThrowHit}");
        }
    }

    protected override void OnWeaponModifierDeactivated()
    {
        if (CombatEvents != null)
        {
            CombatEvents.Hit -= OnWeaponHit;
            CombatEvents.ThrowHit -= OnWeaponThrowHit;
        }

        if (definition.LogApplications)
            Debug.Log($"[Bloodletter] Deactivated '{ModifierId}'.");
    }

    private void OnWeaponHit(WeaponCombatEvents.EventData eventData)
    {
        ApplyBleed(eventData, "ON_HIT");
    }

    private void OnWeaponThrowHit(WeaponCombatEvents.EventData eventData)
    {
        ApplyBleed(eventData, "ON_THROW_HIT");
    }

    private void ApplyBleed(
        WeaponCombatEvents.EventData eventData,
        string triggerName)
    {
        Health targetHealth = eventData.TargetHealth;

        // Schutz für künftige Eventquellen, Despawns und tödliche Treffer.
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

        // Owner ist die aktive Waffeninstanz, zum Beispiel sword_A.
        // SourceId dokumentiert eindeutig Modifier und auslösenden Trigger.
        // Bei einem Stack-Refresh ist bewusst der zuletzt anwendende
        // Bloodletter-Proc als Quelle im TagInstance-Kontext gespeichert.
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