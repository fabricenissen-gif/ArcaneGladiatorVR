using UnityEngine;

/// <summary>
/// Projektil für direkten Spell-Schaden und optionale Tag-Anwendung.
/// Initialize() muss direkt nach Instantiate() aufgerufen werden.
/// Der Tag wird erst nach dem direkten Schaden angewandt, damit das
/// neue Tag nicht den Schaden desselben Projektils beeinflussen kann.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class ArcaneBoltProjectile : MonoBehaviour
{
    [Header("Impact")]
    [SerializeField] private ParticleSystem impactVfxPrefab;
    [SerializeField] private LayerMask hitLayers;

    private float damage;
    private float speed;
    private float lifetime;

    private TagType spellTag = TagType.ARCANE;
    private float tagDuration = 3f;
    private bool applyTag;
    private bool hasHit;

    private TagApplicationContext tagContext =
        TagApplicationContext.Unknown;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
    }

    /// <summary>
    /// Reines Schadensprojektil ohne Tag.
    /// </summary>
    public void Initialize(
        float damage,
        float speed,
        float lifetime)
    {
        this.damage = Mathf.Max(0f, damage);
        this.speed = Mathf.Max(0f, speed);
        this.lifetime = Mathf.Max(0f, lifetime);

        spellTag = TagType.NONE;
        tagDuration = 0f;
        applyTag = false;
        tagContext = TagApplicationContext.Unknown;

        Launch();
    }

    /// <summary>
    /// Kompatibler Tag-Projektilpfad ohne bekannte Quelle.
    /// Bestehende externe Aufrufer bleiben dadurch funktionsfähig.
    /// </summary>
    public void Initialize(
        float damage,
        float speed,
        float lifetime,
        TagType tag,
        float tagDuration)
    {
        Initialize(
            damage,
            speed,
            lifetime,
            tag,
            tagDuration,
            TagApplicationContext.Unknown);
    }

    /// <summary>
    /// Tag-Projektil mit nachvollziehbarer Quelle.
    /// </summary>
    public void Initialize(
        float damage,
        float speed,
        float lifetime,
        TagType tag,
        float tagDuration,
        TagApplicationContext context)
    {
        this.damage = Mathf.Max(0f, damage);
        this.speed = Mathf.Max(0f, speed);
        this.lifetime = Mathf.Max(0f, lifetime);

        spellTag = tag;
        this.tagDuration = Mathf.Max(0f, tagDuration);
        applyTag = tag != TagType.NONE;
        tagContext = context;

        Launch();
    }

    private void Launch()
    {
        if (rb == null)
        {
            Debug.LogError(
                $"[ArcaneBoltProjectile] Rigidbody missing on " +
                $"'{gameObject.name}'.");

            Destroy(gameObject);
            return;
        }

        rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit || other == null)
            return;

        int otherLayerMask = 1 << other.gameObject.layer;

        if ((hitLayers.value & otherLayerMask) == 0)
            return;

        hasHit = true;

        Health health = other.GetComponentInParent<Health>();

        if (health != null && !health.IsDead)
        {
            Vector3 hitDirection = transform.forward;

            TagReactionSystem reactions =
                other.GetComponentInParent<TagReactionSystem>();

            bool isExposed =
                reactions != null &&
                reactions.ConsumeExposedModifier();

            float finalDamage = isExposed
                ? damage * reactions.ExposedMultiplier
                : damage;

            if (isExposed)
            {
                Debug.Log(
                    $"[ArcaneBoltProjectile] EXPOSED hit | " +
                    $"damage:{finalDamage:F1}");

                health.TakeDamageReaction(
                    finalDamage,
                    hitDirection,
                    isExposed: true);
            }
            else if (applyTag)
            {
                health.TakeDamageTagged(
                    finalDamage,
                    hitDirection,
                    spellTag);
            }
            else
            {
                health.TakeDamage(
                    finalDamage,
                    hitDirection);
            }
        }

        // Der Tag kommt bewusst nach dem direkten Treffer.
        if (applyTag)
        {
            TagHandler tagHandler =
                other.GetComponentInParent<TagHandler>();

            tagHandler?.ApplyTag(
                spellTag,
                tagDuration,
                tagContext);
        }

        SpawnImpactVfx();
        Destroy(gameObject);
    }

    private void SpawnImpactVfx()
    {
        if (impactVfxPrefab == null)
            return;

        ParticleSystem impactVfx = Instantiate(
            impactVfxPrefab,
            transform.position,
            Quaternion.identity);

        Destroy(impactVfx.gameObject, 3f);
    }
}