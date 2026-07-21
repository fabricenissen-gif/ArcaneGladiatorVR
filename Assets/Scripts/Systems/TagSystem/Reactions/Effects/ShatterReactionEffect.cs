using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Effekt für ELECTRIC + FROST.
///
/// Verursacht sofortigen Reaction-Schaden und deaktiviert unterstützte
/// Enemy-AI-Komponenten für eine feste Dauer. Bereits deaktivierte
/// Komponenten bleiben danach deaktiviert.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShatterReactionEffect :
    MonoBehaviour,
    IReactionEffect
{
    [Header("Shatter")]
    [SerializeField, Min(0f)] private float damage = 30f;

    [SerializeField, Min(0f)] private float stunDuration = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool logExecution = true;

    private readonly Dictionary<MonoBehaviour, Coroutine> activeStuns =
        new Dictionary<MonoBehaviour, Coroutine>();

    public ReactionEffectId EffectId =>
        ReactionEffectId.Shatter;

    private void OnDisable()
    {
        StopAndRestoreAllStuns();
    }

    public bool Execute(ReactionContext context)
    {
        if (context.Health == null || context.Health.IsDead)
            return false;

        context.Health.TakeDamageReaction(damage, Vector3.up);

        ApplyStunToSupportedAi<SimpleEnemyChase>(
            context.TargetGameObject);

        ApplyStunToSupportedAi<SwarmerAI>(
            context.TargetGameObject);

        if (logExecution)
        {
            Debug.Log(
                $"[ShatterReactionEffect] Executed on " +
                $"'{context.TargetGameObject.name}' | " +
                $"damage:{damage:F1} | stun:{stunDuration:F2}s");
        }

        return true;
    }

    private void ApplyStunToSupportedAi<T>(GameObject target)
        where T : MonoBehaviour
    {
        if (target == null)
            return;

        T behaviour = target.GetComponent<T>();

        if (behaviour == null)
            return;

        if (activeStuns.TryGetValue(
                behaviour,
                out Coroutine activeStun))
        {
            StopCoroutine(activeStun);
            activeStuns.Remove(behaviour);
        }

        bool wasEnabledBeforeStun = behaviour.enabled;

        if (!wasEnabledBeforeStun)
        {
            if (logExecution)
            {
                Debug.Log(
                    $"[ShatterReactionEffect] '{behaviour.GetType().Name}' " +
                    $"on '{target.name}' was already disabled; " +
                    "no restore coroutine started.");
            }

            return;
        }

        behaviour.enabled = false;

        Coroutine stunRoutine = StartCoroutine(
            RestoreBehaviourAfterDelay(
                behaviour,
                stunDuration));

        activeStuns.Add(behaviour, stunRoutine);
    }

    private IEnumerator RestoreBehaviourAfterDelay(
        MonoBehaviour behaviour,
        float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, duration));

        activeStuns.Remove(behaviour);

        if (behaviour == null)
            yield break;

        Health targetHealth = behaviour.GetComponent<Health>();

        if (targetHealth == null ||
            targetHealth.IsDead ||
            !behaviour.gameObject.activeInHierarchy)
        {
            yield break;
        }

        behaviour.enabled = true;
    }

    private void StopAndRestoreAllStuns()
    {
        foreach (KeyValuePair<MonoBehaviour, Coroutine> pair
                 in activeStuns)
        {
            MonoBehaviour behaviour = pair.Key;

            if (pair.Value != null)
                StopCoroutine(pair.Value);

            if (behaviour == null)
                continue;

            Health targetHealth = behaviour.GetComponent<Health>();

            if (targetHealth != null &&
                !targetHealth.IsDead &&
                behaviour.gameObject.activeInHierarchy)
            {
                behaviour.enabled = true;
            }
        }

        activeStuns.Clear();
    }
}