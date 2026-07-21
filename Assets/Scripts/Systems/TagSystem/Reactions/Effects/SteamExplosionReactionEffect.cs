using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Effekt-Controller für FIRE + FROST.
///
/// Das Matching, die Priorität und der Tag-Konsum bleiben im
/// ReactionResolver. Diese Komponente führt ausschließlich den
/// bereits bestehenden Steam-Gameplay-Effekt aus.
/// </summary>
[DisallowMultipleComponent]
public sealed class SteamExplosionReactionEffect :
    MonoBehaviour,
    IReactionEffect
{
    [Header("Steam Explosion")]
    [SerializeField, Min(0f)] private float damage = 20f;

    [SerializeField, Min(0f)] private float radius = 3f;

    [SerializeField, Min(0f)] private float knockbackForce = 6f;

    [SerializeField] private LayerMask enemyLayerMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool logExecution = true;

    public ReactionEffectId EffectId =>
        ReactionEffectId.SteamExplosion;

    public bool Execute(ReactionContext context)
    {
        if (context.Health == null || context.Health.IsDead)
            return false;

        Collider[] hits = Physics.OverlapSphere(
            context.TargetTransform.position,
            radius,
            enemyLayerMask,
            QueryTriggerInteraction.Ignore);

        HashSet<Health> alreadyHit = new HashSet<Health>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (hit == null)
                continue;

            Health targetHealth = hit.GetComponentInParent<Health>();

            if (targetHealth == null ||
                targetHealth.IsDead ||
                !alreadyHit.Add(targetHealth))
            {
                continue;
            }

            Vector3 direction =
                targetHealth.transform.position -
                context.TargetTransform.position;

            direction = direction.sqrMagnitude <= 0.0001f
                ? Vector3.up
                : direction.normalized;

            targetHealth.TakeDamageReaction(damage, direction);

            Rigidbody targetRigidbody =
                targetHealth.GetComponent<Rigidbody>();

            if (targetRigidbody == null ||
                targetRigidbody.isKinematic)
            {
                continue;
            }

            Vector3 forceDirection =
                (direction + Vector3.up * 0.35f).normalized;

            targetRigidbody.AddForce(
                forceDirection * knockbackForce,
                ForceMode.Impulse);
        }

        if (logExecution)
        {
            Debug.Log(
                $"[SteamExplosionReactionEffect] Executed on " +
                $"'{context.TargetGameObject.name}' | " +
                $"damage:{damage:F1} | radius:{radius:F1} | " +
                $"knockback:{knockbackForce:F1}");
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}