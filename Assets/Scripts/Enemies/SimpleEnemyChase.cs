using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SimpleEnemyChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform attackPoint;

    [Header("Chase Settings")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float stopDistance = 1.8f;
    [SerializeField] private float repathInterval = 0.1f;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2.1f;
    [SerializeField] private float attackRadius = 0.45f;
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private float attackWindup = 0.18f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float fallbackAttackForwardOffset = 1.0f;
    [SerializeField] private float fallbackAttackUpOffset = 1.0f;

    [Header("Hit Pause")]
    [SerializeField] private float hitPauseDuration = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private float repathTimer;
    private bool isTemporarilyPaused;
    private Coroutine hitPauseRoutine;

    private bool isAttacking;
    private bool hitAppliedThisAttack;
    private float nextAttackTime;
    private Coroutine attackRoutine;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent != null)
            agent.stoppingDistance = stopDistance;
    }

    private void Update()
    {
        if (agent == null || target == null)
        {
            Log("Missing agent or target reference.");
            return;
        }

        if (!agent.enabled || isTemporarilyPaused)
            return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget > detectionRange)
        {
            if (agent.hasPath)
            {
                agent.ResetPath();
                Log("Target outside detection range. Resetting path.");
            }

            return;
        }

        if (isAttacking)
        {
            if (agent.hasPath)
                agent.ResetPath();

            FaceTarget();
            return;
        }

        if (distanceToTarget <= attackRange)
        {
            if (agent.hasPath)
                agent.ResetPath();

            FaceTarget();

            if (Time.time >= nextAttackTime)
            {
                Log($"Starting attack. Distance to target: {distanceToTarget:F2}");

                if (attackRoutine != null)
                    StopCoroutine(attackRoutine);

                attackRoutine = StartCoroutine(AttackRoutine());
            }

            return;
        }

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            agent.stoppingDistance = stopDistance;
            agent.SetDestination(target.position);
        }

        if (distanceToTarget <= stopDistance)
        {
            agent.ResetPath();
            Log("Inside stop distance. Resetting path.");
        }

        FaceTarget();
    }

    private void FaceTarget()
    {
        if (target == null || isTemporarilyPaused)
            return;

        Vector3 lookDirection = target.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 6f);
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        hitAppliedThisAttack = false;
        nextAttackTime = Time.time + attackCooldown;

        Log("Enemy attack started.");

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        yield return new WaitForSeconds(attackWindup);

        Log("Enemy attack hit check executed.");
        ApplyAttackHit();

        yield return new WaitForSeconds(0.12f);

        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
        }

        isAttacking = false;
        hitAppliedThisAttack = false;
        attackRoutine = null;

        Log("Enemy attack finished.");
    }

    private void ApplyAttackHit()
    {
        if (!isAttacking)
        {
            Log("ApplyAttackHit aborted: enemy is not attacking.");
            return;
        }

        if (hitAppliedThisAttack)
        {
            Log("ApplyAttackHit aborted: hit was already applied this attack.");
            return;
        }

        Vector3 attackCenter = GetAttackCenter();

        Collider[] hits = Physics.OverlapSphere(
            attackCenter,
            attackRadius,
            playerLayer,
            QueryTriggerInteraction.Collide
        );

        Log("Attack overlaps found: " + hits.Length);

        for (int i = 0; i < hits.Length; i++)
        {
            Log("Overlap hit collider: " + hits[i].name);

            PlayerHealth playerHealth = hits[i].GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                Log("Collider has no PlayerHealth in parent chain: " + hits[i].name);
                continue;
            }

            Vector3 hitDirection = (target.position - transform.position).normalized;
            playerHealth.TakeDamage(attackDamage, hitDirection);

            hitAppliedThisAttack = true;
            Log("Enemy successfully damaged player.");
            break;
        }

        if (!hitAppliedThisAttack)
        {
            Log("Attack hit check finished, but no valid player target was damaged.");
        }
    }

    private Vector3 GetAttackCenter()
    {
        if (attackPoint != null)
            return attackPoint.position;

        Vector3 fallbackCenter =
            transform.position +
            transform.forward * fallbackAttackForwardOffset +
            Vector3.up * fallbackAttackUpOffset;

        Log("AttackPoint missing. Using fallback attack center.");

        return fallbackCenter;
    }

    public void NotifyHit()
    {
        Log("Enemy received NotifyHit.");

        if (hitPauseRoutine != null)
            StopCoroutine(hitPauseRoutine);

        hitPauseRoutine = StartCoroutine(HitPauseRoutine());
    }

    private IEnumerator HitPauseRoutine()
    {
        isTemporarilyPaused = true;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
            Log("Attack interrupted by hit pause.");
        }

        isAttacking = false;
        hitAppliedThisAttack = false;

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        yield return new WaitForSeconds(hitPauseDuration);

        if (agent != null && agent.enabled)
        {
            agent.Warp(transform.position);
            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }

        isTemporarilyPaused = false;
        hitPauseRoutine = null;

        Log("Hit pause finished.");
    }

    private void Log(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.Log($"[{name} / SimpleEnemyChase] {message}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 attackCenter;

        if (attackPoint != null)
            attackCenter = attackPoint.position;
        else
            attackCenter =
                transform.position +
                transform.forward * fallbackAttackForwardOffset +
                Vector3.up * fallbackAttackUpOffset;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(attackCenter, attackRadius);
    }
}