using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public sealed class ToxicFumeCloud : MonoBehaviour
{
    [Header("Toxic Fumes")]
    [Tooltip(
        "Zeit zwischen reinen Duration-Refreshes für Ziele in der Wolke.")]
    [SerializeField, Min(0.05f)] private float refreshInterval = 1f;

    [Tooltip(
        "POISON-Dauer beim ersten Kontakt und bei späteren Refreshes.")]
    [SerializeField, Min(0.05f)] private float poisonDuration = 3f;

    [Tooltip("Temporäres Debug-Logging für die Toxic-Fumes-Wolke.")]
    [SerializeField] private bool logApplications;

    [Header("Visuals")]
    [SerializeField] private ParticleSystem cloudVfx;

    private readonly Dictionary<Health, float> nextRefreshTimes =
        new Dictionary<Health, float>();

    private readonly HashSet<Health> targetsThatReceivedInitialStack =
        new HashSet<Health>();

    private LayerMask enemyLayerMask;
    private TagApplicationContext reactionContext;
    private bool isInitialized;
    private float radius;

    public void Initialize(
        float lifetime,
        float radius,
        LayerMask enemyLayerMask,
        TagApplicationContext reactionContext)
    {
        float safeLifetime = Mathf.Max(0.05f, lifetime);

        this.radius = Mathf.Max(0.01f, radius);
        this.enemyLayerMask = enemyLayerMask;
        this.reactionContext = reactionContext;
        isInitialized = true;

        SphereCollider sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = this.radius;

        if (cloudVfx != null)
            cloudVfx.Play();

        Destroy(gameObject, safeLifetime);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isInitialized || other == null)
            return;

        if ((enemyLayerMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        Health health = other.GetComponentInParent<Health>();

        if (health == null || health.IsDead)
            return;

        TagHandler tagHandler = health.GetComponent<TagHandler>();

        if (tagHandler == null)
            return;

        float now = Time.time;

        if (nextRefreshTimes.TryGetValue(
                health,
                out float nextAllowedTime) &&
            now < nextAllowedTime)
        {
            return;
        }

        bool isFirstApplicationForThisCloud =
            targetsThatReceivedInitialStack.Add(health);

        ApplyOrRefreshPoison(
            health,
            tagHandler,
            isFirstApplicationForThisCloud);

        nextRefreshTimes[health] = now + refreshInterval;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null)
            return;

        Health health = other.GetComponentInParent<Health>();

        if (health == null)
            return;

        // Ein erneutes Betreten derselben Wolke darf keinen weiteren
        // POISON-Stack ergeben. Nur der Refresh-Cooldown wird aufgehoben.
        nextRefreshTimes.Remove(health);
    }

    private void ApplyOrRefreshPoison(
        Health health,
        TagHandler tagHandler,
        bool isFirstApplicationForThisCloud)
    {
        TagInstance existingPoison = tagHandler.GetTag(TagType.POISON);

        if (isFirstApplicationForThisCloud &&
            existingPoison == null)
        {
            tagHandler.ApplyTag(
                TagType.POISON,
                poisonDuration,
                reactionContext);

            LogApplication(
                health,
                "initial POISON stack applied");

            return;
        }

        if (existingPoison == null)
        {
            // POISON wurde nach dem initialen Kontakt beispielsweise
            // durch Venom Burst konsumiert. Die gleiche Wolke darf den
            // Stack nicht erneut anlegen.
            LogApplication(
                health,
                "POISON already consumed; no new stack applied");

            return;
        }

        tagHandler.RefreshTag(
            TagType.POISON,
            poisonDuration,
            reactionContext);

        LogApplication(
            health,
            "POISON duration refreshed");
    }

    private void LogApplication(
        Health health,
        string action)
    {
        if (!logApplications)
            return;

        Debug.Log(
            $"[ToxicFumeCloud] {health.name} -> {action} | " +
            $"duration:{poisonDuration:F2}s | " +
            $"refreshInterval:{refreshInterval:F2}s | " +
            $"source:{reactionContext.SourceCategory}:" +
            $"{reactionContext.SourceId}");
    }

    private void OnValidate()
    {
        refreshInterval = Mathf.Max(0.05f, refreshInterval);
        poisonDuration = Mathf.Max(0.05f, poisonDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}