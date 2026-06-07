using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 20f;

    private void OnTriggerEnter(Collider other)
    {
        Health health = other.GetComponent<Health>();

        if (health != null)
        {
            health.TakeDamage(damage);
            Debug.Log($"Hit {other.name} for {damage} damage.");
        }
    }
}