using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ToxicFumeCloud : MonoBehaviour
{
    [SerializeField] private float tickInterval = 0.75f;
    [SerializeField] private float poisonDuration = 3f;
    [SerializeField] private ParticleSystem cloudVfx;

    private float lifetime;
    private float radius;
    private LayerMask enemyLayerMask;
    private readonly Dictionary<Health, float> nextTickTime = new Dictionary<Health, float>();

    public void Initialize(float lifetime, float radius, LayerMask enemyLayerMask)
    {
        this.lifetime = lifetime;
        this.radius = radius;
        this.enemyLayerMask = enemyLayerMask;

        SphereCollider sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = radius;

        if (cloudVfx != null)
            cloudVfx.Play();

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerStay(Collider other)
    {
        if ((enemyLayerMask.value & (1 << other.gameObject.layer)) == 0) return;

        Health health = other.GetComponentInParent<Health>();
        if (health == null) return;

        if (!nextTickTime.TryGetValue(health, out float nextAllowedTime))
            nextAllowedTime = 0f;

        if (Time.time < nextAllowedTime) return;

        TagHandler handler = health.GetComponent<TagHandler>();
        handler?.ApplyTag(TagType.POISON, poisonDuration);
        nextTickTime[health] = Time.time + tickInterval;
    }
}