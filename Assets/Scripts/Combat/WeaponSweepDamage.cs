using System.Collections.Generic;
using UnityEngine;

public class WeaponSweepDamage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] samplePoints;

    [Header("Hit Detection")]
    [SerializeField] private float sampleRadius = 0.14f;
    [SerializeField] private float bladeHalfWidth = 0.035f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Damage")]
    [SerializeField] private float damage = 20f;

    [Header("Attack Window")]
    [SerializeField] private bool attackWindowActive = false;

    private Vector3[] lastPositionsCenter;
    private readonly HashSet<Health> hitThisWindow = new HashSet<Health>();

    private void Start()
    {
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
        Physics.SyncTransforms();

        if (!attackWindowActive)
        {
            UpdateLastPositions();
            return;
        }

        for (int i = 0; i < samplePoints.Length; i++)
        {
            Transform point = samplePoints[i];
            if (point == null) continue;

            Vector3 currentCenter = point.position;
            Vector3 previousCenter = lastPositionsCenter[i];
            Vector3 delta = currentCenter - previousCenter;

            Vector3 hitDirection = delta.sqrMagnitude > 0.0001f
                ? delta.normalized
                : transform.forward;

            Vector3 bladeWidthDirection = point.right;

            CheckTrack(previousCenter, currentCenter, hitDirection, sampleRadius);
            CheckTrack(
                previousCenter - bladeWidthDirection * bladeHalfWidth,
                currentCenter - bladeWidthDirection * bladeHalfWidth,
                hitDirection,
                sampleRadius
            );
            CheckTrack(
                previousCenter + bladeWidthDirection * bladeHalfWidth,
                currentCenter + bladeWidthDirection * bladeHalfWidth,
                hitDirection,
                sampleRadius
            );

            lastPositionsCenter[i] = currentCenter;
        }
    }

    public void BeginAttackWindow()
    {
        attackWindowActive = true;
        hitThisWindow.Clear();
        UpdateLastPositions();

        Debug.Log("Attack window started.");
    }

    public void EndAttackWindow()
    {
        attackWindowActive = false;
        hitThisWindow.Clear();
        UpdateLastPositions();

        Debug.Log("Attack window ended.");
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
        if (hitThisWindow.Contains(health)) return;

        hitThisWindow.Add(health);
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