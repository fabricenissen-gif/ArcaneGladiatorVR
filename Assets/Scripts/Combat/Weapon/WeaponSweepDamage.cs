using System.Collections.Generic;
using UnityEngine;

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

    [Header("Damage — Swing")]
    [SerializeField] private float damage                  = 20f;
    [SerializeField] private float chargedDamageMultiplier = 2f;

    [Header("Damage — Throw")]
    [SerializeField] private float throwBaseDamage        = 25f;
    [SerializeField] private float throwChargedMultiplier = 2.5f;
    [SerializeField] private float minThrowDamageVelocity = 4f;

    [Header("Attack Window")]
    [SerializeField] private bool attackWindowActive = false;

    [HideInInspector] public bool isFlying = false;

    private Rigidbody       rb;
    private Vector3[]       lastPositions;
    private HashSet<Health> hitThisWindow = new HashSet<Health>();

    // ── Lifecycle ─────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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

    // ── Swing Detection ───────────────────────────────────────

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

            CheckTrack(prev,                              cur,                              hitDir, sampleRadius);
            CheckTrack(prev - bladeAxis * bladeHalfWidth, cur - bladeAxis * bladeHalfWidth, hitDir, sampleRadius);
            CheckTrack(prev + bladeAxis * bladeHalfWidth, cur + bladeAxis * bladeHalfWidth, hitDir, sampleRadius);

            lastPositions[i] = cur;
        }
    }

    // ── Throw Damage ──────────────────────────────────────────

    // Aufgerufen via RaycastHit (SphereCast-Pfad)
    public void HandleThrowHit(RaycastHit hit, bool wasCharged, WeaponChargeSystem throwChargeSystem)
    {
        Vector3 hitDir   = rb != null && rb.linearVelocity.sqrMagnitude > 0.001f
            ? rb.linearVelocity.normalized : transform.forward;
        HandleThrowDamage(hit.collider, hit.point, hitDir, wasCharged, throwChargeSystem);
    }

    // Aufgerufen via OverlapSphere-Fallback
    public void HandleThrowHitDirect(Collider col, Vector3 hitPoint, Vector3 hitDir,
        bool wasCharged, WeaponChargeSystem throwChargeSystem)
    {
        HandleThrowDamage(col, hitPoint, hitDir, wasCharged, throwChargeSystem);
    }

    private void HandleThrowDamage(Collider col, Vector3 hitPoint, Vector3 hitDir,
        bool wasCharged, WeaponChargeSystem throwChargeSystem)
    {
        float  vel           = rb != null ? rb.linearVelocity.magnitude : 0f;
        int    layer         = col.gameObject.layer;
        bool   enemyLayerHit = (enemyLayers.value & (1 << layer)) != 0;
        Health health        = col.GetComponentInParent<Health>();

        Debug.Log($"[WeaponSweepDamage] HandleThrowDamage — " +
                  $"vel:{vel:F2} (min:{minThrowDamageVelocity}) | " +
                  $"layer:{LayerMask.LayerToName(layer)}({layer}) | " +
                  $"enemyLayerHit:{enemyLayerHit} | health:{health != null} | " +
                  $"wasCharged:{wasCharged}");

        if (vel < minThrowDamageVelocity)
        {
            Debug.Log("[WeaponSweepDamage] ABORTED — velocity zu niedrig.");
            return;
        }
        if (!enemyLayerHit)
        {
            Debug.Log("[WeaponSweepDamage] ABORTED — Layer nicht in enemyLayers.");
            return;
        }
        if (health == null)
        {
            Debug.Log("[WeaponSweepDamage] ABORTED — kein Health Component.");
            return;
        }

        WeaponChargeSystem cs = throwChargeSystem != null ? throwChargeSystem : chargeSystem;
        float finalDamage     = wasCharged ? throwBaseDamage * throwChargedMultiplier : throwBaseDamage;

        TagReactionSystem reactions = col.GetComponentInParent<TagReactionSystem>();
        bool isExposed = reactions != null && reactions.ConsumeExposedModifier();
        if (isExposed) finalDamage *= reactions.ExposedMultiplier;

        Debug.Log($"[WeaponSweepDamage] THROW DAMAGE — base:{throwBaseDamage} " +
                  $"final:{finalDamage:F1} wasCharged:{wasCharged} exposed:{isExposed}");

        if (isExposed)
            health.TakeDamageReaction(finalDamage, hitDir, true);
        else
            health.TakeDamage(finalDamage, hitDir);

        if (wasCharged && cs != null)
        {
            cs.NotifyChargedHit(health, col, hitDir, hitPoint,
                throwBaseDamage, finalDamage);
            cs.ExpendCharge();
        }

        if (hitFeedback != null)
            hitFeedback.PlayHitFeedback(hitPoint, hitDir);
    }

    // ── Attack Window API ─────────────────────────────────────

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

    // ── Swing Internals ───────────────────────────────────────

    private void CheckTrack(Vector3 start, Vector3 end, Vector3 hitDir, float radius)
    {
        Vector3 delta    = end - start;
        float   distance = delta.magnitude;

        if (distance > 0.0001f)
        {
            RaycastHit[] hits = Physics.SphereCastAll(start, radius, delta.normalized,
                distance, enemyLayers, QueryTriggerInteraction.Collide);
            foreach (RaycastHit h in hits) TryDamage(h.collider, hitDir);

            CheckOverlap(start + delta * 0.25f, hitDir, radius);
            CheckOverlap(start + delta * 0.5f,  hitDir, radius);
            CheckOverlap(start + delta * 0.75f, hitDir, radius);
        }
        CheckOverlap(end, hitDir, radius);
    }

    private void CheckOverlap(Vector3 pos, Vector3 hitDir, float radius)
    {
        Collider[] cols = Physics.OverlapSphere(pos, radius, enemyLayers,
            QueryTriggerInteraction.Collide);
        foreach (Collider c in cols) TryDamage(c, hitDir);
    }

    private void TryDamage(Collider col, Vector3 hitDir)
    {
        if (col == null) return;
        Health health = col.GetComponentInParent<Health>();
        if (health == null) return;
        if (!hitThisWindow.Add(health)) return;

        bool  wasCharged  = chargeSystem != null && chargeSystem.IsCharged;
        float finalDamage = wasCharged ? damage * chargedDamageMultiplier : damage;

        TagReactionSystem reactions = col.GetComponentInParent<TagReactionSystem>();
        bool isExposed = reactions != null && reactions.ConsumeExposedModifier();
        if (isExposed) finalDamage *= reactions.ExposedMultiplier;

        if (isExposed)
            health.TakeDamageReaction(finalDamage, hitDir, true);
        else
            health.TakeDamage(finalDamage, hitDir);

        if (wasCharged && chargeSystem != null)
        {
            Vector3 hitPoint = col.ClosestPoint(transform.position);
            chargeSystem.NotifyChargedHit(health, col, hitDir, hitPoint, damage, finalDamage);
            chargeSystem.ExpendCharge();
        }

        if (hitFeedback != null)
            hitFeedback.PlayHitFeedback(col.ClosestPoint(transform.position), hitDir);

        Debug.Log($"[WeaponSweepDamage] SWING Hit {health.name} | " +
                  $"DMG:{finalDamage:F1} charged:{wasCharged} exposed:{isExposed}");
    }

    private void SnapLastPositions()
    {
        if (lastPositions == null) return;
        for (int i = 0; i < samplePoints.Length; i++)
            if (samplePoints[i] != null)
                lastPositions[i] = samplePoints[i].position;
    }
}