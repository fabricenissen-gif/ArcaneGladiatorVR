using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class RangedGruntProjectile : MonoBehaviour
{
    private enum ProjectileMode { Direct, Lobbed }

    private ProjectileMode mode;
    private Rigidbody rb;
    private Collider projectileCollider;

    private Vector3 direction;
    private float speed;
    private float damage;
    private float knockback;
    private LayerMask targetLayer;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float arcHeight;
    private float flightTime;
    private float elapsed;
    private float lobbedDamage;
    private float splashRadius;

    private bool hasHit = false;

    [SerializeField] private float lifetime = 6f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        projectileCollider = GetComponent<Collider>();
    }

    public void InitDirect(Vector3 dir, float spd, float dmg, float kb, LayerMask layer)
    {
        mode = ProjectileMode.Direct;
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        knockback = kb;
        targetLayer = layer;

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = direction * speed;

        IgnoreSwordColliders();

        Destroy(gameObject, lifetime);
    }

    public void InitLobbed(Vector3 target, float arc, float flight, float dmg, LayerMask layer, float splash = 1.5f)
    {
        mode = ProjectileMode.Lobbed;
        startPos = transform.position;
        targetPos = target;
        arcHeight = arc;
        flightTime = flight;
        lobbedDamage = dmg;
        splashRadius = splash;
        targetLayer = layer;
        elapsed = 0f;

        rb.useGravity = false;
        rb.isKinematic = true;

        IgnoreSwordColliders();

        Destroy(gameObject, flightTime + 0.5f);
    }

    private void IgnoreSwordColliders()
    {
        if (projectileCollider == null) return;

        GameObject[] swordRoots = GameObject.FindGameObjectsWithTag("Sword");
        foreach (GameObject swordRoot in swordRoots)
        {
            Collider[] swordColliders = swordRoot.GetComponentsInChildren<Collider>(true);
            foreach (Collider swordCol in swordColliders)
            {
                if (swordCol == null) continue;
                Physics.IgnoreCollision(projectileCollider, swordCol, true);
            }
        }
    }

    private void Update()
    {
        if (mode != ProjectileMode.Lobbed || hasHit) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightTime);

        Vector3 flatPos = Vector3.Lerp(startPos, targetPos, t);
        float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = new Vector3(flatPos.x, flatPos.y + height, flatPos.z);

        if (t < 0.99f)
        {
            float t2 = Mathf.Clamp01((elapsed + 0.02f) / flightTime);
            Vector3 next = Vector3.Lerp(startPos, targetPos, t2);
            float nextH = Mathf.Sin(t2 * Mathf.PI) * arcHeight;
            Vector3 nextPos = new Vector3(next.x, next.y + nextH, next.z);
            Vector3 dir = (nextPos - transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        if (t >= 1f)
            ExplodeLobbed();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (other == null) return;
        if (other.isTrigger) return;
        if (other.transform.IsChildOf(transform)) return;

        int hitLayer = other.gameObject.layer;
        bool isTarget = (targetLayer.value & (1 << hitLayer)) != 0;

        if (isTarget)
        {
            hasHit = true;
            ApplyDirectDamage(other);
            Destroy(gameObject);
            return;
        }

        hasHit = true;
        Destroy(gameObject);
    }

    private void ApplyDirectDamage(Collider col)
    {
        PlayerHealth ph = col.GetComponentInParent<PlayerHealth>();

        if (ph == null)
        {
            PlayerHealth[] allPH = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            foreach (var candidate in allPH)
            {
                if (col.transform.IsChildOf(candidate.transform) ||
                    candidate.transform.IsChildOf(col.transform))
                {
                    ph = candidate;
                    break;
                }
            }
        }

        if (ph == null)
        {
            Debug.LogWarning($"[Proj] Treffer auf '{col.gameObject.name}' — kein PlayerHealth gefunden.");
            return;
        }

        Vector3 hitDir = direction.sqrMagnitude > 0.001f ? direction : transform.forward;
        ph.TakeDamage((int)damage, hitDir);

        Rigidbody playerRb = col.GetComponentInParent<Rigidbody>();
        if (playerRb != null && !playerRb.isKinematic)
            playerRb.AddForce(hitDir * knockback, ForceMode.Impulse);
    }

    private void ExplodeLobbed()
    {
        hasHit = true;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            splashRadius,
            targetLayer,
            QueryTriggerInteraction.Collide);

        foreach (Collider col in hits)
        {
            PlayerHealth ph = col.GetComponentInParent<PlayerHealth>();
            if (ph == null) continue;

            Vector3 dir = (col.transform.position - transform.position).normalized;
            ph.TakeDamage((int)lobbedDamage, dir);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (mode == ProjectileMode.Lobbed)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPos, splashRadius);
        }
    }
}