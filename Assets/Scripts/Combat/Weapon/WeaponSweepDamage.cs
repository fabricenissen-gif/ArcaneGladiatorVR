using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WeaponSweepDamage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] samplePoints;
    [SerializeField] private WeaponHitFeedback hitFeedback;
    [SerializeField] private WeaponChargeSystem chargeSystem;
    [SerializeField] private WeaponCombatEvents combatEvents;
    [SerializeField] private WeaponSwingDetector swingDetector;

    [Header("Hit Detection")]
    [SerializeField, Min(0.001f)] private float sampleRadius = 0.14f;
    [SerializeField, Min(0f)] private float bladeHalfWidth = 0.035f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Damage - Swing")]
    [SerializeField, Min(0f)] private float damage = 20f;
    [SerializeField, Min(0f)] private float chargedDamageMultiplier = 2f;

    [Header("Damage - Throw")]
    [SerializeField, Min(0f)] private float throwBaseDamage = 25f;
    [SerializeField, Min(0f)] private float throwChargedMultiplier = 2.5f;
    [SerializeField, Min(0f)] private float minThrowDamageVelocity = 4f;

    [Header("Attack Window")]
    [SerializeField] private bool attackWindowActive;

    [HideInInspector] public bool isFlying;

    private readonly HashSet<Health> hitThisWindow = new HashSet<Health>();

    private Rigidbody rb;
    private Vector3[] lastPositions;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (chargeSystem == null)
            chargeSystem = GetComponent<WeaponChargeSystem>();

        if (combatEvents == null)
            combatEvents = GetComponent<WeaponCombatEvents>();

        if (swingDetector == null)
            swingDetector = GetComponent<WeaponSwingDetector>();

        if (combatEvents == null)
        {
            Debug.LogWarning(
                $"[WeaponSweepDamage] '{name}' has no WeaponCombatEvents component. " +
                "Damage still works, but ON_HIT and ON_THROW_HIT are not published.");
        }
    }

    private void Start()
    {
        if (samplePoints == null || samplePoints.Length == 0)
        {
            Debug.LogWarning(
                $"[WeaponSweepDamage] '{name}' has no Sample Points. " +
                "Melee hit detection was disabled.");

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
            Transform samplePoint = samplePoints[i];

            if (samplePoint == null)
                continue;

            Vector3 currentPosition = samplePoint.position;
            Vector3 previousPosition = lastPositions[i];
            Vector3 delta = currentPosition - previousPosition;

            Vector3 hitDirection = delta.sqrMagnitude > 0.0001f
                ? delta.normalized
                : transform.forward;

            Vector3 bladeAxis = samplePoint.right;

            CheckTrack(
                previousPosition,
                currentPosition,
                hitDirection,
                sampleRadius);

            CheckTrack(
                previousPosition - bladeAxis * bladeHalfWidth,
                currentPosition - bladeAxis * bladeHalfWidth,
                hitDirection,
                sampleRadius);

            CheckTrack(
                previousPosition + bladeAxis * bladeHalfWidth,
                currentPosition + bladeAxis * bladeHalfWidth,
                hitDirection,
                sampleRadius);

            lastPositions[i] = currentPosition;
        }
    }

    // ── Throw Damage ──────────────────────────────────────────

    public void HandleThrowHit(
        RaycastHit hit,
        bool wasCharged,
        WeaponChargeSystem throwChargeSystem)
    {
        Vector3 hitDirection = rb != null &&
                               rb.linearVelocity.sqrMagnitude > 0.001f
            ? rb.linearVelocity.normalized
            : transform.forward;

        HandleThrowDamage(
            hit.collider,
            hit.point,
            hitDirection,
            wasCharged,
            throwChargeSystem);
    }

    public void HandleThrowHitDirect(
        Collider collider,
        Vector3 hitPoint,
        Vector3 hitDirection,
        bool wasCharged,
        WeaponChargeSystem throwChargeSystem)
    {
        HandleThrowDamage(
            collider,
            hitPoint,
            hitDirection,
            wasCharged,
            throwChargeSystem);
    }

    private void HandleThrowDamage(
        Collider collider,
        Vector3 hitPoint,
        Vector3 hitDirection,
        bool wasCharged,
        WeaponChargeSystem throwChargeSystem)
    {
        if (collider == null)
            return;

        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;

        if (speed < minThrowDamageVelocity)
            return;

        if (!IsEnemyLayer(collider.gameObject.layer))
            return;

        Health health = collider.GetComponentInParent<Health>();

        if (health == null || health.IsDead)
            return;

        WeaponChargeSystem activeChargeSystem =
            throwChargeSystem != null
                ? throwChargeSystem
                : chargeSystem;

        float finalDamage = wasCharged
            ? throwBaseDamage * throwChargedMultiplier
            : throwBaseDamage;

        TagReactionSystem reactions =
            collider.GetComponentInParent<TagReactionSystem>();

        bool isExposed =
            reactions != null &&
            reactions.ConsumeExposedModifier();

        if (isExposed)
            finalDamage *= reactions.ExposedMultiplier;

        combatEvents?.RaiseThrowHit(
            health,
            collider,
            hitPoint,
            hitDirection,
            finalDamage,
            speed,
            wasCharged);

        if (isExposed)
            health.TakeDamageReaction(finalDamage, hitDirection, true);
        else
            health.TakeDamage(finalDamage, hitDirection);

        if (wasCharged && activeChargeSystem != null)
        {
            activeChargeSystem.NotifyChargedHit(
                health,
                collider,
                hitDirection,
                hitPoint,
                throwBaseDamage,
                finalDamage);

            activeChargeSystem.ExpendCharge();
        }

        hitFeedback?.PlayHitFeedback(hitPoint, hitDirection);
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

    // ── Swing Hit Detection ───────────────────────────────────

    private void CheckTrack(
        Vector3 start,
        Vector3 end,
        Vector3 hitDirection,
        float radius)
    {
        Vector3 delta = end - start;
        float distance = delta.magnitude;

        if (distance > 0.0001f)
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                start,
                radius,
                delta.normalized,
                distance,
                enemyLayers,
                QueryTriggerInteraction.Collide);

            foreach (RaycastHit hit in hits)
                TryDamage(hit.collider, hitDirection);

            CheckOverlap(
                start + delta * 0.25f,
                hitDirection,
                radius);

            CheckOverlap(
                start + delta * 0.5f,
                hitDirection,
                radius);

            CheckOverlap(
                start + delta * 0.75f,
                hitDirection,
                radius);
        }

        CheckOverlap(end, hitDirection, radius);
    }

    private void CheckOverlap(
        Vector3 position,
        Vector3 hitDirection,
        float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(
            position,
            radius,
            enemyLayers,
            QueryTriggerInteraction.Collide);

        foreach (Collider collider in colliders)
            TryDamage(collider, hitDirection);
    }

    private void TryDamage(Collider collider, Vector3 hitDirection)
    {
        if (collider == null)
            return;

        Health health = collider.GetComponentInParent<Health>();

        if (health == null || health.IsDead)
            return;

        if (!hitThisWindow.Add(health))
            return;

        bool wasCharged = chargeSystem != null && chargeSystem.IsCharged;

        float finalDamage = wasCharged
            ? damage * chargedDamageMultiplier
            : damage;

        TagReactionSystem reactions =
            collider.GetComponentInParent<TagReactionSystem>();

        bool isExposed =
            reactions != null &&
            reactions.ConsumeExposedModifier();

        if (isExposed)
            finalDamage *= reactions.ExposedMultiplier;

        Vector3 hitPoint = collider.ClosestPoint(transform.position);

        float swingSpeed = swingDetector != null
            ? swingDetector.CurrentSwingSpeed
            : 0f;

        int swingId = swingDetector != null
            ? swingDetector.CurrentSwingId
            : 0;

        combatEvents?.RaiseHit(
            health,
            collider,
            hitPoint,
            hitDirection,
            finalDamage,
            swingSpeed,
            wasCharged,
            swingId);

        if (isExposed)
            health.TakeDamageReaction(finalDamage, hitDirection, true);
        else
            health.TakeDamage(finalDamage, hitDirection);

        if (wasCharged && chargeSystem != null)
        {
            chargeSystem.NotifyChargedHit(
                health,
                collider,
                hitDirection,
                hitPoint,
                damage,
                finalDamage);

            chargeSystem.ExpendCharge();
        }

        hitFeedback?.PlayHitFeedback(hitPoint, hitDirection);

        Debug.Log(
            $"[WeaponSweepDamage] SWING Hit {health.name} | " +
            $"DMG:{finalDamage:F1} charged:{wasCharged} exposed:{isExposed}");
    }

    private bool IsEnemyLayer(int layer)
    {
        return (enemyLayers.value & (1 << layer)) != 0;
    }

    private void SnapLastPositions()
    {
        if (lastPositions == null)
            return;

        for (int i = 0; i < samplePoints.Length; i++)
        {
            if (samplePoints[i] != null)
                lastPositions[i] = samplePoints[i].position;
        }
    }
}