using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ArcaneBoltProjectile : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private float damage = 12f;
    [SerializeField] private float speed = 18f;
    [SerializeField] private float lifetime = 4f;

    [Header("Impact")]
    [SerializeField] private ParticleSystem impactVfxPrefab;
    [SerializeField] private LayerMask hitLayers = ~0;

    private bool initialized = false;

    private void Awake()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
    }

    public void Initialize(float newDamage, float newSpeed, float newLifetime)
    {
        damage = newDamage;
        speed = newSpeed;
        lifetime = newLifetime;
        initialized = true;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized)
            return;

        if (((1 << other.gameObject.layer) & hitLayers.value) == 0)
            return;

        if (other.isTrigger)
            return;

        Health health = other.GetComponentInParent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage, transform.forward);
        }

        SpawnImpact(other.ClosestPoint(transform.position));
        Destroy(gameObject);
    }

    private void SpawnImpact(Vector3 position)
    {
        if (impactVfxPrefab == null)
            return;

        ParticleSystem spawned = Instantiate(
            impactVfxPrefab,
            position,
            Quaternion.LookRotation(-transform.forward)
        );

        Destroy(spawned.gameObject, 3f);
    }
}