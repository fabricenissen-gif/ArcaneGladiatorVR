using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public class RangedGruntAI : EnemyBase
{
    private enum RangedState { Reposition, Telegraph, Shoot, Recover, Evade }
    public  enum ShootStyle  { Single, Burst, Lobbed }

    [Header("Ranged — References")]
    [SerializeField] private Transform  visualModel;
    [SerializeField] private Transform  projectileSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Ranged — Movement")]
    [SerializeField] private float chaseSpeed      = 2.5f;
    [SerializeField] private float evadeSpeed      = 3.2f;
    [SerializeField] private float idealMinDist    = 5f;
    [SerializeField] private float idealMaxDist    = 9f;
    [SerializeField] private float evadeDistance   = 3f;
    [SerializeField] private float evadeCooldown   = 2.5f;
    [SerializeField] private float evadeDuration   = 0.8f;
    [SerializeField] private float repositionPause = 0.4f;

    [Header("Ranged — Aim")]
    [Tooltip("Wie hoch über player.position gezielt wird (Körpermitte)")]
    [SerializeField] private float aimHeightOffset = 1.0f;

    [Header("Ranged — Shoot Style")]
    [SerializeField] private ShootStyle shootStyle = ShootStyle.Single;

    [SerializeField] private float singleDamage    = 12f;
    [SerializeField] private float singleSpeed     = 14f;
    [SerializeField] private float singleKnockback = 2f;

    [SerializeField] private int   burstCount      = 3;
    [SerializeField] private float burstDamage     = 8f;
    [SerializeField] private float burstSpeed      = 16f;
    [SerializeField] private float burstInterval   = 0.18f;
    [SerializeField] private float burstSpread     = 4f;

    [SerializeField] private float lobbedDamage      = 20f;
    [SerializeField] private float lobbedArcHeight   = 4f;
    [SerializeField] private float lobbedFlightTime  = 1.2f;
    [SerializeField] private float lobbedSplashRadius = 1.5f;

    [Header("Ranged — Telegraph")]
    [SerializeField] private float telegraphDuration = 0.65f;
    [SerializeField] private float recoverDuration   = 0.5f;
    [SerializeField] private float attackCooldownMin = 2.2f;
    [SerializeField] private float attackCooldownMax = 3.8f;

    [Header("Ranged — Hit Feedback")]
    [SerializeField] private float      hitPauseDuration = 0.12f;
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color      flashColor    = new Color(1f, 0.25f, 0.1f, 1f);
    [SerializeField] private float      flashDuration = 0.06f;
    private static readonly Vector3 HitSquishScale  = new Vector3(1.2f, 0.78f, 1.2f);
    private static readonly Vector3 HitRecoverScale = new Vector3(0.9f, 1.14f, 0.9f);
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // ── State ─────────────────────────────────────────────────
    private RangedState rangedState        = RangedState.Reposition;
    private bool        isShooting         = false;
    private bool        isEvading          = false;
    private bool        isHitPaused        = false;
    private float       evadeCooldownTimer = 0f;
    private Coroutine   shootRoutine;
    private Coroutine   evadeRoutine;
    private Coroutine   hitPauseRoutine;

    // ── Lifecycle ─────────────────────────────────────────────

    protected override void Start()
    {
        base.Start();
        agent.speed            = chaseSpeed;
        agent.stoppingDistance = 0.3f;
        attackCooldownTimer    = Random.Range(attackCooldownMin * 0.5f, attackCooldownMin);
        evadeCooldownTimer     = 0f; // Sofort reaktionsfähig

        foreach (var r in flashRenderers)
            foreach (var mat in r.materials)
                mat.EnableKeyword("_EMISSION");
    }

    protected override void Update()
    {
        base.Update();
        if (!isDead) evadeCooldownTimer -= Time.deltaTime;
    }

    // ── EnemyBase overrides ───────────────────────────────────

    protected override void UpdateState()
    {
        if (player == null) return;
        if (isShooting) { FaceTargetSlerp(); return; }
        if (isHitPaused) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Evade Priorität — Agent muss bewegungsfähig sein
        if (dist < evadeDistance && evadeCooldownTimer <= 0f && !isEvading)
        {
            // Agent wieder aktivieren falls er gestoppt war
            EnsureAgentActive();
            if (evadeRoutine != null) StopCoroutine(evadeRoutine);
            evadeRoutine = StartCoroutine(EvadeRoutine());
            return;
        }

        if (isEvading) return;

        // Schuss wenn in Idealrange
        if (dist >= idealMinDist && dist <= idealMaxDist && attackCooldownTimer <= 0f)
        {
            EnsureAgentActive();
            agent.isStopped = true;
            FaceTargetSlerp();
            if (shootRoutine != null) StopCoroutine(shootRoutine);
            shootRoutine = StartCoroutine(ShootSequence());
            return;
        }

        // Reposition
        RepositionToIdeal(dist);
    }

    protected override void HandleDamaged(float damage, Vector3 direction)
    {
        if (hitPauseRoutine != null) StopCoroutine(hitPauseRoutine);
        hitPauseRoutine = StartCoroutine(HitPauseRoutine());
    }

    protected override void OnDeath()
    {
        StopAllCoroutines();
        ResetVisual();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped      = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.ResetPath();
            agent.Warp(transform.position);
            agent.enabled        = false;
        }

        Debug.Log($"[RangedGruntAI] {gameObject.name} OnDeath @ {transform.position}");
    }

    // ── Reposition ────────────────────────────────────────────

    private void RepositionToIdeal(float dist)
    {
        if (player == null) return;

        EnsureAgentActive();
        agent.speed     = chaseSpeed;
        agent.isStopped = false;

        if (dist > idealMaxDist)
        {
            agent.SetDestination(player.position);
        }
        else if (dist < idealMinDist)
        {
            Vector3 awayDir = (transform.position - player.position).normalized;
            awayDir.y       = 0f;
            Vector3 backPos = transform.position + awayDir * (idealMinDist - dist + 1f);
            Vector3 safe    = GetSafeNavMeshPosition(backPos, 2f);
            agent.SetDestination(safe);
        }
        else
        {
            agent.isStopped = true;
            FaceTargetSlerp();
        }
    }

    // ── Evade ─────────────────────────────────────────────────

    private IEnumerator EvadeRoutine()
    {
        isEvading          = true;
        evadeCooldownTimer = evadeCooldown;
        rangedState        = RangedState.Evade;

        EnsureAgentActive();
        agent.speed     = evadeSpeed;
        agent.isStopped = false;

        Vector3 evadeDir    = GetSafeEvadeDirection();
        Vector3 evadeTarget = transform.position + evadeDir * (evadeSpeed * evadeDuration * 0.6f);
        Vector3 safeTarget  = GetSafeNavMeshPosition(evadeTarget, 3f);

        agent.SetDestination(safeTarget);

        float timer = 0f;
        while (timer < evadeDuration)
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f) break;
            FaceTargetSlerp();
            timer += Time.deltaTime;
            yield return null;
        }

        agent.speed = chaseSpeed;
        isEvading   = false;
        evadeRoutine = null;
        rangedState  = RangedState.Reposition;

        yield return new WaitForSeconds(repositionPause);
    }

    private Vector3 GetSafeEvadeDirection()
    {
        Vector3 away = (transform.position - player.position).normalized;
        away.y = 0f;

        Vector3[] candidates =
        {
            Quaternion.Euler(0,  50f, 0) * away,
            Quaternion.Euler(0, -50f, 0) * away,
            away,
            Quaternion.Euler(0,  90f, 0) * away,
            Quaternion.Euler(0, -90f, 0) * away,
        };

        foreach (Vector3 dir in candidates)
        {
            Vector3 test = transform.position + dir * 3f;
            Vector3 safe = GetSafeNavMeshPosition(test, 2f);
            if (Vector3.Distance(safe, test) < 1.5f) return dir;
        }

        return away;
    }

    private Vector3 GetSafeNavMeshPosition(Vector3 desired, float radius)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desired, out hit, radius, NavMesh.AllAreas))
            return hit.position;
        return transform.position;
    }

    // Stellt sicher dass Agent bewegungsfähig ist nach StopAgent
    private void EnsureAgentActive()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        if (!agent.updatePosition)
        {
            agent.Warp(transform.position);
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
        agent.isStopped = false;
    }

    // ── Shoot Sequence ────────────────────────────────────────

    private IEnumerator ShootSequence()
    {
        isShooting  = true;
        rangedState = RangedState.Telegraph;

        float timer = 0f;
        while (timer < telegraphDuration)
        {
            if (visualModel != null)
            {
                float pulse = 1f + Mathf.Sin(timer * Mathf.PI * 6f) * 0.04f;
                visualModel.localScale = Vector3.one * pulse;
            }
            FaceTargetSlerp();
            timer += Time.deltaTime;
            yield return null;
        }

        if (visualModel != null) visualModel.localScale = Vector3.one;

        rangedState = RangedState.Shoot;

        switch (shootStyle)
        {
            case ShootStyle.Single: yield return StartCoroutine(FireSingle()); break;
            case ShootStyle.Burst:  yield return StartCoroutine(FireBurst());  break;
            case ShootStyle.Lobbed: yield return StartCoroutine(FireLobbed()); break;
        }

        rangedState = RangedState.Recover;
        yield return new WaitForSeconds(recoverDuration);

        attackCooldownTimer = Random.Range(attackCooldownMin, attackCooldownMax);
        isShooting          = false;
        shootRoutine        = null;
        rangedState         = RangedState.Reposition;
    }

    // ── Fire Modes ────────────────────────────────────────────

    private IEnumerator FireSingle()
    {
        SpawnProjectile(GetAimDirection(), singleSpeed, singleDamage, singleKnockback,
            false, 0f, 0f, 0f);
        yield return null;
    }

    private IEnumerator FireBurst()
    {
        for (int i = 0; i < burstCount; i++)
        {
            float   angle = (i - (burstCount - 1) * 0.5f) * burstSpread;
            Vector3 dir   = Quaternion.Euler(0f, angle, 0f) * GetAimDirection();
            SpawnProjectile(dir, burstSpeed, burstDamage, 1f, false, 0f, 0f, 0f);
            if (i < burstCount - 1)
                yield return new WaitForSeconds(burstInterval);
        }
    }

    private IEnumerator FireLobbed()
    {
        if (player == null) yield break;
        SpawnProjectile(Vector3.up, 0f, lobbedDamage, 0f,
            true, lobbedArcHeight, lobbedFlightTime, lobbedSplashRadius);
        yield return null;
    }

    private void SpawnProjectile(Vector3 direction, float speed, float damage,
        float knockback, bool isLobbed, float arcHeight, float flightTime, float splash)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[RangedGruntAI] Kein projectilePrefab gesetzt!");
            return;
        }

        Transform spawnTf = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        GameObject proj   = Instantiate(projectilePrefab, spawnTf.position,
            Quaternion.LookRotation(direction == Vector3.up ? transform.forward : direction));

        RangedGruntProjectile rp = proj.GetComponent<RangedGruntProjectile>();
        if (rp == null)
        {
            Debug.LogWarning("[RangedGruntAI] Projektil hat kein RangedGruntProjectile Script!");
            return;
        }

        if (isLobbed)
            rp.InitLobbed(player.position + Vector3.up * aimHeightOffset,
                arcHeight, flightTime, damage, playerLayer, splash);
        else
            rp.InitDirect(direction, speed, damage, knockback, playerLayer);
    }

    // Zielhöhe korrigiert — trifft Körpermitte statt Füße
    private Vector3 GetAimDirection()
    {
        if (player == null) return transform.forward;

        Vector3 aimTarget = player.position + Vector3.up * aimHeightOffset;

        // Leichte Vorausberechnung
        Rigidbody playerRb = player.GetComponentInParent<Rigidbody>();
        if (playerRb != null)
            aimTarget += playerRb.linearVelocity * 0.12f;

        Transform spawnTf = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Vector3   dir     = (aimTarget - spawnTf.position).normalized;
        return dir;
    }

    // ── Hit Pause ─────────────────────────────────────────────

    private IEnumerator HitPauseRoutine()
    {
        isHitPaused = true;
        if (!isShooting && !isEvading) agent.isStopped = true;

        if (flashRenderers.Length > 0) StartCoroutine(MaterialFlash());

        if (visualModel != null) visualModel.localScale = HitSquishScale;
        yield return new WaitForSeconds(hitPauseDuration * 0.35f);

        if (visualModel != null) visualModel.localScale = HitRecoverScale;
        yield return new WaitForSeconds(hitPauseDuration * 0.35f);

        float elapsed = 0f, remaining = hitPauseDuration * 0.30f;
        while (elapsed < remaining)
        {
            if (visualModel != null)
                visualModel.localScale = Vector3.Lerp(HitRecoverScale, Vector3.one, elapsed / remaining);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (visualModel != null) visualModel.localScale = Vector3.one;

        if (!isShooting && !isEvading)
        {
            EnsureAgentActive();
            attackCooldownTimer = attackCooldownMin * 0.4f;
        }

        isHitPaused     = false;
        hitPauseRoutine = null;
    }

    private IEnumerator MaterialFlash()
    {
        foreach (var r in flashRenderers)
            foreach (var mat in r.materials)
                mat.SetColor(EmissionColorId, flashColor);
        yield return new WaitForSeconds(flashDuration);
        foreach (var r in flashRenderers)
            foreach (var mat in r.materials)
                mat.SetColor(EmissionColorId, Color.black);
    }

    // ── Helpers ───────────────────────────────────────────────

    private void FaceTargetSlerp()
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * 6f);
    }

    private void ResetVisual()
    {
        if (visualModel == null) return;
        visualModel.localRotation = Quaternion.identity;
        visualModel.localScale    = Vector3.one;
    }

    // ── Gizmos ────────────────────────────────────────────────

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, idealMinDist);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, idealMaxDist);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, evadeDistance);
    }
}