using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private HitWobble hitWobble;
    private HitReaction hitReaction;

    private void Awake()
    {
        currentHealth = maxHealth;
        hitWobble = GetComponent<HitWobble>();
        hitReaction = GetComponent<HitReaction>();
    }

    public void TakeDamage(float amount, Vector3 hitDirection)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (hitWobble != null)
            hitWobble.PlayWobble(hitDirection);

        if (hitReaction != null)
            hitReaction.PlayReaction(hitDirection);

        Debug.Log($"{gameObject.name} took {amount} damage. HP left: {currentHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died.");
        Destroy(gameObject);
    }
}