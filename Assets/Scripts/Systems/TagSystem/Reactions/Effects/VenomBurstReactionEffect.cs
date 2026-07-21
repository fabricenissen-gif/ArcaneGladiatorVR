using UnityEngine;

/// <summary>
/// Effekt für ARCANE + POISON.
///
/// Der Resolver liefert den POISON-Snapshot vor dem Konsum. Dadurch
/// bleibt die Schadensermittlung korrekt, selbst wenn POISON unmittelbar
/// vor der Effekt-Ausführung entfernt wird.
/// </summary>
[DisallowMultipleComponent]
public sealed class VenomBurstReactionEffect :
    MonoBehaviour,
    IReactionEffect
{
    [Header("Venom Burst")]
    [SerializeField, Min(0f)]
    private float damagePerPoisonStack = 10f;

    [Header("Debug")]
    [SerializeField] private bool logExecution = true;

    public ReactionEffectId EffectId =>
        ReactionEffectId.VenomBurst;

    public bool Execute(ReactionContext context)
    {
        if (context.Health == null || context.Health.IsDead)
            return false;

        int poisonStacks = GetPoisonStackCount(context);

        float totalDamage =
            damagePerPoisonStack * poisonStacks;

        context.Health.TakeDamageReaction(
            totalDamage,
            Vector3.zero);

        if (logExecution)
        {
            Debug.Log(
                $"[VenomBurstReactionEffect] Executed on " +
                $"'{context.TargetGameObject.name}' | " +
                $"poisonStacks:{poisonStacks} | " +
                $"damagePerStack:{damagePerPoisonStack:F1} | " +
                $"totalDamage:{totalDamage:F1}");
        }

        return true;
    }

    private static int GetPoisonStackCount(
        ReactionContext context)
    {
        TagInstance firstTag = context.FirstTagSnapshot;
        TagInstance secondTag = context.SecondTagSnapshot;

        if (firstTag != null &&
            firstTag.Type == TagType.POISON)
        {
            return Mathf.Max(1, firstTag.StackCount);
        }

        if (secondTag != null &&
            secondTag.Type == TagType.POISON)
        {
            return Mathf.Max(1, secondTag.StackCount);
        }

        Debug.LogWarning(
            "[VenomBurstReactionEffect] No POISON snapshot found. " +
            "Using one stack as safe fallback.");

        return 1;
    }
}