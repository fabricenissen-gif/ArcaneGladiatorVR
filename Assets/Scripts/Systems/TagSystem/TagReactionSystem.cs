using System.Collections;
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
    [SerializeField] private float arcaneIgniteBurstDamage = 12f;
    [SerializeField] private int arcaneIgniteTicks = 4;
    [SerializeField] private float arcaneIgniteDuration = 3f;
    [SerializeField] private float arcaneIgniteTickDamage = 4f;
    [SerializeField] private GameObject burningVfxPrefab;

    [Header("Venom Burst")]
    [SerializeField] private float venomBurstDamagePerPoisonStack = 10f;

    [Header("Thunderstrike")]
    [SerializeField] private float thunderstrikeDamage = 18f;
    [SerializeField] private float thunderstrikeChainRange = 5f;
    [SerializeField] private LightningEffect lightningEffectPrefab;
    [SerializeField] private float chainedElectricDuration = 3f;

    [Header("Shock Bleed")]
    [SerializeField] private float shockBleedDamagePerBleedStack = 8f;

    [Header("Cauterize")]
    [SerializeField] private float cauterizeDamage = 22f;

    [Header("Exposed")]
    [SerializeField] private float exposedDuration = 5f;
    [SerializeField] private float exposedDamageMultiplier = 2f;

    [Header("Toxic Fumes")]
    [SerializeField] private float toxicFumesDuration = 4f;
    [SerializeField] private float toxicFumesRadius = 3f;
    [SerializeField] private ToxicFumeCloud toxicFumeCloudPrefab;

    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayerMask = ~0;

    private TagHandler tagHandler;
    private Health health;

    private bool isExposedActive;
    private float exposedTimer;
    private Coroutine igniteRoutine;
    private GameObject activeBurningVfx;

    public bool IsExposed => isExposedActive;
    public float ExposedMultiplier => exposedDamageMultiplier;

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

    public bool ConsumeExposedModifier()
    {
        if (!isExposedActive) return false;
        isExposedActive = false;
        exposedTimer = 0f;
        return true;
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

    private bool TryReactSteamExplosion(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.FIRE, TagType.FROST)) return false;
        tagHandler.RemoveTag(TagType.FIRE);
        tagHandler.RemoveTag(TagType.FROST);
        ReactionSteamExplosion();
        return true;
    }

    private bool TryReactShatter(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ELECTRIC, TagType.FROST)) return false;
        tagHandler.RemoveTag(TagType.ELECTRIC);
        tagHandler.RemoveTag(TagType.FROST);
        ReactionShatter();
        return true;
    }

    private bool TryReactBlizzard(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.WIND, TagType.FROST)) return false;
        tagHandler.RemoveTag(TagType.WIND);
        tagHandler.RemoveTag(TagType.FROST);
        ReactionBlizzard();
        return true;
    }

    private bool TryReactArcaneIgnite(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ARCANE, TagType.FIRE)) return false;
        tagHandler.RemoveTag(TagType.ARCANE);
        tagHandler.RemoveTag(TagType.FIRE);
        ReactionArcaneIgnite();
        return true;
    }

    private bool TryReactVenomBurst(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ARCANE, TagType.POISON)) return false;
        TagInstance poisonTag = tagHandler.GetTag(TagType.POISON);
        int poisonStacks = poisonTag != null ? poisonTag.StackCount : 1;
        tagHandler.RemoveTag(TagType.ARCANE);
        tagHandler.RemoveTag(TagType.POISON);
        ReactionVenomBurst(poisonStacks);
        return true;
    }

    private bool TryReactThunderstrike(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ELECTRIC, TagType.WIND)) return false;
        tagHandler.RemoveTag(TagType.ELECTRIC);
        tagHandler.RemoveTag(TagType.WIND);
        ReactionThunderstrike();
        return true;
    }

    private bool TryReactShockBleed(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ELECTRIC, TagType.BLEED)) return false;
        TagInstance bleedTag = tagHandler.GetTag(TagType.BLEED);
        int bleedStacks = bleedTag != null ? bleedTag.StackCount : 1;
        tagHandler.RemoveTag(TagType.ELECTRIC);
        tagHandler.RemoveTag(TagType.BLEED);
        ReactionShockBleed(bleedStacks);
        return true;
    }

    private bool TryReactCauterize(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.FIRE, TagType.BLEED)) return false;
        tagHandler.RemoveTag(TagType.FIRE);
        tagHandler.RemoveTag(TagType.BLEED);
        ReactionCauterize();
        return true;
    }

    private bool TryReactExposed(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.MARKED, TagType.ARCANE)) return false;
        tagHandler.RemoveTag(TagType.MARKED);
        tagHandler.RemoveTag(TagType.ARCANE);
        ReactionExposed();
        return true;
    }

    private bool TryReactToxicFumes(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.POISON, TagType.FIRE)) return false;
        tagHandler.RemoveTag(TagType.POISON);
        tagHandler.RemoveTag(TagType.FIRE);
        ReactionToxicFumes();
        return true;
    }

    private void ReactionSteamExplosion()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, steamExplosionRadius, enemyLayerMask, QueryTriggerInteraction.Ignore);
        HashSet<Health> alreadyHit = new HashSet<Health>();

        foreach (Collider hit in hits)
        {
            Health targetHealth = hit.GetComponentInParent<Health>();
            if (targetHealth == null) continue;
            if (!alreadyHit.Add(targetHealth)) continue;

            Vector3 direction = targetHealth.transform.position - transform.position;
            direction = direction.sqrMagnitude <= 0.0001f ? Vector3.up : direction.normalized;
            targetHealth.TakeDamage(steamExplosionDamage, direction);

            Rigidbody targetRb = targetHealth.GetComponent<Rigidbody>();
            if (targetRb != null && !targetRb.isKinematic)
            {
                Vector3 forceDir = (direction + Vector3.up * 0.35f).normalized;
                targetRb.AddForce(forceDir * steamExplosionKnockbackForce, ForceMode.Impulse);
            }
        }
    }

    private void ReactionShatter()
    {
        health.TakeDamage(shatterDamage, Vector3.up);

        SimpleEnemyChase chase = GetComponent<SimpleEnemyChase>();
        if (chase != null)
            StartCoroutine(DisableBehaviourTemporarily(chase, shatterStunDuration));

        SwarmerAI swarmer = GetComponent<SwarmerAI>();
        if (swarmer != null)
            StartCoroutine(DisableBehaviourTemporarily(swarmer, shatterStunDuration));
    }

    private void ReactionBlizzard()
    {
        Debug.Log($"[TagReaction] BLIZZARD auf {gameObject.name} noch nicht priorisiert. Radius {blizzardRadius}, Dauer {blizzardDuration}");
    }

    private void ReactionArcaneIgnite()
    {
        health.TakeDamage(arcaneIgniteBurstDamage, Vector3.zero);

        if (igniteRoutine != null)
            StopCoroutine(igniteRoutine);
        igniteRoutine = StartCoroutine(ArcaneIgniteRoutine());

        if (burningVfxPrefab != null)
        {
            if (activeBurningVfx != null)
                Destroy(activeBurningVfx);
            activeBurningVfx = Instantiate(burningVfxPrefab, transform.position, Quaternion.identity, transform);
        }
    }

    private IEnumerator ArcaneIgniteRoutine()
    {
        float tickInterval = arcaneIgniteDuration / Mathf.Max(1, arcaneIgniteTicks);
        for (int i = 0; i < arcaneIgniteTicks; i++)
        {
            yield return new WaitForSeconds(tickInterval);
            if (health == null) yield break;
            health.TakeDamage(arcaneIgniteTickDamage, Vector3.zero);
        }

        if (activeBurningVfx != null)
            Destroy(activeBurningVfx);
        igniteRoutine = null;
    }

    private void ReactionVenomBurst(int poisonStacks)
    {
        float totalDamage = venomBurstDamagePerPoisonStack * poisonStacks;
        health.TakeDamage(totalDamage, Vector3.zero);
    }

    private void ReactionThunderstrike()
    {
        health.TakeDamage(thunderstrikeDamage, Vector3.up);

        Collider[] hits = Physics.OverlapSphere(transform.position, thunderstrikeChainRange, enemyLayerMask, QueryTriggerInteraction.Ignore);
        float closestDistance = float.MaxValue;
        Health closestHealth = null;
        TagHandler closestTagHandler = null;

        foreach (Collider hit in hits)
        {
            Health targetHealth = hit.GetComponentInParent<Health>();
            if (targetHealth == null || targetHealth == health) continue;

            float distance = Vector3.Distance(transform.position, targetHealth.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHealth = targetHealth;
                closestTagHandler = targetHealth.GetComponent<TagHandler>();
            }
        }

        if (closestHealth == null) return;

        Vector3 dir = (closestHealth.transform.position - transform.position).normalized;
        closestHealth.TakeDamage(thunderstrikeDamage, dir);
        closestTagHandler?.ApplyTag(TagType.ELECTRIC, chainedElectricDuration);

        if (lightningEffectPrefab != null)
        {
            LightningEffect fx = Instantiate(lightningEffectPrefab, Vector3.zero, Quaternion.identity);
            fx.Initialize(transform.position, closestHealth.transform.position);
        }
    }

    private void ReactionShockBleed(int bleedStacks)
    {
        float totalDamage = shockBleedDamagePerBleedStack * bleedStacks;
        health.TakeDamage(totalDamage, Vector3.up);
    }

    private void ReactionCauterize()
    {
        health.TakeDamage(cauterizeDamage, Vector3.forward);
    }

    private void ReactionExposed()
    {
        isExposedActive = true;
        exposedTimer = exposedDuration;
    }

    private void ReactionToxicFumes()
    {
        if (toxicFumeCloudPrefab == null) return;
        ToxicFumeCloud cloud = Instantiate(toxicFumeCloudPrefab, transform.position, Quaternion.identity);
        cloud.Initialize(toxicFumesDuration, toxicFumesRadius, enemyLayerMask);
    }

    private bool IsCombo(TagType triggerTag, TagType a, TagType b)
    {
        if (triggerTag != a && triggerTag != b) return false;
        return tagHandler.HasTag(a) && tagHandler.HasTag(b);
    }

    private void UpdateExposedTimer()
    {
        if (!isExposedActive) return;
        exposedTimer -= Time.deltaTime;
        if (exposedTimer <= 0f)
        {
            isExposedActive = false;
            exposedTimer = 0f;
        }
    }

    private IEnumerator DisableBehaviourTemporarily(MonoBehaviour behaviour, float duration)
    {
        if (behaviour == null) yield break;
        behaviour.enabled = false;
        yield return new WaitForSeconds(duration);
        if (behaviour != null)
            behaviour.enabled = true;
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