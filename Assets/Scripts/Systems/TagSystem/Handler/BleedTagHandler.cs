using System.Collections;
using UnityEngine;

/// <summary>
/// Verwaltet den stapelbaren BLEED-DoT eines Ziels.
/// Der Schaden wird pro Tick anhand der aktuell vorhandenen BLEED-Stacks
/// berechnet. Ein Refresh startet die Coroutine nicht neu, damit bereits
/// laufende Tick-Zyklen stabil und vorhersehbar bleiben.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TagHandler))]
[RequireComponent(typeof(Health))]
public class BleedTagHandler : MonoBehaviour
{
    [Header("Bleed DoT")]
    [Tooltip("Schaden pro aktivem BLEED-Stack bei einem Tick.")]
    [SerializeField, Min(0f)] private float damagePerStackPerTick = 1f;

    [Tooltip("Zeit zwischen zwei BLEED-Schadensticks in Sekunden.")]
    [SerializeField, Min(0.05f)] private float tickInterval = 1f;

    [Header("Debug")]
    [SerializeField] private bool logTicks;

    private TagHandler tagHandler;
    private Health health;
    private Coroutine bleedRoutine;

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

        // Sicherer Fall: Komponente wird aktiviert, während BLEED bereits aktiv ist.
        if (tagHandler.HasTag(TagType.BLEED))
            StartBleed();
    }

    private void OnDisable()
    {
        if (tagHandler != null)
        {
            tagHandler.OnTagApplied -= OnTagApplied;
            tagHandler.OnTagRemoved -= OnTagRemoved;
            tagHandler.OnTagExpired -= OnTagExpired;
        }

        StopBleed();
    }

    private void OnTagApplied(TagType type, TagInstance instance)
    {
        if (type != TagType.BLEED)
            return;

        StartBleed();
    }

    private void OnTagRemoved(TagType type)
    {
        if (type == TagType.BLEED)
            StopBleed();
    }

    private void OnTagExpired(TagType type)
    {
        if (type == TagType.BLEED)
            StopBleed();
    }

    private void StartBleed()
    {
        if (bleedRoutine != null || health == null || health.IsDead)
            return;

        bleedRoutine = StartCoroutine(BleedRoutine());
    }

    private void StopBleed()
    {
        if (bleedRoutine == null)
            return;

        StopCoroutine(bleedRoutine);
        bleedRoutine = null;
    }

    private IEnumerator BleedRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(tickInterval);

        while (tagHandler != null &&
               health != null &&
               !health.IsDead &&
               tagHandler.HasTag(TagType.BLEED))
        {
            yield return wait;

            if (tagHandler == null ||
                health == null ||
                health.IsDead ||
                !tagHandler.HasTag(TagType.BLEED))
            {
                break;
            }

            TagInstance bleed = tagHandler.GetTag(TagType.BLEED);

            if (bleed == null || bleed.StackCount <= 0)
                continue;

            float damage = damagePerStackPerTick * bleed.StackCount;

            health.TakeDamageTagged(
                damage,
                Vector3.zero,
                TagType.BLEED);

            if (logTicks)
            {
                Debug.Log(
                    $"[BleedTagHandler] {gameObject.name} -> " +
                    $"BLEED tick | stacks:{bleed.StackCount} | " +
                    $"damage:{damage:F1}");
            }
        }

        bleedRoutine = null;
    }

    private void OnValidate()
    {
        damagePerStackPerTick = Mathf.Max(0f, damagePerStackPerTick);
        tickInterval = Mathf.Max(0.05f, tickInterval);
    }
}