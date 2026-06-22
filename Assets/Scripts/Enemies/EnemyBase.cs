using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] protected float detectionRange  = 12f;
    [SerializeField] protected float loseTargetRange = 18f;
    [SerializeField] protected LayerMask playerLayer;

    [Header("Combat")]
    [SerializeField] protected float attackRange    = 1.5f;
    [SerializeField] protected float attackDamage   = 10f;
    [SerializeField] protected float attackCooldown = 1.5f;
    [SerializeField] protected float knockbackForce = 3f;

    [Header("Stun")]
    [SerializeField] protected float stunRecoveryDelay = 0.2f;

    protected NavMeshAgent agent;
    protected Health       health;
    protected Transform    player;

    protected EnemyState currentState      = EnemyState.Idle;
    protected float      attackCooldownTimer = 0f;
    protected bool       isStunned         = false;
    protected bool       isDead            = false;

    // ── Lifecycle ────────────────────────────────────────────

    protected virtual void Awake()
    {
        agent  = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
    }

    protected virtual void OnEnable()
    {
        if (health != null)
        {
            health.OnDeath   += HandleDeath;
            health.OnDamaged += HandleDamaged;
        }
    }

    protected virtual void OnDisable()
    {
        if (health != null)
        {
            health.OnDeath   -= HandleDeath;
            health.OnDamaged -= HandleDamaged;
        }
    }

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning($"[{gameObject.name}] Kein GameObject mit Tag 'Player' gefunden!");

        EnterState(EnemyState.Idle);
    }

    protected virtual void Update()
    {
        if (isDead || isStunned) return;
        attackCooldownTimer -= Time.deltaTime;
        UpdateState();
    }

    // ── State Machine ─────────────────────────────────────────

    protected virtual void EnterState(EnemyState newState)
    {
        currentState = newState;
        OnStateEnter(newState);
    }

    protected abstract void UpdateState();
    protected virtual void OnStateEnter(EnemyState state) { }

    // ── Helpers ───────────────────────────────────────────────

    protected float DistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }

    protected bool PlayerInRange(float range) => DistanceToPlayer() <= range;

    protected void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    // ── Angriff ───────────────────────────────────────────────

    protected virtual void PerformAttack()
    {
        if (player == null) return;
        attackCooldownTimer = attackCooldown;

        PlayerHealth playerHealth = player.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;

        Vector3 hitDir = (player.position - transform.position).normalized;
        playerHealth.TakeDamage((int)attackDamage, hitDir);

        Rigidbody playerRb = player.GetComponentInParent<Rigidbody>();
        if (playerRb != null && !playerRb.isKinematic)
            playerRb.AddForce(hitDir * knockbackForce, ForceMode.Impulse);

        Debug.Log($"[{gameObject.name}] Angriff auf Spieler — DMG:{attackDamage}");
    }

    // ── Schaden & Tod ─────────────────────────────────────────

    /// <summary>
    /// Wird von Health.cs via Event aufgerufen — ODER direkt via NotifyDamaged()
    /// falls Health noch keine Events hat.
    /// </summary>
    protected virtual void HandleDamaged(float damage, Vector3 direction) { }

    /// <summary>
    /// Public wrapper — wird von Health.cs aufgerufen wenn kein Event vorhanden.
    /// Nie direkt aus SubKlassen aufrufen.
    /// </summary>
    public void NotifyDamaged(float damage, Vector3 direction)
    {
        if (isDead) return;
        HandleDamaged(damage, direction);
    }

    protected virtual void HandleDeath()
    {
        isDead = true;
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled   = false;
        }
        EnterState(EnemyState.Dead);
        OnDeath();
    }

    protected virtual void OnDeath()
    {
        Destroy(gameObject, 1.5f);
    }

    // ── Stun ──────────────────────────────────────────────────

    public void ApplyStun(float duration)
    {
        if (isDead) return;
        StopAllCoroutines();
        StartCoroutine(StunRoutine(duration));
    }

    private System.Collections.IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        if (agent != null && agent.enabled) agent.isStopped = true;
        Debug.Log($"[{gameObject.name}] STUNNED für {duration}s");

        yield return new WaitForSeconds(duration);
        yield return new WaitForSeconds(stunRecoveryDelay);

        isStunned = false;
        if (agent != null && agent.enabled) agent.isStopped = false;
    }

    // ── Gizmos ────────────────────────────────────────────────

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }
}

public enum EnemyState
{
    Idle,
    Chase,
    Attack,
    Stunned,
    Dead
}