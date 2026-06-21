using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hört auf TagHandler.OnTagApplied und löst Reaktionen aus.
/// EXPOSED ist ein Zustandsmodifier — kein direkter Schaden.
/// ConsumeExposedModifier() wird von ALLEN Schadensquellen aufgerufen.
/// ExposedMultiplier ist lesbar damit alle Caller denselben Wert nutzen.
/// </summary>
[RequireComponent(typeof(TagHandler))]
[RequireComponent(typeof(Health))]
public class TagReactionSystem : MonoBehaviour
{
    [Header("Steam Explosion")]
    [SerializeField] private float steamExplosionDamage = 20f;
    [SerializeField] private float steamExplosionRadius = 3f;
    [SerializeField] private float steamExplosionKnockbackForce = 6f;

    [Header("Shatter")]
    [SerializeField] private float shatterDamage = 30f;
    [SerializeField] private float shatterStunDuration = 1.5f;

    [Header("Blizzard")]
    [SerializeField] private float blizzardRadius = 4f;
    [SerializeField] private float blizzardDuration = 3f;

    [Header("Arcane Ignite")]
    [SerializeField] private float arcaneIgniteDamage = 15f;

    [Header("Venom Burst")]
    [SerializeField] private float venomBurstDamagePerPoisonStack = 10f;

    [Header("Thunderstrike")]
    [SerializeField] private float thunderstrikeDamage = 18f;
    [SerializeField] private float thunderstrikeChainRange = 5f;

    [Header("Shock Bleed")]
    [SerializeField] private float shockBleedDamagePerBleedStack = 8f;

    [Header("Cauterize")]
    [SerializeField] private float cauterizeDamage = 22f;

    [Header("Exposed")]
    [Tooltip("Wie lange bleibt Exposed aktiv wenn kein Treffer kommt?")]
    [SerializeField] private float exposedDuration = 5f;
    [Tooltip("Schadensmultiplikator — wird von allen Schadensquellen via ExposedMultiplier gelesen")]
    [SerializeField] private float exposedDamageMultiplier = 2f;

    [Header("Toxic Fumes")]
    [SerializeField] private float toxicFumesDuration = 4f;
    [SerializeField] private float toxicFumesRadius = 3f;

    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayerMask = ~0;

    private TagHandler tagHandler;
    private Health health;

    private bool  isExposedActive;
    private float exposedTimer;

    // Öffentlich lesbar — für UI, VFX und Schadensquellen
    public bool  IsExposed         => isExposedActive;
    public float ExposedMultiplier => exposedDamageMultiplier;

    private void Awake()
    {
        tagHandler = GetComponent<TagHandler>();
        health     = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (tagHandler != null) tagHandler.OnTagApplied += OnTagApplied;
    }

    private void OnDisable()
    {
        if (tagHandler != null) tagHandler.OnTagApplied -= OnTagApplied;
    }

    private void Update()
    {
        TickExposed();
    }

    // ─────────────────────────────────────────
    // Public API für Schadensquellen
    // ─────────────────────────────────────────

    /// Verbraucht Exposed einmalig. Gibt true zurück wenn aktiv war.
    /// Caller: finalDamage *= reactions.ExposedMultiplier, dann TakeDamageReaction(..., true)
    public bool ConsumeExposedModifier()
    {
        if (!isExposedActive) return false;

        isExposedActive = false;
        exposedTimer    = 0f;
        Debug.Log($"[TagReaction] EXPOSED verbraucht auf {gameObject.name}");
        return true;
    }

    // ─────────────────────────────────────────
    // Tag Reactions
    // ─────────────────────────────────────────

    private void OnTagApplied(TagType newTag, TagInstance instance)
        => CheckReactions(newTag);

    private void CheckReactions(TagType t)
    {
        if (TryReactSteamExplosion(t)) return;
        if (TryReactShatter(t))        return;
        if (TryReactBlizzard(t))       return;
        if (TryReactArcaneIgnite(t))   return;
        if (TryReactVenomBurst(t))     return;
        if (TryReactThunderstrike(t))  return;
        if (TryReactShockBleed(t))     return;
        if (TryReactCauterize(t))      return;
        if (TryReactExposed(t))        return;
        if (TryReactToxicFumes(t))     return;
    }

