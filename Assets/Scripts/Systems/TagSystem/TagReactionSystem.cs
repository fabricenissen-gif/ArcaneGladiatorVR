using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TagHandler))]
[RequireComponent(typeof(Health))]
public class TagReactionSystem : MonoBehaviour
{
    [Header("Steam Explosion")]
    [SerializeField, Min(0f)] private float steamExplosionDamage = 20f;
    [SerializeField, Min(0f)] private float steamExplosionRadius = 3f;
    [SerializeField, Min(0f)] private float steamExplosionKnockbackForce = 6f;

    [Header("Shatter")]
    [SerializeField, Min(0f)] private float shatterDamage = 30f;
    [SerializeField, Min(0f)] private float shatterStunDuration = 1.5f;

    [Header("Blizzard")]
    [SerializeField, Min(0f)] private float blizzardRadius = 4f;
    [SerializeField, Min(0f)] private float blizzardDuration = 3f;

    [Header("Arcane Ignite")]
    [SerializeField, Min(0f)] private float arcaneIgniteBurstDamage = 12f;
    [SerializeField, Min(1)] private int arcaneIgniteTicks = 4;
    [SerializeField, Min(0f)] private float arcaneIgniteDuration = 3f;
    [SerializeField, Min(0f)] private float arcaneIgniteTickDamage = 4f;
    [SerializeField] private GameObject burningVfxPrefab;

    [Header("Venom Burst")]
    [SerializeField, Min(0f)] private float venomBurstDamagePerPoisonStack = 10f;

    [Header("Thunderstrike")]
    [SerializeField, Min(0f)] private float thunderstrikeDamage = 18f;
    [SerializeField, Min(0f)] private float thunderstrikeChainRange = 5f;
    [SerializeField] private LightningEffect lightningEffectPrefab;
    [SerializeField, Min(0f)] private float chainedElectricDuration = 3f;

    [Header("Exposed")]
    [SerializeField, Min(0f)] private float exposedDuration = 5f;
    [SerializeField, Min(0f)] private float exposedDamageMultiplier = 2f;

