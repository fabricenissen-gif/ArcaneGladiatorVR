using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SimpleEnemyChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private NavMeshAgent agent;

    [Header("Chase Settings")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float stopDistance = 1.8f;
    [SerializeField] private float repathInterval = 0.1f;

    [Header("Hit Pause")]
    [SerializeField] private float hitPauseDuration = 0.15f;

    private float repathTimer;
    private bool isTemporarilyPaused;
    private Coroutine hitPauseRoutine;

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
            return;

        if (!agent.enabled || isTemporarilyPaused)
            return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget > detectionRange)
        {
            if (agent.hasPath)
                agent.ResetPath();

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

    public void NotifyHit()
    {
        if (hitPauseRoutine != null)
            StopCoroutine(hitPauseRoutine);

        hitPauseRoutine = StartCoroutine(HitPauseRoutine());
    }

    private IEnumerator HitPauseRoutine()
    {
        isTemporarilyPaused = true;

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
    }
}