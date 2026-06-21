using UnityEngine;

/// <summary>
/// Projektil das Tags appliziert und Exposed-Bonus nutzt.
/// Initialize() MUSS direkt nach Instantiate() aufgerufen werden.
/// Tag wird NACH dem Schaden appliziert — verhindert self-trigger.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class ArcaneBoltProjectile : MonoBehaviour
{
    [Header("Impact")]
    [SerializeField] private ParticleSystem impactVfxPrefab;
    [SerializeField] private LayerMask      hitLayers;

    private float   damage;
    private float   speed;
    private float   lifetime;
    private TagType spellTag    = TagType.ARCANE;
    private float   tagDuration = 3f;
    private bool    applyTag    = true;
    private bool    hasHit      = false;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity             = false;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        GetComponent<SphereCollider>().isTrigger = true;
    }

    /// Ohne Tag — reines Schadensgeschoss (weiße Zahl)
    public void Initialize(float damage, float speed, float lifetime)
    {
        this.damage   = damage;
        this.speed    = speed;
        this.lifetime = lifetime;
        this.applyTag = false;
        Launch();
    }

    /// Mit Tag — appliziert Tag UND prüft Exposed
    public void Initialize(float damage, float speed, float lifetime, TagType tag, float tagDuration)
    {
        this.damage      = damage;
        this.speed       = speed;
        this.lifetime    = lifetime;
        this.spellTag    = tag;
        this.tagDuration = tagDuration;
        this.applyTag    = true;
        Launch();
    }

    private void Launch()
    {
        rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        hasHit = true;

        Health health = other.GetComponentInParent<Health>();
        if (health != null)
        {
            Vector3 hitDir = transform.forward;

            // Exposed ZUERST prüfen — bevor der Tag appliziert wird
            TagReactionSystem reactions = other.GetComponentInParent<TagReactionSystem>();
            bool  isExposed   = reactions != null && reactions.ConsumeExposedModifier();
            float finalDamage = isExposed
                ? damage * (reactions?.ExposedMultiplier ?? 2f)
                : damage;

            if (isExposed)
            {
                Debug.Log($"[ArcaneBolt] EXPOSED hit! DMG:{finalDamage:F1}");
                health.TakeDamageReaction(finalDamage, hitDir, true);
            }
            else
            {
                // Tagged → farbige Zahl (lila für Arcane), kein Reaction-Bonus
                health.TakeDamageTagged(finalDamage, hitDir, spellTag);
            }
        }

        // Tag NACH dem Schaden — verhindert dass neuer Tag sofort eine Reaction triggert
        if (applyTag)
            other.GetComponentInParent<TagHandler>()?.ApplyTag(spellTag, tagDuration);

        if (impactVfxPrefab != null)
            Destroy(Instantiate(impactVfxPrefab, transform.position, Quaternion.identity).gameObject, 3f);

        Destroy(gameObject);
    }
}