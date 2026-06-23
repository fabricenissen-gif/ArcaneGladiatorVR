using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public class GruntMeleeAI : EnemyBase
{
    private enum GruntState { Chase, Telegraph, Attack, Recover }

    [Header("Grunt — References")]
    [SerializeField] private Transform visualModel;

    [Header("Grunt — Movement")]
    [SerializeField] private float chaseSpeed    = 3.5f;
    [SerializeField] private float stopDistance  = 1.6f;

    [Header("Grunt — Attack")]
    [SerializeField] private float telegraphDuration = 0.6f;
    [SerializeField] private float attackDuration    = 0.15f;
    [SerializeField] private float recoverDuration   = 0.5f;
    [SerializeField] private float attackRadius      = 0.5f;
    [SerializeField] private float attackCooldownMin = 2f;
    [SerializeField] private float attackCooldownMax = 3.5f;

    [Header("Grunt — Telegraph Tilt")]
    [SerializeField] private float tiltAngle = 60f;
    [SerializeField] private float tiltSpeed = 14f;

    [Header("Grunt — Hit Feedback")]
    [SerializeField] private float hitPauseDuration = 0.15f;
    private static readonly Vector3 HitSquishScale  = new Vector3(1.25f, 0.72f, 1.25f);
    private static readonly Vector3 HitRecoverScale = new Vector3(0.88f, 1.18f, 0.88f);

    [Header("Grunt — Material Flash (optional)")]
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color      flashColor    = new Color(1f, 0.25f, 0.1f, 1f);
    [SerializeField] private float      flashDuration = 0.06f;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // ── State ─────────────────────────────────────────────────
    private GruntState gruntState     = GruntState.Chase;
    private bool       isHitPaused    = false;
    private bool       isAttacking    = false;
    private Coroutine  hitPauseRoutine;
    private Coroutine  attackRoutine;

    // ── Lifecycle ─────────────────────────────────────────────

    protected override void Start()
    {
        base.Start();
        agent.speed            = chaseSpeed;
        agent.stoppingDistance = stopDistance;
        attackCooldownTimer    = Random.Range(attackCooldownMin * 0.5f, attackCooldownMin);
        gruntState             = GruntState.Chase;

        foreach (var r in flashRenderers)
            foreach (var mat in r.materials)
                mat.EnableKeyword("_EMISSION");
    }

    // ── EnemyBase overrides ───────────────────────────────────

    protected override void UpdateState()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (isAttacking)
        {
            if (agent.hasPath) agent.ResetPath();
            FaceTargetSlerp();
            return;
        }

        // Hit-Pause blockiert nur Bewegung — Angriff läuft weiter
        if (isHitPaused)
        {
            if (agent.hasPath) agent.ResetPath();
            return;
        }

        if (dist <= attackRange)
        {
            if (agent.hasPath) agent.ResetPath();
            FaceTargetSlerp();

            if (attackCooldownTimer <= 0f)
            {
                if (attackRoutine != null) StopCoroutine(attackRoutine);
                attackRoutine = StartCoroutine(AttackSequence());
            }
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(player.position);
        FaceTargetSlerp();
    }

    protected override void HandleDamaged(float damage, Vector3 direction)
    {
        if (hitPauseRoutine != null) StopCoroutine(hitPauseRoutine);
        hitPauseRoutine = StartCoroutine(HitPauseRoutine());
    }

    protected override void OnDeath()
    {
        // Alle Coroutines stoppen — inkl. AttackSequence
        StopAllCoroutines();

        // Visual sofort resetten
        ResetVisual();

        // Agent einfrieren — Warp auf aktuelle Position verhindert NavMesh-Teleport
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped      = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.ResetPath();
            agent.Warp(transform.position);
            agent.enabled        = false;
        }

        // KEIN base.OnDeath() — Health.DeathRoutine übernimmt Destroy
        Debug.Log($"[GruntMeleeAI] {gameObject.name} OnDeath an Position {transform.position}");
    }

    // ── Movement ──────────────────────────────────────────────

    private void FaceTargetSlerp()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * 6f);
    }

    // ── Attack Sequence ───────────────────────────────────────

    private IEnumerator AttackSequence()
    {
        isAttacking = true;
        gruntState  = GruntState.Telegraph;

        StopAgent();

        Quaternion tiltBack    = Quaternion.Euler(-tiltAngle, 0f, 0f);
        Quaternion tiltForward = Quaternion.Euler(15f, 0f, 0f);
        Quaternion neutral     = Quaternion.identity;

        // -- Telegraph
        float timer = 0f;
        while (timer < telegraphDuration)
        {
            if (visualModel != null)
                visualModel.localRotation = Quaternion.Slerp(
                    visualModel.localRotation, tiltBack, Time.deltaTime * tiltSpeed);
            timer += Time.deltaTime;
            yield return null;
        }

        // -- Attack
        gruntState       = GruntState.Attack;
        bool damageDealt = false;

        timer = 0f;
        while (timer < attackDuration)
        {
            if (visualModel != null)
                visualModel.localRotation = Quaternion.Slerp(
                    visualModel.localRotation, tiltForward, Time.deltaTime * tiltSpeed * 4f);

            if (!damageDealt && timer >= attackDuration * 0.6f)
            {
                damageDealt = true;
                ApplyHit();
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // -- Recover
        gruntState = GruntState.Recover;

        timer = 0f;
        while (timer < recoverDuration)
        {
            if (visualModel != null)
                visualModel.localRotation = Quaternion.Slerp(
                    visualModel.localRotation, neutral, Time.deltaTime * tiltSpeed);
            timer += Time.deltaTime;
            yield return null;
        }

        ResetVisual();
        ResumeAgent();

        attackCooldownTimer = Random.Range(attackCooldownMin, attackCooldownMax);
        isAttacking         = false;
        attackRoutine       = null;
        gruntState          = GruntState.Chase;
    }

    private void ApplyHit()
    {
        Vector3    center = transform.position + transform.forward * 1.0f + Vector3.up * 1.0f;
        Collider[] hits   = Physics.OverlapSphere(
            center, attackRadius, playerLayer, QueryTriggerInteraction.Collide);

        foreach (Collider col in hits)
        {
            PlayerHealth ph = col.GetComponentInParent<PlayerHealth>();
            if (ph == null) continue;
            ph.TakeDamage((int)attackDamage,
                (player.position - transform.position).normalized);
            Debug.Log($"[GruntMeleeAI] {gameObject.name} — Treffer!");
            break;
        }
    }

    // ── Hit Pause + Feedback ──────────────────────────────────

    private IEnumerator HitPauseRoutine()
    {
        isHitPaused = true;

        // Angriff läuft weiter — nur Bewegung stoppen
        if (!isAttacking)
            StopAgent();

        // Material-Flash parallel
        if (flashRenderers.Length > 0)
            StartCoroutine(MaterialFlash());

        // Phase 1: Squish
        if (visualModel != null) visualModel.localScale = HitSquishScale;
        yield return new WaitForSeconds(hitPauseDuration * 0.35f);

        // Phase 2: Bounce
        if (visualModel != null) visualModel.localScale = HitRecoverScale;
        yield return new WaitForSeconds(hitPauseDuration * 0.35f);

        // Phase 3: Lerp zurück
        float elapsed   = 0f;
        float remaining = hitPauseDuration * 0.30f;
        while (elapsed < remaining)
        {
            if (visualModel != null)
                visualModel.localScale = Vector3.Lerp(
                    HitRecoverScale, Vector3.one, elapsed / remaining);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Nur Scale resetten — Rotation läuft weiter wenn Angriff aktiv
        if (visualModel != null) visualModel.localScale = Vector3.one;

        if (!isAttacking)
        {
            ResumeAgent();
            attackCooldownTimer = attackCooldownMin * 0.5f;
            gruntState          = GruntState.Chase;
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

    private void StopAgent()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        agent.ResetPath();
        agent.isStopped      = true;
        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    private void ResumeAgent()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        agent.Warp(transform.position);
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped      = false;
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(
            transform.position + transform.forward * 1.0f + Vector3.up * 1.0f,
            attackRadius);
    }
}