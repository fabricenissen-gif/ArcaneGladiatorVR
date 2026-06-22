using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Health))]
public class SwarmerAI : EnemyBase
{
    private enum SwarmState { Chase, Orbit, Telegraph, Dive }

    [Header("Swarmer — Movement")]
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float orbitSpeed = 2f;
    [SerializeField] private float orbitDistance = 1.2f;

    [Header("Swarmer — Height")]
    [SerializeField] private float hoverHeight = 1.6f;

    [Header("Swarmer — Dive Attack")]
    [SerializeField] private float diveSpeed = 8f;
    [SerializeField] private float telegraphDuration = 0.7f;

    [Header("Swarmer — Visual")]
    [SerializeField] private MeshRenderer enemyRenderer;

    private SwarmState swarmState = SwarmState.Chase;

    private float lastAttackTime;
    private float currentCooldown;

    private float orbitAngle;
    private int orbitDirection = 1;

    private static float globalLastAttackTime = -999f;
    private static float globalAttackSpacing = 2.5f;

    private Vector3 diveTargetPosition;
    private Material originalMaterial;

    protected override void Start()
    {
        base.Start();

        if (enemyRenderer != null)
            originalMaterial = enemyRenderer.material;

        orbitDirection = Random.value > 0.5f ? 1 : -1;
        orbitAngle = Random.Range(0f, 360f);

        currentCooldown = Random.Range(attackCooldown, attackCooldown * 2f);
        lastAttackTime = Time.time + Random.Range(0f, 2f);
        swarmState = SwarmState.Chase;
    }

    protected override void UpdateState()
    {
        if (player == null) return;

        float dist = DistanceToPlayer();

        switch (swarmState)
        {
            case SwarmState.Chase:     HandleChase(dist); break;
            case SwarmState.Orbit:     HandleOrbit(dist); break;
            case SwarmState.Telegraph: break;
            case SwarmState.Dive:      HandleDive();      break;
        }

        if (swarmState != SwarmState.Dive)
            FacePlayer();
    }

    private Vector3 GetHoverTarget()
    {
        return new Vector3(player.position.x, player.position.y + hoverHeight, player.position.z);
    }

    private void HandleChase(float dist)
    {
        if (dist <= orbitDistance)
        {
            swarmState = SwarmState.Orbit;
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position, GetHoverTarget(), chaseSpeed * Time.deltaTime);
    }

    private void HandleOrbit(float dist)
    {
        if (dist > orbitDistance * 2f)
        {
            swarmState = SwarmState.Chase;
            return;
        }

        orbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;
        float hover = Mathf.Sin(Time.time * 3f) * 0.2f;
        Vector3 offset = new Vector3(
            Mathf.Sin(orbitAngle), hover, Mathf.Cos(orbitAngle)) * orbitDistance;

        transform.position = Vector3.Lerp(
            transform.position, GetHoverTarget() + offset, Time.deltaTime * 5f);

        bool cooldownReady = Time.time >= lastAttackTime + currentCooldown;
        bool globalReady   = Time.time >= globalLastAttackTime + globalAttackSpacing;

        if (cooldownReady && globalReady)
        {
            Vector3 dirToSwarmer = (transform.position - player.position).normalized;
            dirToSwarmer.y = 0f;
            Vector3 playerFwd = player.forward;
            playerFwd.y = 0f;

            if (Vector3.Angle(playerFwd, dirToSwarmer) <= 30f)
            {
                globalLastAttackTime = Time.time;
                StartCoroutine(TelegraphAndDive());
            }
            else
            {
                orbitSpeed = 4f;
            }
        }
        else
        {
            orbitSpeed = 2f;
        }
    }

    private IEnumerator TelegraphAndDive()
    {
        swarmState = SwarmState.Telegraph;

        if (enemyRenderer != null)
            enemyRenderer.material.color = Color.red;

        Vector3 pullback = transform.position +
            (transform.position - player.position).normalized * 0.2f;

        float timer = 0f;
        while (timer < telegraphDuration)
        {
            transform.position = Vector3.Lerp(
                transform.position, pullback, Time.deltaTime * 5f);
            timer += Time.deltaTime;
            yield return null;
        }

        // Position einmalig einfrieren — kein Tracking während Dive
        diveTargetPosition = GetHoverTarget();

        if (enemyRenderer != null)
            enemyRenderer.material = originalMaterial;

        swarmState = SwarmState.Dive;
    }

    private void HandleDive()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, diveTargetPosition, diveSpeed * Time.deltaTime);

        // Beide Checks gegen Kopfhöhe — konsistent mit diveTargetPosition
        if (Vector3.Distance(transform.position, GetHoverTarget()) < 0.5f)
        {
            PerformAttack();
            Debug.Log($"[SwarmerAI] {gameObject.name} — Treffer!");
            ResetAfterDive();
            return;
        }

        if (Vector3.Distance(transform.position, diveTargetPosition) < 0.1f)
        {
            Debug.Log($"[SwarmerAI] {gameObject.name} — Spieler ausgewichen!");
            ResetAfterDive();
        }
    }

    private void ResetAfterDive()
    {
        lastAttackTime = Time.time;
        currentCooldown = Random.Range(attackCooldown, attackCooldown * 2f);
        orbitDirection *= -1;
        swarmState = SwarmState.Orbit;
    }

    protected override void HandleDamaged(float damage, Vector3 direction)
    {
        if (swarmState == SwarmState.Telegraph)
        {
            StopAllCoroutines();
            if (enemyRenderer != null)
                enemyRenderer.material = originalMaterial;
            swarmState = SwarmState.Chase;
        }
    }

    protected override void OnDeath()
    {
        StopAllCoroutines();
        if (enemyRenderer != null)
            enemyRenderer.material = originalMaterial;
        base.OnDeath();
    }
}