    [Header("Toxic Fumes")]
    [SerializeField, Min(0f)] private float toxicFumesDuration = 4f;
    [SerializeField, Min(0f)] private float toxicFumesRadius = 3f;
    [SerializeField] private ToxicFumeCloud toxicFumeCloudPrefab;

    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayerMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool logReactions = true;

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
            tagHandler.OnTagChanged += OnTagChanged;
    }

    private void OnDisable()
    {
        if (tagHandler != null)
            tagHandler.OnTagChanged -= OnTagChanged;

        StopArcaneIgnite();
    }

    private void Update()
    {
        UpdateExposedTimer();
    }

    public bool ConsumeExposedModifier()
    {
        if (!isExposedActive)
            return false;

        isExposedActive = false;
        exposedTimer = 0f;

        return true;
    }

    private void OnTagChanged(TagHandler.TagChangeEvent tagEvent)
    {
        if (health == null || health.IsDead)
            return;

        if (tagEvent.ChangeType != TagHandler.TagChangeType.Applied &&
            tagEvent.ChangeType != TagHandler.TagChangeType.Refreshed)
        {
            return;
        }

        TagInstance instance = tagEvent.Instance;

        if (instance == null || !instance.CanTriggerReactions)
            return;

        CheckReactions(tagEvent.TagType);
    }

    private void CheckReactions(TagType triggerTag)
    {
        if (TryReactSteamExplosion(triggerTag))
            return;

        if (TryReactShatter(triggerTag))
            return;

        if (TryReactBlizzard(triggerTag))
            return;

        if (TryReactArcaneIgnite(triggerTag))
            return;

        if (TryReactVenomBurst(triggerTag))
            return;

        if (TryReactThunderstrike(triggerTag))
            return;

        if (TryReactExposed(triggerTag))
            return;

        TryReactToxicFumes(triggerTag);
    }

    private bool TryReactSteamExplosion(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.FIRE, TagType.FROST))
            return false;

        if (!ConsumeTags(TagType.FIRE, TagType.FROST))
            return false;

        LogReaction("STEAM EXPLOSION");
        ReactionSteamExplosion();

        return true;
    }

    private bool TryReactShatter(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ELECTRIC, TagType.FROST))
            return false;

        if (!ConsumeTags(TagType.ELECTRIC, TagType.FROST))
            return false;

        LogReaction("SHATTER");
        ReactionShatter();

        return true;
    }

    private bool TryReactBlizzard(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.WIND, TagType.FROST))
            return false;

        if (!ConsumeTags(TagType.WIND, TagType.FROST))
            return false;

        LogReaction("BLIZZARD");
        ReactionBlizzard();

        return true;
    }

    private bool TryReactArcaneIgnite(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ARCANE, TagType.FIRE))
            return false;

        if (!ConsumeTags(TagType.ARCANE, TagType.FIRE))
            return false;

        LogReaction("ARCANE IGNITE");
        ReactionArcaneIgnite();

        return true;
    }

    private bool TryReactVenomBurst(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ARCANE, TagType.POISON))
            return false;

        TagInstance poisonTag = tagHandler.GetTag(TagType.POISON);

        int poisonStacks = poisonTag != null
            ? poisonTag.StackCount
            : 1;

        if (!ConsumeTags(TagType.ARCANE, TagType.POISON))
            return false;

        LogReaction($"VENOM BURST | stacks:{poisonStacks}");
        ReactionVenomBurst(poisonStacks);

        return true;
    }

    private bool TryReactThunderstrike(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.ELECTRIC, TagType.WIND))
            return false;

        if (!ConsumeTags(TagType.ELECTRIC, TagType.WIND))
            return false;

        LogReaction("THUNDERSTRIKE");
        ReactionThunderstrike();

        return true;
    }

    private bool TryReactExposed(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.MARKED, TagType.ARCANE))
            return false;

        if (!ConsumeTags(TagType.MARKED, TagType.ARCANE))
            return false;

        LogReaction("EXPOSED");
        ReactionExposed();

        return true;
    }

    private bool TryReactToxicFumes(TagType triggerTag)
    {
        if (!IsCombo(triggerTag, TagType.POISON, TagType.FIRE))
            return false;

        if (!ConsumeTags(TagType.POISON, TagType.FIRE))
            return false;

        LogReaction("TOXIC FUMES");
        ReactionToxicFumes();

        return true;
    }

    private bool IsCombo(
        TagType triggerTag,
        TagType firstTag,
        TagType secondTag)
    {
        if (triggerTag != firstTag && triggerTag != secondTag)
            return false;

        return tagHandler.HasTag(firstTag) &&
               tagHandler.HasTag(secondTag);
    }

    private bool ConsumeTags(TagType firstTag, TagType secondTag)
    {
        TagInstance first = tagHandler.GetTag(firstTag);
        TagInstance second = tagHandler.GetTag(secondTag);

        if (first == null || second == null)
            return false;

        if (!first.IsConsumedOnReaction ||
            !second.IsConsumedOnReaction)
        {
            return false;
        }

        tagHandler.RemoveTag(firstTag);
        tagHandler.RemoveTag(secondTag);

        return true;
    }

    private void ReactionSteamExplosion()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            steamExplosionRadius,
            enemyLayerMask,
            QueryTriggerInteraction.Ignore);

        HashSet<Health> alreadyHit = new HashSet<Health>();

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            Health targetHealth = hit.GetComponentInParent<Health>();

            if (targetHealth == null ||
                targetHealth.IsDead ||
                !alreadyHit.Add(targetHealth))
            {
                continue;
            }

            Vector3 direction =
                targetHealth.transform.position - transform.position;

            direction = direction.sqrMagnitude <= 0.0001f
                ? Vector3.up
                : direction.normalized;

            targetHealth.TakeDamageReaction(
                steamExplosionDamage,
                direction);

            Rigidbody targetRigidbody =
                targetHealth.GetComponent<Rigidbody>();

            if (targetRigidbody != null &&
                !targetRigidbody.isKinematic)
            {
                Vector3 forceDirection =
                    (direction + Vector3.up * 0.35f).normalized;

                targetRigidbody.AddForce(
                    forceDirection * steamExplosionKnockbackForce,
                    ForceMode.Impulse);
            }
        }
    }

    private void ReactionShatter()
    {
        if (health == null || health.IsDead)
            return;

        health.TakeDamageReaction(shatterDamage, Vector3.up);

        SimpleEnemyChase chase = GetComponent<SimpleEnemyChase>();

        if (chase != null)
        {
            StartCoroutine(
                DisableBehaviourTemporarily(
                    chase,
                    shatterStunDuration));
        }

        SwarmerAI swarmer = GetComponent<SwarmerAI>();

        if (swarmer != null)
        {
            StartCoroutine(
                DisableBehaviourTemporarily(
                    swarmer,
                    shatterStunDuration));
        }
    }

    private void ReactionBlizzard()
    {
        Debug.Log(
            $"[TagReactionSystem] BLIZZARD on '{gameObject.name}' " +
            $"is not yet prioritised. Radius:{blizzardRadius:F1} " +
            $"Duration:{blizzardDuration:F1}");
    }

    private void ReactionArcaneIgnite()
    {
        if (health == null || health.IsDead)
            return;

        health.TakeDamageReaction(
            arcaneIgniteBurstDamage,
            Vector3.zero);

        StopArcaneIgnite();

        igniteRoutine = StartCoroutine(ArcaneIgniteRoutine());

        if (burningVfxPrefab == null)
            return;

        if (activeBurningVfx != null)
            Destroy(activeBurningVfx);

        activeBurningVfx = Instantiate(
            burningVfxPrefab,
            transform.position,
            Quaternion.identity,
            transform);
    }

    private IEnumerator ArcaneIgniteRoutine()
    {
        float tickInterval = arcaneIgniteDuration /
            Mathf.Max(1, arcaneIgniteTicks);

        for (int i = 0; i < arcaneIgniteTicks; i++)
        {
            yield return new WaitForSeconds(tickInterval);

            if (health == null || health.IsDead)
                break;

            health.TakeDamageReaction(
                arcaneIgniteTickDamage,
                Vector3.zero);
        }

        igniteRoutine = null;

        if (activeBurningVfx != null)
        {
            Destroy(activeBurningVfx);
            activeBurningVfx = null;
        }
    }

    private void ReactionVenomBurst(int poisonStacks)
    {
        if (health == null || health.IsDead)
            return;

        float totalDamage =
            venomBurstDamagePerPoisonStack *
            Mathf.Max(1, poisonStacks);

        health.TakeDamageReaction(totalDamage, Vector3.zero);
    }

    private void ReactionThunderstrike()
    {
        if (health == null || health.IsDead)
            return;

        health.TakeDamageReaction(thunderstrikeDamage, Vector3.up);

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            thunderstrikeChainRange,
            enemyLayerMask,
            QueryTriggerInteraction.Ignore);

        float closestDistance = float.MaxValue;
        Health closestHealth = null;
        TagHandler closestTagHandler = null;

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            Health targetHealth = hit.GetComponentInParent<Health>();

            if (targetHealth == null ||
                targetHealth.IsDead ||
                targetHealth == health)
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                targetHealth.transform.position);

            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closestHealth = targetHealth;

            closestTagHandler =
                targetHealth.GetComponent<TagHandler>();
        }

        if (closestHealth == null)
            return;

        Vector3 direction =
            closestHealth.transform.position - transform.position;

        direction = direction.sqrMagnitude <= 0.0001f
            ? Vector3.up
            : direction.normalized;

        closestHealth.TakeDamageReaction(
            thunderstrikeDamage,
            direction);

        closestTagHandler?.ApplyTag(
            TagType.ELECTRIC,
            chainedElectricDuration,
            TagApplicationContext.FromReaction(
                this,
                "ThunderstrikeChain"));

        if (lightningEffectPrefab != null)
        {
            LightningEffect effect = Instantiate(
                lightningEffectPrefab,
                Vector3.zero,
                Quaternion.identity);

            effect.Initialize(
                transform.position,
                closestHealth.transform.position);
        }
    }

    private void ReactionExposed()
    {
        isExposedActive = true;
        exposedTimer = exposedDuration;
    }

    private void ReactionToxicFumes()
    {
        if (toxicFumeCloudPrefab == null)
        {
            Debug.LogWarning(
                $"[TagReactionSystem] TOXIC FUMES on '{gameObject.name}' " +
                "could not spawn because no ToxicFumeCloud prefab is assigned.");

            return;
        }

        ToxicFumeCloud cloud = Instantiate(
            toxicFumeCloudPrefab,
            transform.position,
            Quaternion.identity);

        TagApplicationContext context =
            TagApplicationContext.FromReaction(
                this,
                "ToxicFumesCloud");

        cloud.Initialize(
            toxicFumesDuration,
            toxicFumesRadius,
            enemyLayerMask,
            context);
    }

    private void UpdateExposedTimer()
    {
        if (!isExposedActive)
            return;

        exposedTimer -= Time.deltaTime;

        if (exposedTimer > 0f)
            return;

        isExposedActive = false;
        exposedTimer = 0f;
    }

    private void StopArcaneIgnite()
    {
        if (igniteRoutine != null)
        {
            StopCoroutine(igniteRoutine);
            igniteRoutine = null;
        }

        if (activeBurningVfx != null)
        {
            Destroy(activeBurningVfx);
            activeBurningVfx = null;
        }
    }

    private void LogReaction(string reactionName)
    {
        if (!logReactions)
            return;

        Debug.Log(
            $"[TagReactionSystem] {reactionName} on '{gameObject.name}'.");
    }

    private IEnumerator DisableBehaviourTemporarily(
        MonoBehaviour behaviour,
        float duration)
    {
        if (behaviour == null)
            yield break;

        behaviour.enabled = false;

        yield return new WaitForSeconds(duration);

        if (behaviour != null &&
            gameObject.activeInHierarchy &&
            health != null &&
            !health.IsDead)
        {
            behaviour.enabled = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(
            transform.position,
            steamExplosionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            blizzardRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            thunderstrikeChainRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            transform.position,
            toxicFumesRadius);
    }
}