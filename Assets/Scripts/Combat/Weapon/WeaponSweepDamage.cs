using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Physikalische Trefferermittlung per SphereCast + Overlap.
/// Exposed wird immer über TagReactionSystem.ConsumeExposedModifier() geholt.
/// Charged + Exposed stapeln sich multiplikativ.
/// </summary>
public class WeaponSweepDamage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[]        samplePoints;
    [SerializeField] private WeaponHitFeedback  hitFeedback;
    [SerializeField] private WeaponChargeSystem chargeSystem;

    [Header("Hit Detection")]
    [SerializeField] private float     sampleRadius   = 0.14f;
    [SerializeField] private float     bladeHalfWidth = 0.035f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Damage")]
    [SerializeField] private float damage                  = 20f;
    [SerializeField] private float chargedDamageMultiplier = 2f;

    [Header("Attack Window")]
    [SerializeField] private bool attackWindowActive = false;

    [HideInInspector] public bool isFlying = false;

    private Vector3[]       lastPositions;
    private HashSet<Health> hitThisWindow = new HashSet<Health>();

    private void Awake()
    {
        if (chargeSystem == null)
            chargeSystem = GetComponent<WeaponChargeSystem>();
    }

    private void Start()
    {
        if (samplePoints == null || samplePoints.Length == 0)
        {
            Debug.LogWarning("[WeaponSweepDamage] Keine SamplePoints gesetzt!");
            enabled = false;
            return;
        }
        lastPositions = new Vector3[samplePoints.Length];
        SnapLastPositions();
    }

    private void Update()
    {
        Physics.SyncTransforms();

        if (!attackWindowActive || isFlying)
        {
            SnapLastPositions();
            return;
        }

        for (int i = 0; i < samplePoints.Length; i++)
        {
            Transform pt = samplePoints[i];
            if (pt == null) continue;

            Vector3 cur       = pt.position;
            Vector3 prev      = lastPositions[i];
            Vector3 delta     = cur - prev;
            Vector3 hitDir    = delta.sqrMagnitude > 0.0001f ? delta.normalized : transform.forward;
            Vector3 bladeAxis = pt.right;

            CheckTrack(prev,                               cur,                               hitDir, sampleRadius);
            CheckTrack(prev - bladeAxis * bladeHalfWidth,  cur - bladeAxis * bladeHalfWidth,  hitDir, sampleRadius);
            CheckTrack(prev + bladeAxis * bladeHalfWidth,  cur + bladeAxis * bladeHalfWidth,  hitDir, sampleRadius);

            lastPositions[i] = cur;
        }
    }

    public void BeginAttackWindow()
    {
        attackWindowActive = true;
        hitThisWindow.Clear();
        SnapLastPositions();
        Debug.Log("[WeaponSweepDamage] AttackWindow BEGIN");
    }

    public void EndAttackWindow()
    {
        attackWindowActive = false;
        hitThisWindow.Clear();
        SnapLastPositions();
        Debug.Log("[WeaponSweepDamage] AttackWindow END");
    }

    private void CheckTrack(Vector3 start, Vector3 end, Vector3 hitDir, float radius)
    {
        Vector3 delta    = end - start;
        float   distance = delta.magnitude;

        if (distance > 0.0001f)
        {
            RaycastHit[] hits = Physics.SphereCastAll(start, radius, delta.normalized, distance, enemyLayers, QueryTriggerInteraction.Collide);
            foreach (RaycastHit h in hits) TryDamage(h.collider, hitDir);

            CheckOverlap(start + delta * 0.25f, hitDir, radius);
            CheckOverlap(start + delta * 0.5f,  hitDir, radius);
            CheckOverlap(start + delta * 0.75f, hitDir, radius);
        }
        CheckOverlap(end, hitDir, radius);
    }

    private void CheckOverlap(Vector3 pos, Vector3 hitDir, float radius)
    {
        Collider[] cols = Physics.OverlapSphere(pos, radius, enemyLayers, QueryTriggerInteraction.Collide);
        foreach (Collider c in cols) TryDamage(c, hitDir);
    }

    private void TryDamage(Collider col, Vector3 hitDir)
    {
        if (col == null) return;

        Health health = col.GetComponentInParent<Health>();
        if (health == null)              return;
        if (!hitThisWindow.Add(health))  return;

        // ── Charged ──
        bool  wasCharged  = chargeSystem != null && chargeSystem.IsCharged;
        float finalDamage = wasCharged ? damage * chargedDamageMultiplier : damage;

        // ── Exposed — liest Multiplier direkt vom ReactionSystem ──
        TagReactionSystem reactions = col.GetComponentInParent<TagReactionSystem>();
        bool isExposed = reactions != null && reactions.ConsumeExposedModifier();

        if (isExposed)
            finalDamage *= reactions.ExposedMultiplier;

        // ── Schaden ──
        if (isExposed)
            health.TakeDamageReaction(finalDamage, hitDir, true);
        else
            health.TakeDamage(finalDamage, hitDir);

        // ── Charged Events + Charge verbrauchen ──
        if (wasCharged && chargeSystem != null)
        {
            Vector3 hitPoint = col.ClosestPoint(transform.position);
            chargeSystem.NotifyChargedHit(health, col, hitDir, hitPoint, damage, finalDamage);
            chargeSystem.ExpendCharge();
        }

        if (hitFeedback != null)
            hitFeedback.PlayHitFeedback(col.ClosestPoint(transform.position), hitDir);

        Debug.Log($"[WeaponSweepDamage] Hit {health.name} | DMG:{finalDamage:F1} charged:{wasCharged} exposed:{isExposed}");
    }

    private void SnapLastPositions()
    {
        if (lastPositions == null) return;
        for (int i = 0; i < samplePoints.Length; i++)
            if (samplePoints[i] != null)
                lastPositions[i] = samplePoints[i].position;
    }
}