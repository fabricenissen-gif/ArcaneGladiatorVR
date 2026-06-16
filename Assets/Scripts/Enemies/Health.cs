using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float deathDelay = 1.2f;
    [SerializeField] private GameObject healthBarRoot;

    private float currentHealth;

    private HitWobble hitWobble;
    private HitReaction hitReaction;
    private EnemyHealthBar healthBar;
    private Collider[] allColliders;
    private SimpleEnemyChase simpleEnemyChase;
    private SwarmerAI swarmerAI; // Neu: Referenz für den Swarmer
    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;

        hitWobble = GetComponent<HitWobble>();
        hitReaction = GetComponent<HitReaction>();
        healthBar = GetComponentInChildren<EnemyHealthBar>();
        allColliders = GetComponentsInChildren<Collider>();
        simpleEnemyChase = GetComponent<SimpleEnemyChase>();
        swarmerAI = GetComponent<SwarmerAI>(); // Sucht auch nach dem neuen Swarmer

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

        if (simpleEnemyChase != null)
            simpleEnemyChase.NotifyHit();
            
        // Optional: Wenn du willst, dass der Swarmer reagiert, 
        // könntest du hier auch Methoden in SwarmerAI aufrufen.

        UpdateHealthBar();

        Debug.Log($"{gameObject.name} took {amount} damage. HP left: {currentHealth}");

        if (currentHealth <= 0f)
        {
            StartDeath(hitDirection);
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.SetNormalized(currentHealth / maxHealth);
    }

    private void StartDeath(Vector3 hitDirection)
    {
        if (isDead)
            return;

        isDead = true;

        if (hitWobble != null)
        {
            hitWobble.ResetToRestPose(); 
        }

        // --- WICHTIGE ÄNDERUNG ---
        // Wenn wir die Collider beim Tod ausschalten, fallen Schwerter, 
        // die NICHT im Gegner stecken (z.B. die ihn nur streifen), durch ihn hindurch.
        // Das ist meistens gewünscht.
        foreach (Collider col in allColliders)
        {
            col.enabled = false;
        }

        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(false);
        }
        
        // Die AI-Skripte deaktivieren, damit er aufhört anzugreifen/zu fliegen
        if (simpleEnemyChase != null) simpleEnemyChase.enabled = false;
        if (swarmerAI != null) swarmerAI.enabled = false;

        StartCoroutine(DeathRoutine(hitDirection));
    }

    private IEnumerator DeathRoutine(Vector3 hitDirection)
    {
        Debug.Log($"{gameObject.name} died.");

        Transform t = transform;
        Vector3 originalScale = t.localScale;
        Vector3 deathScale = new Vector3(originalScale.x * 0.85f, originalScale.y * 0.6f, originalScale.z * 0.85f);

        float duration = deathDelay;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t01 = elapsed / duration;

            transform.localScale = Vector3.Lerp(originalScale, deathScale, t01);
            yield return null;
        }

        // --- NEU: WAFFEN ABWERFEN ---
        // Bevor das Objekt zerstört wird, suchen wir in allen Children nach einem steckenden Schwert.
        // Das ist wichtig, da das Schwert sonst mit dem Gegner gelöscht wird.
        WeaponThrowAssist[] stuckWeapons = GetComponentsInChildren<WeaponThrowAssist>();
        foreach (var weapon in stuckWeapons)
        {
            weapon.ForceUnstick();
        }

        // Jetzt können wir den toten Gegner sicher zerstören
        Destroy(gameObject);
    }
}