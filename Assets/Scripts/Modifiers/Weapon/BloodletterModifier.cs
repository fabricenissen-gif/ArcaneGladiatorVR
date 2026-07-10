using UnityEngine;

/// <summary>
/// Weapon modifier:
/// Jeder bestätigte Schwerttreffer wendet BLEED auf das getroffene,
/// noch lebende Ziel an.
///
/// Abonniert ausschließlich WeaponCombatEvents.Hit:
/// - Kein eigener Collider.
/// - Keine eigene Schadenserkennung.
/// - Keine Doppel-Procs durch Child-Collider oder Sweep-Samples,
///   weil WeaponSweepDamage pro Attack Window nur ein ON_HIT pro
///   Health-Komponente veröffentlicht.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(WeaponCombatEvents))]
public sealed class BloodletterModifier : MonoBehaviour
{
    [Header("Bloodletter")]
    [Tooltip("Dauer eines neu angewandten oder refreshten BLEED-Tags.")]
    [SerializeField, Min(0.05f)] private float bleedDuration = 4f;

    [Header("Optional")]
    [Tooltip(
        "Wenn aktiv, wendet ein erfolgreicher Schwertwurf ebenfalls BLEED an. " +
        "Standardmäßig deaktiviert, damit Bloodletter zunächst ausschließlich ON_HIT nutzt.")]
    [SerializeField] private bool applyOnThrowHit;

    [Header("Debug")]
    [Tooltip("Für den aktuellen Integrationstest aktiv lassen.")]
    [SerializeField] private bool logApplications = true;

    private WeaponCombatEvents combatEvents;

    private void Awake()
    {
        combatEvents = GetComponent<WeaponCombatEvents>();

        if (combatEvents == null)
        {
            Debug.LogError(
                $"[BloodletterModifier] '{name}' benötigt " +
                "WeaponCombatEvents, wurde aber nicht gefunden.");

            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (combatEvents == null)
            return;

        combatEvents.Hit += OnWeaponHit;

        if (applyOnThrowHit)
            combatEvents.ThrowHit += OnWeaponThrowHit;
    }

    private void OnDisable()
    {
        if (combatEvents == null)
            return;

        combatEvents.Hit -= OnWeaponHit;
        combatEvents.ThrowHit -= OnWeaponThrowHit;
    }

    private void OnWeaponHit(WeaponCombatEvents.EventData eventData)
    {
        ApplyBleed(eventData, "OnHit");
    }

    private void OnWeaponThrowHit(WeaponCombatEvents.EventData eventData)
    {
        ApplyBleed(eventData, "OnThrowHit");
    }

    private void ApplyBleed(
        WeaponCombatEvents.EventData eventData,
        string triggerName)
    {
        Health targetHealth = eventData.TargetHealth;

        // WeaponSweepDamage veröffentlicht zwar nur überlebende Ziele,
        // der Guard schützt aber gegen andere spätere Event-Quellen.
        if (targetHealth == null || targetHealth.IsDead)
            return;

        TagHandler tagHandler =
            targetHealth.GetComponent<TagHandler>();

        if (tagHandler == null)
        {
            if (logApplications)
            {
                Debug.LogWarning(
                    $"[BloodletterModifier] Kein TagHandler auf " +
                    $"'{targetHealth.name}'. BLEED wurde nicht angewandt.");
            }

            return;
        }

        TagApplicationContext context =
            TagApplicationContext.FromModifier(
                this,
                "Bloodletter");

        tagHandler.ApplyTag(
            TagType.BLEED,
            bleedDuration,
            context);

        if (logApplications)
        {
            TagInstance bleedTag = tagHandler.GetTag(TagType.BLEED);

            int stackCount = bleedTag != null
                ? bleedTag.StackCount
                : 0;

            Debug.Log(
                $"[BloodletterModifier] {triggerName} | " +
                $"Target:{targetHealth.name} | " +
                $"BLEED stacks:{stackCount} | " +
                $"duration:{bleedDuration:F2}s | " +
                $"swing:{eventData.SwingId}");
        }
    }

    private void OnValidate()
    {
        bleedDuration = Mathf.Max(0.05f, bleedDuration);
    }
}