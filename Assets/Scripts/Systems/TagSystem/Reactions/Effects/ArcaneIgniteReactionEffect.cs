using System.Collections;
using UnityEngine;

/// <summary>
/// Effekt für ARCANE + FIRE.
///
/// Führt initialen Reaction-Schaden aus und startet anschließend einen
/// zeitlich begrenzten Damage-over-Time-Effekt. Ein erneutes Arcane Ignite
/// auf demselben Ziel ersetzt den vorherigen DOT sauber, statt parallele
/// Coroutines oder doppelte Burning-VFX anzusammeln.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArcaneIgniteReactionEffect :
    MonoBehaviour,
    IReactionEffect
{
    [Header("Arcane Ignite")]
    [SerializeField, Min(0f)] private float burstDamage = 12f;

    [SerializeField, Min(1)] private int tickCount = 4;

    [SerializeField, Min(0f)] private float totalDuration = 3f;

    [SerializeField, Min(0f)] private float tickDamage = 4f;

    [Header("Visuals")]
    [Tooltip(
        "Optionales VFX-Prefab. Es wird am Ziel geparentet und beim Ende " +
        "des DOT, beim Tod oder beim Deaktivieren bereinigt.")]
    [SerializeField] private GameObject burningVfxPrefab;

    [Header("Debug")]
    [SerializeField] private bool logExecution = true;

    private Coroutine igniteRoutine;
    private GameObject activeBurningVfx;
    private Health activeTargetHealth;

    public ReactionEffectId EffectId =>
        ReactionEffectId.ArcaneIgnite;

    private void OnDisable()
    {
        StopArcaneIgnite();
    }

    private void OnDestroy()
    {
        StopArcaneIgnite();
    }

    public bool Execute(ReactionContext context)
    {
        if (context.Health == null || context.Health.IsDead)
            return false;

        StopArcaneIgnite();

        activeTargetHealth = context.Health;

        activeTargetHealth.TakeDamageReaction(
            burstDamage,
            Vector3.zero);

        // Burst kann das Ziel töten. In dem Fall keine Coroutine oder VFX
        // auf einem sterbenden Ziel erzeugen.
        if (activeTargetHealth == null || activeTargetHealth.IsDead)
        {
            activeTargetHealth = null;
            return true;
        }

        CreateBurningVfx(activeTargetHealth.transform);

        igniteRoutine = StartCoroutine(
            ArcaneIgniteRoutine(activeTargetHealth));

        if (logExecution)
        {
            Debug.Log(
                $"[ArcaneIgniteReactionEffect] Executed on " +
                $"'{context.TargetGameObject.name}' | " +
                $"burst:{burstDamage:F1} | ticks:{tickCount} | " +
                $"tickDamage:{tickDamage:F1} | " +
                $"duration:{totalDuration:F2}s");
        }

        return true;
    }

    private IEnumerator ArcaneIgniteRoutine(Health targetHealth)
    {
        int safeTickCount = Mathf.Max(1, tickCount);
        float tickInterval =
            Mathf.Max(0f, totalDuration) / safeTickCount;

        for (int currentTick = 1;
             currentTick <= safeTickCount;
             currentTick++)
        {
            if (tickInterval > 0f)
                yield return new WaitForSeconds(tickInterval);
            else
                yield return null;

            if (targetHealth == null || targetHealth.IsDead)
                break;

            targetHealth.TakeDamageReaction(
                tickDamage,
                Vector3.zero);

            if (logExecution)
            {
                Debug.Log(
                    $"[ArcaneIgniteReactionEffect] Tick " +
                    $"{currentTick}/{safeTickCount} on " +
                    $"'{targetHealth.gameObject.name}' | " +
                    $"damage:{tickDamage:F1}");
            }
        }

        igniteRoutine = null;
        activeTargetHealth = null;

        DestroyBurningVfx();
    }

    private void CreateBurningVfx(Transform targetTransform)
    {
        if (burningVfxPrefab == null || targetTransform == null)
            return;

        activeBurningVfx = Instantiate(
            burningVfxPrefab,
            targetTransform.position,
            Quaternion.identity,
            targetTransform);
    }

    private void StopArcaneIgnite()
    {
        if (igniteRoutine != null)
        {
            StopCoroutine(igniteRoutine);
            igniteRoutine = null;
        }

        activeTargetHealth = null;

        DestroyBurningVfx();
    }

    private void DestroyBurningVfx()
    {
        if (activeBurningVfx == null)
            return;

        Destroy(activeBurningVfx);
        activeBurningVfx = null;
    }

    private void OnValidate()
    {
        burstDamage = Mathf.Max(0f, burstDamage);
        tickCount = Mathf.Max(1, tickCount);
        totalDuration = Mathf.Max(0f, totalDuration);
        tickDamage = Mathf.Max(0f, tickDamage);
    }
}