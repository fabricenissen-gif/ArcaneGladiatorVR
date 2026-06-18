using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class ArcaneBoltProjectile : MonoBehaviour
{
    [Header("Runtime — wird per Initialize gesetzt")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float speed = 18f;
    [SerializeField] private float lifetime = 4f;

    [Header("Impact")]
    [SerializeField] private ParticleSystem impactVfxPrefab;
    [SerializeField] private LayerMask hitLayers;

    private Rigidbody rb;
    private bool hasHit = false;

    private TagType spellTag = TagType.ARCANE;
    private float tagDuration = 3f;
    private bool applyTag = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
    }

    public void Initialize(float damage, float speed, float lifetime)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifetime = lifetime;

        applyTag = false;

        rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, lifetime);
    }

    public void Initialize(float damage, float speed, float lifetime, TagType tag, float tagDuration)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifetime = lifetime;
        this.spellTag = tag;
        this.tagDuration = tagDuration;
        this.applyTag = true;

        rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Layer-Check
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        hasHit = true;

        // Schaden
        Health health = other.GetComponentInParent<Health>();
        if (health != null)
        {
            Vector3 hitDirection = transform.forward;
            health.TakeDamage(damage, hitDirection);
        }

        // TAG applizieren
        if (applyTag)
        {
            TagHandler tagHandler = other.GetComponentInParent<TagHandler>();
            if (tagHandler != null)
                tagHandler.ApplyTag(spellTag, tagDuration);
        }

        // Impact VFX
        if (impactVfxPrefab != null)
        {
            ParticleSystem vfx = Instantiate(impactVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx.gameObject, 3f);
        }

        Destroy(gameObject);
    }
}