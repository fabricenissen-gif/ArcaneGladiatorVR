using System.Collections.Generic;
using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 20f;

    private HashSet<Health> hitTargets = new HashSet<Health>();
    private HashSet<Health> currentlyOverlapping = new HashSet<Health>();

    public void BeginSwing()
    {
        hitTargets.Clear();

        foreach (Health h in currentlyOverlapping)
        {
            hitTargets.Add(h);
        }

        Debug.Log("New swing started.");
    }

    private void OnTriggerEnter(Collider other)
    {
        Health health = other.GetComponent<Health>();
        if (health == null) return;

        currentlyOverlapping.Add(health);

        if (hitTargets.Contains(health)) return;

        hitTargets.Add(health);

        Vector3 hitDirection = (other.transform.position - transform.position).normalized;
        health.TakeDamage(damage, hitDirection);

        Debug.Log($"Hit {other.name} for {damage} damage.");
    }

    private void OnTriggerExit(Collider other)
    {
        Health health = other.GetComponent<Health>();
        if (health == null) return;

        currentlyOverlapping.Remove(health);
    }
}