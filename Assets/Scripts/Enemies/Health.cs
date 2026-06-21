using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float deathDelay = 1.2f;
    [SerializeField] private GameObject healthBarRoot;

    [Header("MARKED Passiv")]
    [Tooltip("Schadensverstärkung wenn Gegner MARKED ist (z.B. 0.25 = +25%)")]
    [SerializeField] private float markedDamageAmp = 0.25f;

    private float currentHealth;
    private bool isDead;

    private HitWobble hitWobble;
    private HitReaction hitReaction;
    private EnemyHealthBar healthBar;
    private Collider[] allColliders;
    private SimpleEnemyChase simpleEnemyChase;
    private SwarmerAI swarmerAI;
    private TagHandler tagHandler;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth    = maxHealth;
        hitWobble        = GetComponent<HitWobble>();
        hitReaction      = GetComponent<HitReaction>();
        healthBar        = GetComponentInChildren<EnemyHealthBar>();
        allColliders     = GetComponentsInChildren<Collider>();
        simpleEnemyChase = GetComponent<SimpleEnemyChase>();
        swarmerAI        = GetComponent<SwarmerAI>();
        tagHandler       = GetComponent<TagHandler>();
        UpdateHealthBar();
    }

    public void TakeDamage(float amount, Vector3 hitDirection)
        => TakeDamageInternal(amount, hitDirection, TagType.NONE, false, false);

    public void TakeDamageTagged(float amount, Vector3 hitDirection, TagType tag)
        => TakeDamageInternal(amount, hitDirection, tag, false, false);

    public void TakeDamageReaction(float amount, Vector3 hitDirection, bool isExposed = false)
        => TakeDamageInternal(amount, hitDirection, TagType.NONE, true, isExposed);

    private void TakeDamageInternal(float amount, Vector3 hitDirection,
                                     TagType tag, bool isReaction, bool isExposed)
    {
        if (isDead) return;

        // MARKED Passiv: +markedDamageAmp% auf jeden Schaden
        // Ausnahme: Exposed hat eigenen Multiplier — kein doppeltes Stapeln
        bool isMarked = !isExposed && tagHandler != null && tagHandler.HasTag(TagType.MARKED);
        if (isMarked)
            amount *= (1f + markedDamageAmp);

        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        SpawnDamageNumber(amount, tag, isReaction, isExposed);

        if (hitWobble != null)        hitWobble.PlayWobble(hitDirection);
        if (hitReaction != null)      hitReaction.PlayReaction(hitDirection);
        if (simpleEnemyChase != null) simpleEnemyChase.NotifyHit();

        UpdateHealthBar();

        Debug.Log($"{gameObject.name} took {amount:F1} | HP:{currentHealth:F1} | tag:{tag} reaction:{isReaction} exposed:{isExposed} marked:{isMarked}");

        if (currentHealth <= 0f)
            StartDeath(hitDirection);
    }

    private void SpawnDamageNumber(float amount, TagType tag, bool isReaction, bool isExposed)
    {
        if (DamageNumberSpawner.Instance == null) return;
        Vector3 pos = transform.position;
        if (isReaction)
            DamageNumberSpawner.Instance.SpawnReaction(pos, amount, isExposed);
        else if (tag != TagType.NONE)
            DamageNumberSpawner.Instance.SpawnTagged(pos, amount, tag);
        else
            DamageNumberSpawner.Instance.Spawn(pos, amount);
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.SetNormalized(currentHealth / maxHealth);
    }

    private void StartDeath(Vector3 hitDirection)
    {
        if (isDead) return;
        isDead = true;

        if (hitWobble != null) hitWobble.ResetToRestPose();
        foreach (Collider col in allColliders) col.enabled = false;
        if (healthBarRoot != null) healthBarRoot.SetActive(false);
        if (simpleEnemyChase != null) simpleEnemyChase.enabled = false;
        if (swarmerAI != null)        swarmerAI.enabled = false;

        StartCoroutine(DeathRoutine(hitDirection));
    }

    private IEnumerator DeathRoutine(Vector3 hitDirection)
    {
        Debug.Log($"{gameObject.name} died.");
        Vector3 originalScale = transform.localScale;
        Vector3 deathScale    = new Vector3(originalScale.x * 0.85f,
                                            originalScale.y * 0.6f,
                                            originalScale.z * 0.85f);
        float elapsed = 0f;
        while (elapsed < deathDelay)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, deathScale, elapsed / deathDelay);
            yield return null;
        }

        WeaponThrowAssist[] stuck = GetComponentsInChildren<WeaponThrowAssist>();
        foreach (var w in stuck) w.ForceUnstick();
        Destroy(gameObject);
    }
}