    private bool TryReactSteamExplosion(TagType t)
    {
        if (!IsCombo(t, TagType.FIRE, TagType.FROST)) return false;
        tagHandler.RemoveTag(TagType.FIRE);
        tagHandler.RemoveTag(TagType.FROST);
        ReactionSteamExplosion();
        return true;
    }

    private bool TryReactShatter(TagType t)
    {
        if (!IsCombo(t, TagType.ELECTRIC, TagType.FROST)) return false;
        tagHandler.RemoveTag(TagType.ELECTRIC);
        tagHandler.RemoveTag(TagType.FROST);
        ReactionShatter();
        return true;
    }

    private bool TryReactBlizzard(TagType t)
    {
        if (!IsCombo(t, TagType.WIND, TagType.FROST)) return false;
        tagHandler.RemoveTag(TagType.WIND);
        tagHandler.RemoveTag(TagType.FROST);
        ReactionBlizzard();
        return true;
    }

    private bool TryReactArcaneIgnite(TagType t)
    {
        if (!IsCombo(t, TagType.ARCANE, TagType.FIRE)) return false;
        tagHandler.RemoveTag(TagType.ARCANE);
        tagHandler.RemoveTag(TagType.FIRE);
        ReactionArcaneIgnite();
        return true;
    }

    private bool TryReactVenomBurst(TagType t)
    {
        if (!IsCombo(t, TagType.ARCANE, TagType.POISON)) return false;
        TagInstance poisonTag = tagHandler.GetTag(TagType.POISON);
        int stacks = poisonTag != null ? poisonTag.StackCount : 1;
        tagHandler.RemoveTag(TagType.ARCANE);
        tagHandler.RemoveTag(TagType.POISON);
        ReactionVenomBurst(stacks);
        return true;
    }

    private bool TryReactThunderstrike(TagType t)
    {
        if (!IsCombo(t, TagType.ELECTRIC, TagType.WIND)) return false;
        tagHandler.RemoveTag(TagType.ELECTRIC);
        tagHandler.RemoveTag(TagType.WIND);
        ReactionThunderstrike();
        return true;
    }

    private bool TryReactShockBleed(TagType t)
    {
        if (!IsCombo(t, TagType.ELECTRIC, TagType.BLEED)) return false;
        TagInstance bleedTag = tagHandler.GetTag(TagType.BLEED);
        int stacks = bleedTag != null ? bleedTag.StackCount : 1;
        tagHandler.RemoveTag(TagType.ELECTRIC);
        tagHandler.RemoveTag(TagType.BLEED);
        ReactionShockBleed(stacks);
        return true;
    }

    private bool TryReactCauterize(TagType t)
    {
        if (!IsCombo(t, TagType.FIRE, TagType.BLEED)) return false;
        tagHandler.RemoveTag(TagType.FIRE);
        tagHandler.RemoveTag(TagType.BLEED);
        ReactionCauterize();
        return true;
    }

    private bool TryReactExposed(TagType t)
    {
        if (!IsCombo(t, TagType.MARKED, TagType.ARCANE)) return false;
        tagHandler.RemoveTag(TagType.MARKED);
        tagHandler.RemoveTag(TagType.ARCANE);
        ReactionExposed();
        return true;
    }

    private bool TryReactToxicFumes(TagType t)
    {
        if (!IsCombo(t, TagType.POISON, TagType.FIRE)) return false;
        tagHandler.RemoveTag(TagType.POISON);
        tagHandler.RemoveTag(TagType.FIRE);
        ReactionToxicFumes();
        return true;
    }

    // ─────────────────────────────────────────
    // Reaktionseffekte
    // ─────────────────────────────────────────

    private void ReactionSteamExplosion()
    {
        Debug.Log($"[TagReaction] STEAM EXPLOSION auf {gameObject.name}");
        Collider[]      hits    = Physics.OverlapSphere(transform.position, steamExplosionRadius, enemyLayerMask, QueryTriggerInteraction.Ignore);
        HashSet<Health> already = new HashSet<Health>();

        foreach (Collider col in hits)
        {
            Health h = col.GetComponentInParent<Health>();
            if (h == null || !already.Add(h)) continue;

            Vector3 dir = h.transform.position - transform.position;
            if (dir.sqrMagnitude <= 0.0001f) dir = Vector3.up;
            else dir.Normalize();

            h.TakeDamage(steamExplosionDamage, dir);

            Rigidbody rb = h.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
                rb.AddForce((dir + Vector3.up * 0.35f).normalized * steamExplosionKnockbackForce, ForceMode.Impulse);
        }
    }

