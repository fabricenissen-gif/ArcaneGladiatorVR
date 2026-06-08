using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private HitWobble hitWobble;
    private HitReaction hitReaction;
    private EnemyHealthBar healthBar;
    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        hitWobble = GetComponent<HitWobble>();
        hitReaction = GetComponent<HitReaction>();
        healthBar = GetComponentInChildren<EnemyHealthBar>();

        UpdateHealthBar();
    }

    public void TakeDamage(float amount, Vector3 hitDirection)
    {
        if (isDead)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (hitWobble != null)
            hitWobble.PlayWobble(hitDirection);

        if (hitReaction != null)
            hitReaction.PlayReaction(hitDirection);

        UpdateHealthBar();

        Debug.Log($"{gameObject.name} took {amount} damage. HP left: {currentHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.SetNormalized(currentHealth / maxHealth);
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} died.");
        Destroy(gameObject);
    }
}