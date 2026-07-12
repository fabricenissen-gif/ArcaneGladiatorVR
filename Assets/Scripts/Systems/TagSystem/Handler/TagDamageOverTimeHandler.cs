using System.Collections;
using UnityEngine;

/// <summary>
/// Gemeinsame Basis für Tags, die über ihre Laufzeit periodischen Schaden
/// verursachen.
///
/// WICHTIG: Diese Basisklasse darf kein DisallowMultipleComponent-Attribut
/// besitzen. Unity vererbt dieses Attribut auf Unterklassen und würde dann
/// BleedTagHandler, PoisonTagHandler und FireTagHandler gegenseitig
/// ausschließen.
/// </summary>
[RequireComponent(typeof(TagHandler))]
[RequireComponent(typeof(Health))]
public abstract class TagDamageOverTimeHandler : MonoBehaviour
{
    [Header("Damage over Time")]
    [Tooltip("Schaden pro aktivem Stack bei einem Tick.")]
    [SerializeField, Min(0f)] private float damagePerStackPerTick = 1f;

    [Tooltip("Zeit zwischen zwei Schadensticks in Sekunden.")]
    [SerializeField, Min(0.05f)] private float tickInterval = 1f;

    [Header("Debug")]
    [SerializeField] private bool logTicks;

    private TagHandler tagHandler;
    private Health health;
    private Coroutine damageRoutine;

    /// <summary>
    /// Der Tag, dessen Laufzeit und Stacks dieser Handler verarbeitet.
    /// </summary>
    protected abstract TagType DamageTag { get; }

    /// <summary>
    /// Nur für eindeutige Debug-Ausgaben.
    /// </summary>
    protected abstract string DebugTagName { get; }

    private void Awake()
    {
        tagHandler = GetComponent<TagHandler>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (tagHandler == null)
            return;

        tagHandler.OnTagApplied += OnTagApplied;
        tagHandler.OnTagRemoved += OnTagRemoved;
        tagHandler.OnTagExpired += OnTagExpired;

        // Fail-safe: Die Komponente kann aktiviert werden, nachdem ihr
        // Tag bereits durch eine andere Quelle appliziert wurde.
        if (tagHandler.HasTag(DamageTag))
            StartDamageRoutine();
    }

    private void OnDisable()
    {
        if (tagHandler != null)
        {
            tagHandler.OnTagApplied -= OnTagApplied;
            tagHandler.OnTagRemoved -= OnTagRemoved;
            tagHandler.OnTagExpired -= OnTagExpired;
        }

        StopDamageRoutine();
    }

    private void OnTagApplied(TagType type, TagInstance instance)
    {
        if (type == DamageTag)
            StartDamageRoutine();
    }

    private void OnTagRemoved(TagType type)
    {
        if (type == DamageTag)
            StopDamageRoutine();
    }

    private void OnTagExpired(TagType type)
    {
        if (type == DamageTag)
            StopDamageRoutine();
    }

    private void StartDamageRoutine()
    {
        if (damageRoutine != null || health == null || health.IsDead)
            return;

        damageRoutine = StartCoroutine(DamageRoutine());
    }

    private void StopDamageRoutine()
    {
        if (damageRoutine == null)
            return;

        StopCoroutine(damageRoutine);
        damageRoutine = null;
    }

    private IEnumerator DamageRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(tickInterval);

        while (tagHandler != null &&
               health != null &&
               !health.IsDead &&
               tagHandler.HasTag(DamageTag))
        {
            yield return wait;

            if (tagHandler == null ||
                health == null ||
                health.IsDead ||
                !tagHandler.HasTag(DamageTag))
            {
                break;
            }

            TagInstance tagInstance = tagHandler.GetTag(DamageTag);

            if (tagInstance == null || tagInstance.StackCount <= 0)
                continue;

            float damage =
                damagePerStackPerTick * tagInstance.StackCount;

            if (damage <= 0f)
                continue;

            health.TakeDamageTagged(
                damage,
                Vector3.zero,
                DamageTag);

            if (logTicks)
            {
                Debug.Log(
                    $"[{GetType().Name}] {gameObject.name} -> " +
                    $"{DebugTagName} tick | " +
                    $"stacks:{tagInstance.StackCount} | " +
                    $"damage:{damage:F1}");
            }
        }

        damageRoutine = null;
    }

    private void OnValidate()
    {
        damagePerStackPerTick = Mathf.Max(0f, damagePerStackPerTick);
        tickInterval = Mathf.Max(0.05f, tickInterval);
    }
}