    private void ReactionShatter()
    {
        Debug.Log($"[TagReaction] SHATTER auf {gameObject.name} | Stun:{shatterStunDuration}s TODO");
        health.TakeDamage(shatterDamage, Vector3.up);
        // TODO: Stun-Effekt implementieren
    }

    private void ReactionBlizzard()
    {
        Debug.Log($"[TagReaction] BLIZZARD auf {gameObject.name} | Slow TODO r:{blizzardRadius} d:{blizzardDuration}s");
        // TODO: Slow auf alle Gegner im Radius
    }

    private void ReactionArcaneIgnite()
    {
        Debug.Log($"[TagReaction] ARCANE IGNITE auf {gameObject.name} | DoT TODO");
        health.TakeDamage(arcaneIgniteDamage, Vector3.zero);
        // TODO: DoT-Effekt
    }

    private void ReactionVenomBurst(int stacks)
    {
        float dmg = venomBurstDamagePerPoisonStack * stacks;
        Debug.Log($"[TagReaction] VENOM BURST auf {gameObject.name} | stacks:{stacks} dmg:{dmg}");
        health.TakeDamage(dmg, Vector3.zero);
    }

    private void ReactionThunderstrike()
    {
        Debug.Log($"[TagReaction] THUNDERSTRIKE auf {gameObject.name}");
        health.TakeDamage(thunderstrikeDamage, Vector3.up);

        Collider[] hits          = Physics.OverlapSphere(transform.position, thunderstrikeChainRange, enemyLayerMask, QueryTriggerInteraction.Ignore);
        float      closestDist   = float.MaxValue;
        Health     closestHealth = null;
        Vector3    closestPos    = Vector3.zero;

        foreach (Collider col in hits)
        {
            Health h = col.GetComponentInParent<Health>();
            if (h == null || h == health) continue;
            float d = Vector3.Distance(transform.position, col.transform.position);
            if (d < closestDist) { closestDist = d; closestHealth = h; closestPos = col.transform.position; }
        }

        if (closestHealth != null)
        {
            Vector3 dir = (closestPos - transform.position).normalized;
            closestHealth.TakeDamage(thunderstrikeDamage, dir);
            Debug.Log($"[TagReaction] THUNDERSTRIKE chain → {closestHealth.gameObject.name}");
        }
    }

    private void ReactionShockBleed(int stacks)
    {
        float dmg = shockBleedDamagePerBleedStack * stacks;
        Debug.Log($"[TagReaction] SHOCK BLEED auf {gameObject.name} | stacks:{stacks} dmg:{dmg}");
        health.TakeDamage(dmg, Vector3.up);
    }

    private void ReactionCauterize()
    {
        Debug.Log($"[TagReaction] CAUTERIZE auf {gameObject.name}");
        health.TakeDamage(cauterizeDamage, Vector3.forward);
    }

    private void ReactionExposed()
    {
        isExposedActive = true;
        exposedTimer    = exposedDuration;
        Debug.Log($"[TagReaction] EXPOSED aktiv auf {gameObject.name} | {exposedDuration}s | x{exposedDamageMultiplier}");
    }

    private void ReactionToxicFumes()
    {
        Debug.Log($"[TagReaction] TOXIC FUMES auf {gameObject.name} | Cloud TODO r:{toxicFumesRadius} d:{toxicFumesDuration}s");
        // TODO: Gift-Wolke spawnen
    }

    // ─────────────────────────────────────────
    // Helper
    // ─────────────────────────────────────────

    private bool IsCombo(TagType trigger, TagType a, TagType b)
    {
        if (trigger != a && trigger != b) return false;
        return tagHandler.HasTag(a) && tagHandler.HasTag(b);
    }

    private void TickExposed()
    {
        if (!isExposedActive) return;
        exposedTimer -= Time.deltaTime;
        if (exposedTimer <= 0f)
        {
            isExposedActive = false;
            exposedTimer    = 0f;
            Debug.Log($"[TagReaction] EXPOSED abgelaufen auf {gameObject.name}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;  Gizmos.DrawWireSphere(transform.position, steamExplosionRadius);
        Gizmos.color = Color.cyan;   Gizmos.DrawWireSphere(transform.position, blizzardRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, thunderstrikeChainRange);
        Gizmos.color = Color.green;  Gizmos.DrawWireSphere(transform.position, toxicFumesRadius);
    }
}