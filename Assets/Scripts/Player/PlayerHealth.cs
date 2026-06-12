using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float damageCooldown = 0.25f;

    private int currentHealth;
    private float nextDamageTime;

    private void Start()
    {
        currentHealth = maxHealth;
        Debug.Log($"[PlayerHealth] Start health: {currentHealth}");
    }

    public void TakeDamage(int damage, Vector3 hitDirection)
    {
        if (Time.time < nextDamageTime)
        {
            Debug.Log("[PlayerHealth] Damage ignored because of cooldown.");
            return;
        }

        nextDamageTime = Time.time + damageCooldown;
        currentHealth -= damage;

        Debug.Log($"[PlayerHealth] Took {damage} damage. Current health: {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] Player died.");
    }
}