using System.Collections.Generic;
using UnityEngine;

public class WeaponSweepDamage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponSwingDetector weaponSwingDetector;
    [SerializeField] private Transform[] samplePoints;

    [Header("Hit Detection")]
    [SerializeField] private float sampleRadius = 0.14f;
    [SerializeField] private float bladeHalfWidth = 0.035f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float minSwingSpeed = 0.7f;

    [Header("Tip Tuning")]
    [SerializeField] private int tipPointCount = 2;
    [SerializeField] private float tipRadiusMultiplier = 1.35f;
    [SerializeField] private float tipForwardOffset = 0.03f;

    [Header("Damage")]
    [SerializeField] private float damage = 20f;

    private Vector3[] lastPositionsCenter;
    private readonly HashSet<Health> hitThisSwing = new HashSet<Health>();
    private int processedSwingId = -1;

    private void Start()
    {
        if (weaponSwingDetector == null)
            weaponSwingDetector = GetComponentInParent<WeaponSwingDetector>();

        if (samplePoints == null || samplePoints.Length == 0)
        {
            Debug.LogWarning("WeaponSweepDamage: no sample points assigned.");
            enabled = false;
            return;
        }

        lastPositionsCenter = new Vector3[samplePoints.Length];
        UpdateLastPositions();
    }

    private void Update()
    {
        if (weaponSwingDetector == null || samplePoints == null || samplePoints.Length == 0)
            return;

        Physics.SyncTransforms();

        bool isSwinging = weaponSwingDetector.IsSwinging;
        float swingSpeed = weaponSwingDetector.CurrentSwingSpeed;
        int currentSwingId = weaponSwingDetector.CurrentSwingId;

        if (!isSwinging || swingSpeed < minSwingSpeed)
        {
            UpdateLastPositions();
            return;
        }

        if (processedSwingId != currentSwingId)
        {
            processedSwingId = currentSwingId;
            hitThisSwing.Clear();
        }

        for (int i = 0; i < samplePoints.Length; i++)
        {
            Transform point = samplePoints[i];
            if (point == null) continue;

            bool isTipPoint = i >= samplePoints.Length - tipPointCount;
            float currentRadius = isTipPoint ? sampleRadius * tipRadiusMultiplier : sampleRadius;

            Vector3 currentCenter = point.position;
            Vector3 previousCenter = lastPositionsCenter[i];
            Vector3 delta = currentCenter - previousCenter;

            Vector3 swingDirection = delta.sqrMagnitude > 0.0001f
                ? delta.normalized
                : weaponSwingDetector.CurrentSwingDirection;

            Vector3 bladeWidthDirection = point.right;

            CheckTrack(previousCenter, currentCenter, swingDirection, currentRadius);
            CheckTrack(previousCenter - bladeWidthDirection * bladeHalfWidth,
                       currentCenter - bladeWidthDirection * bladeHalfWidth,
                       swingDirection, currentRadius);
            CheckTrack(previousCenter + bladeWidthDirection * bladeHalfWidth,
                       currentCenter + bladeWidthDirection * bladeHalfWidth,
                       swingDirection, currentRadius);

            if (isTipPoint)
            {
                Vector3 tipForward = point.forward * tipForwardOffset;

                CheckTrack(previousCenter + tipForward,
                           currentCenter + tipForward,
                           swingDirection, currentRadius);
            }

            lastPositionsCenter[i] = currentCenter;
        }
    }

    private void CheckTrack(Vector3 start, Vector3 end, Vector3 hitDirection, float radius)
    {
        Vector3 delta = end - start;
        float distance = delta.magnitude;

        if (distance > 0.0001f)
        {
            Vector3 direction = delta.normalized;

            RaycastHit[] sweepHits = Physics.SphereCastAll(
                start,
                radius,
                direction,
                distance,
                enemyLayers,
                QueryTriggerInteraction.Collide
            );

            for (int h = 0; h < sweepHits.Length; h++)
            {
                TryDamageCollider(sweepHits[h].collider, hitDirection);
            }

            CheckOverlapAtPosition(start + delta * 0.25f, hitDirection, radius);
            CheckOverlapAtPosition(start + delta * 0.5f, hitDirection, radius);
            CheckOverlapAtPosition(start + delta * 0.75f, hitDirection, radius);
        }

        CheckOverlapAtPosition(end, hitDirection, radius);
    }

    private void CheckOverlapAtPosition(Vector3 position, Vector3 hitDirection, float radius)
    {
        Collider[] overlapHits = Physics.OverlapSphere(
            position,
            radius,
            enemyLayers,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < overlapHits.Length; i++)
        {
            TryDamageCollider(overlapHits[i], hitDirection);
        }
    }

    private void TryDamageCollider(Collider hitCollider, Vector3 hitDirection)
    {
        if (hitCollider == null) return;

        Health health = hitCollider.GetComponentInParent<Health>();
        if (health == null) return;
        if (hitThisSwing.Contains(health)) return;

        hitThisSwing.Add(health);
        health.TakeDamage(damage, hitDirection);

        Debug.Log($"Hit {health.name} for {damage} damage.");
    }

    private void UpdateLastPositions()
    {
        for (int i = 0; i < samplePoints.Length; i++)
        {
            if (samplePoints[i] != null)
                lastPositionsCenter[i] = samplePoints[i].position;
        }
    }
}