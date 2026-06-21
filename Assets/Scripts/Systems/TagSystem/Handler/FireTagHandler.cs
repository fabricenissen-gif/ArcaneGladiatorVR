using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TagHandler))]
[RequireComponent(typeof(Health))]
public class FireTagHandler : MonoBehaviour
{
    [Header("Fire DoT")]
    [SerializeField] private float tickDamage   = 2f;
    [SerializeField] private float tickInterval = 1f;

    private TagHandler tagHandler;
    private Health     health;
    private Coroutine  burnRoutine;

    private void Awake()
    {
        tagHandler = GetComponent<TagHandler>();
        health     = GetComponent<Health>();
    }

    private void OnEnable()
    {
        tagHandler.OnTagApplied += OnTagApplied;
        tagHandler.OnTagRemoved += OnTagRemoved;
        tagHandler.OnTagExpired += OnTagExpired;
    }

    private void OnDisable()
    {
        tagHandler.OnTagApplied -= OnTagApplied;
        tagHandler.OnTagRemoved -= OnTagRemoved;
        tagHandler.OnTagExpired -= OnTagExpired;
        StopBurn();
    }

    private void OnTagApplied(TagType type, TagInstance instance)
    {
        if (type != TagType.FIRE) return;
        StopBurn();
        burnRoutine = StartCoroutine(BurnRoutine());
    }

    private void OnTagRemoved(TagType type) { if (type == TagType.FIRE) StopBurn(); }
    private void OnTagExpired(TagType type) { if (type == TagType.FIRE) StopBurn(); }

    private IEnumerator BurnRoutine()
    {
        while (tagHandler.HasTag(TagType.FIRE))
        {
            yield return new WaitForSeconds(tickInterval);
            if (!tagHandler.HasTag(TagType.FIRE)) yield break;
            health.TakeDamageTagged(tickDamage, Vector3.zero, TagType.FIRE);
        }
        burnRoutine = null;
    }

    private void StopBurn()
    {
        if (burnRoutine == null) return;
        StopCoroutine(burnRoutine);
        burnRoutine = null;
    }
}