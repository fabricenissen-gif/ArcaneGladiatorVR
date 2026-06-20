using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private float exposedDuration = 5f;

    [Header("Toxic Fumes")]
    [SerializeField] private float toxicFumesDuration = 4f;
    [SerializeField] private float toxicFumesRadius = 3f;

    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayerMask = ~0;

    private TagHandler tagHandler;
    private Health health;

    private bool isExposedActive;
    private float exposedTimer;

    private void Awake()
    {
        tagHandler = GetComponent<TagHandler>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (tagHandler != null)
            tagHandler.OnTagApplied += OnTagApplied;
    }

    private void OnDisable()
    {
        if (tagHandler != null)
            tagHandler.OnTagApplied -= OnTagApplied;
    }

    private void Update()
    {
        UpdateExposedTimer();
    }

    private void OnTagApplied(TagType newTag, TagInstance instance)
    {
        CheckReactions(newTag);
    }

    private void CheckReactions(TagType triggerTag)
    {
        if (TryReactSteamExplosion(triggerTag)) return;
        if (TryReactShatter(triggerTag)) return;
        if (TryReactBlizzard(triggerTag)) return;
        if (TryReactArcaneIgnite(triggerTag)) return;
        if (TryReactVenomBurst(triggerTag)) return;
        if (TryReactThunderstrike(triggerTag)) return;
        if (TryReactShockBleed(triggerTag)) return;
        if (TryReactCauterize(triggerTag)) return;
        if (TryReactExposed(triggerTag)) return;
        if (TryReactToxicFumes(triggerTag)) return;
    }

    // ─────────────────────────────────────────
    // Reaktionen
    // ─────────────────────────────────────────

    private bool TryReactSteamExplosion(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.FIRE, TagType.FROST))
            return false;

        tagHandler.RemoveTag(TagType.FIRE);
        tagHandler.RemoveTag(TagType.FROST);

        ReactionSteamExplosion();
        return true;
    }

    private bool TryReactShatter(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ELECTRIC, TagType.FROST))
            return false;

        tagHandler.RemoveTag(TagType.ELECTRIC);
        tagHandler.RemoveTag(TagType.FROST);

        ReactionShatter();
        return true;
    }

    private bool TryReactBlizzard(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.WIND, TagType.FROST))
            return false;

        tagHandler.RemoveTag(TagType.WIND);
        tagHandler.RemoveTag(TagType.FROST);

        ReactionBlizzard();
        return true;
    }

    private bool TryReactArcaneIgnite(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ARCANE, TagType.FIRE))
            return false;

        tagHandler.RemoveTag(TagType.ARCANE);
        tagHandler.RemoveTag(TagType.FIRE);

        ReactionArcaneIgnite();
        return true;
    }

    private bool TryReactVenomBurst(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ARCANE, TagType.POISON))
            return false;

        TagInstance poisonTag = tagHandler.GetTag(TagType.POISON);
        int poisonStacks = poisonTag != null ? poisonTag.StackCount : 1;

        tagHandler.RemoveTag(TagType.ARCANE);
        tagHandler.RemoveTag(TagType.POISON);

        ReactionVenomBurst(poisonStacks);
        return true;
    }

    private bool TryReactThunderstrike(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ELECTRIC, TagType.WIND))
            return false;

        tagHandler.RemoveTag(TagType.ELECTRIC);
        tagHandler.RemoveTag(TagType.WIND);

        ReactionThunderstrike();
        return true;
    }

    private bool TryReactShockBleed(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ELECTRIC, TagType.BLEED))
            return false;

        TagInstance bleedTag = tagHandler.GetTag(TagType.BLEED);
        int bleedStacks = bleedTag != null ? bleedTag.StackCount : 1;

        tagHandler.RemoveTag(TagType.ELECTRIC);
        tagHandler.RemoveTag(TagType.BLEED);

        ReactionShockBleed(bleedStacks);
        return true;
    }

    private bool TryReactCauterize(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.FIRE, TagType.BLEED))
            return false;

        tagHandler.RemoveTag(TagType.FIRE);
        tagHandler.RemoveTag(TagType.BLEED);

        ReactionCauterize();
        return true;
    }

    private bool TryReactExposed(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.MARKED, TagType.ARCANE))
            return false;

        tagHandler.RemoveTag(TagType.MARKED);
        tagHandler.RemoveTag(TagType.ARCANE);

        ReactionExposed();
        return true;
    }

    private bool TryReactToxicFumes(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.POISON, TagType.FIRE))
            return false;

        tagHandler.RemoveTag(TagType.POISON);
        tagHandler.RemoveTag(TagType.FIRE);

        ReactionToxicFumes();
        return true;
    }

    // ─────────────────────────────────────────
    // Konkrete Effekte
    // ─────────────────────────────────────────

   private void ReactionSteamExplosion()
{
    Debug.Log("[TagReaction] STEAM EXPLOSION auf " + gameObject.name);

    Collider[] hits = Physics.OverlapSphere(
        transform.position,
        steamExplosionRadius,
        enemyLayerMask,
        QueryTriggerInteraction.Ignore
    );

    HashSet<Health> alreadyHit = new HashSet<Health>();

    for (int i = 0; i < hits.Length; i++)
    {
        Health targetHealth = hits[i].GetComponentInParent<Health>();
        if (targetHealth == null) continue;
        if (!alreadyHit.Add(targetHealth)) continue; // Dedupe

        Vector3 origin = transform.position;
        Vector3 targetPos = targetHealth.transform.position;
        Vector3 direction = (targetPos - origin);

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.up;
        else
            direction.Normalize();

        targetHealth.TakeDamage(steamExplosionDamage, direction);

        // Knockback
        Rigidbody targetRb = targetHealth.GetComponent<Rigidbody>();
        if (targetRb != null && !targetRb.isKinematic)
        {
            Vector3 forceDir = (direction + Vector3.up * 0.35f).normalized;
            targetRb.AddForce(forceDir * steamExplosionKnockbackForce, ForceMode.Impulse);
        }

        Debug.Log("[TagReaction] STEAM EXPLOSION traf: " + targetHealth.gameObject.name + " | DMG: " + steamExplosionDamage);
    }
}

    private void ReactionShatter()
    {
        Debug.Log("[TagReaction] SHATTER auf " + gameObject.name + " | Stun TODO | Dauer: " + shatterStunDuration);
        health.TakeDamage(shatterDamage, Vector3.up);
    }

    private void ReactionBlizzard()
    {
        Debug.Log("[TagReaction] BLIZZARD auf " + gameObject.name + " | Slow TODO | Radius: " + blizzardRadius + " | Dauer: " + blizzardDuration);
    }

    private void ReactionArcaneIgnite()
    {
        Debug.Log("[TagReaction] ARCANE IGNITE auf " + gameObject.name + " | DoT TODO");
        health.TakeDamage(arcaneIgniteDamage, Vector3.zero);
    }

    private void ReactionVenomBurst(int poisonStacks)
    {
        float totalDamage = venomBurstDamagePerPoisonStack * poisonStacks;
        Debug.Log("[TagReaction] VENOM BURST auf " + gameObject.name + " | Stacks: " + poisonStacks + " | DMG: " + totalDamage);
        health.TakeDamage(totalDamage, Vector3.zero);
    }

    private void ReactionThunderstrike()
    {
        Debug.Log("[TagReaction] THUNDERSTRIKE auf " + gameObject.name);

        health.TakeDamage(thunderstrikeDamage, Vector3.up);

        Collider[] hits = Physics.OverlapSphere(transform.position, thunderstrikeChainRange, enemyLayerMask, QueryTriggerInteraction.Ignore);
        float closestDistance = float.MaxValue;
        Health closestHealth = null;
        Transform closestTransform = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Health targetHealth = hits[i].GetComponentInParent<Health>();
            if (targetHealth == null || targetHealth == health)
                continue;

            float distance = Vector3.Distance(transform.position, hits[i].transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHealth = targetHealth;
                closestTransform = hits[i].transform;
            }
        }

        if (closestHealth != null)
        {
            Vector3 dir = (closestTransform.position - transform.position).normalized;
            closestHealth.TakeDamage(thunderstrikeDamage, dir);
            Debug.Log("[TagReaction] THUNDERSTRIKE chain hit: " + closestHealth.gameObject.name);
        }
    }

    private void ReactionShockBleed(int bleedStacks)
    {
        float totalDamage = shockBleedDamagePerBleedStack * bleedStacks;
        Debug.Log("[TagReaction] SHOCK BLEED auf " + gameObject.name + " | Stacks: " + bleedStacks + " | DMG: " + totalDamage);
        health.TakeDamage(totalDamage, Vector3.up);
    }

    private void ReactionCauterize()
    {
        Debug.Log("[TagReaction] CAUTERIZE auf " + gameObject.name);
        health.TakeDamage(cauterizeDamage, Vector3.forward);
    }

    private void ReactionExposed()
    {
        isExposedActive = true;
        exposedTimer = exposedDuration;
        Debug.Log("[TagReaction] EXPOSED aktiv auf " + gameObject.name + " für " + exposedDuration + "s | Nächster Treffer doppelt TODO");
    }

    private void ReactionToxicFumes()
    {
        Debug.Log("[TagReaction] TOXIC FUMES auf " + gameObject.name + " | Cloud TODO | Radius: " + toxicFumesRadius + " | Dauer: " + toxicFumesDuration);
    }

    // ─────────────────────────────────────────
    // Helper
    // ─────────────────────────────────────────

    private bool IsCombo(TagType triggerTag, TagType a, TagType b)
    {
        if (triggerTag != a && triggerTag != b)
            return false;

        return tagHandler.HasTag(a) && tagHandler.HasTag(b);
    }

    private void UpdateExposedTimer()
    {
        if (!isExposedActive)
            return;

        exposedTimer -= Time.deltaTime;
        if (exposedTimer <= 0f)
        {
            isExposedActive = false;
            exposedTimer = 0f;
            Debug.Log("[TagReaction] EXPOSED abgelaufen auf " + gameObject.name);
        }
    }

    public bool ConsumeExposedModifier()
    {
        if (!isExposedActive)
            return false;

        isExposedActive = false;
        exposedTimer = 0f;
        Debug.Log("[TagReaction] EXPOSED verbraucht auf " + gameObject.name);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, steamExplosionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, blizzardRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, thunderstrikeChainRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, toxicFumesRadius);
    }
}