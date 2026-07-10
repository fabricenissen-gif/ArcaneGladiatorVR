using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TagHandler : MonoBehaviour
{
    public enum TagChangeType
    {
        Applied,
        Refreshed,
        Removed,
        Expired
    }

    public readonly struct TagChangeEvent
    {
        public TagType TagType { get; }
        public TagInstance Instance { get; }
        public TagChangeType ChangeType { get; }

        public TagChangeEvent(
            TagType tagType,
            TagInstance instance,
            TagChangeType changeType)
        {
            TagType = tagType;
            Instance = instance;
            ChangeType = changeType;
        }
    }

    [Header("Stack Limits")]
    [Tooltip(
        "Maximale POISON-Stacks. Kann später durch Modifier erweitert werden.")]
    [SerializeField, Min(1)] private int maxPoisonStacks = 3;

    [Tooltip(
        "Maximale BLEED-Stacks. Kann später durch Modifier erweitert werden.")]
    [SerializeField, Min(1)] private int maxBleedStacks = 5;

    private readonly Dictionary<TagType, TagInstance> activeTags =
        new Dictionary<TagType, TagInstance>();

    public event Action<TagType, TagInstance> OnTagApplied;
    public event Action<TagType, TagInstance> OnTagRefreshed;
    public event Action<TagType> OnTagExpired;
    public event Action<TagType> OnTagRemoved;

    public event Action<TagChangeEvent> OnTagChanged;

    private void OnValidate()
    {
        maxPoisonStacks = Mathf.Max(1, maxPoisonStacks);
        maxBleedStacks = Mathf.Max(1, maxBleedStacks);
    }

    private void Update()
    {
        TickTags();
    }

    // ── Public API ─────────────────────────────────────────────

    public void ApplyTag(TagType type, float duration)
    {
        ApplyTag(type, duration, TagApplicationContext.Unknown);
    }

    public void ApplyTag(
        TagType type,
        float duration,
        TagApplicationContext context)
    {
        if (type == TagType.NONE)
        {
            Debug.LogWarning(
                $"[TagHandler] '{gameObject.name}' tried to apply TagType.NONE. " +
                "The request was ignored.");

            return;
        }

        float safeDuration = Mathf.Max(0f, duration);

        if (activeTags.TryGetValue(type, out TagInstance existing))
        {
            RefreshExistingTag(type, existing, safeDuration, context);
            return;
        }

        TagInstance newTag = new TagInstance(
            type,
            safeDuration,
            context);

        activeTags.Add(type, newTag);

        OnTagApplied?.Invoke(type, newTag);
        RaiseTagChanged(type, newTag, TagChangeType.Applied);

        Debug.Log(
            $"[TagHandler] {gameObject.name} -> TAG applied: {type} " +
            $"({safeDuration:F2}s) | stacks:{newTag.StackCount} | " +
            $"source:{FormatSource(newTag)}");
    }

    public bool HasTag(TagType type)
    {
        return activeTags.ContainsKey(type);
    }

    public TagInstance GetTag(TagType type)
    {
        activeTags.TryGetValue(type, out TagInstance tag);
        return tag;
    }

    public List<TagType> GetAllActiveTags()
    {
        return new List<TagType>(activeTags.Keys);
    }

    public bool RemoveTag(TagType type)
    {
        if (!activeTags.TryGetValue(type, out TagInstance instance))
            return false;

        activeTags.Remove(type);

        OnTagRemoved?.Invoke(type);
        RaiseTagChanged(type, instance, TagChangeType.Removed);

        Debug.Log(
            $"[TagHandler] {gameObject.name} -> TAG removed: {type}");

        return true;
    }

    public void RemoveAllTags()
    {
        if (activeTags.Count == 0)
            return;

        List<TagType> tagsToRemove = new List<TagType>(activeTags.Keys);

        foreach (TagType type in tagsToRemove)
            RemoveTag(type);

        Debug.Log(
            $"[TagHandler] {gameObject.name} -> All TAGs removed.");
    }

    public void SetMaxPoisonStacks(int max)
    {
        maxPoisonStacks = Mathf.Max(1, max);
    }

    public void SetMaxBleedStacks(int max)
    {
        maxBleedStacks = Mathf.Max(1, max);
    }

    // ── Internal tag behaviour ─────────────────────────────────

    private void RefreshExistingTag(
        TagType type,
        TagInstance existing,
        float duration,
        TagApplicationContext context)
    {
        bool stackAdded = false;

        switch (type)
        {
            case TagType.POISON:
                stackAdded = TryAddStack(existing, maxPoisonStacks);
                break;

            case TagType.BLEED:
                stackAdded = TryAddStack(existing, maxBleedStacks);
                break;
        }

        existing.Refresh(duration, context);

        OnTagRefreshed?.Invoke(type, existing);
        RaiseTagChanged(type, existing, TagChangeType.Refreshed);

        string changeDescription = stackAdded
            ? $"stack {existing.StackCount}/{GetStackLimit(type)}"
            : $"refreshed ({duration:F2}s)";

        Debug.Log(
            $"[TagHandler] {gameObject.name} -> {type} {changeDescription} | " +
            $"source:{FormatSource(existing)}");
    }

    private bool TryAddStack(TagInstance instance, int maxStacks)
    {
        if (instance.StackCount >= maxStacks)
            return false;

        instance.AddStack();
        return true;
    }

    private int GetStackLimit(TagType type)
    {
        switch (type)
        {
            case TagType.POISON:
                return maxPoisonStacks;

            case TagType.BLEED:
                return maxBleedStacks;

            default:
                return 1;
        }
    }

    private void TickTags()
    {
        if (activeTags.Count == 0)
            return;

        List<TagType> expiredTags = null;

        foreach (KeyValuePair<TagType, TagInstance> pair in activeTags)
        {
            TagInstance instance = pair.Value;
            instance.Tick(Time.deltaTime);

            if (!instance.IsExpired)
                continue;

            if (expiredTags == null)
                expiredTags = new List<TagType>();

            expiredTags.Add(pair.Key);
        }

        if (expiredTags == null)
            return;

        foreach (TagType type in expiredTags)
        {
            if (!activeTags.TryGetValue(type, out TagInstance instance))
                continue;

            activeTags.Remove(type);

            OnTagExpired?.Invoke(type);
            RaiseTagChanged(type, instance, TagChangeType.Expired);

            Debug.Log(
                $"[TagHandler] {gameObject.name} -> TAG expired: {type}");
        }
    }

    private void RaiseTagChanged(
        TagType type,
        TagInstance instance,
        TagChangeType changeType)
    {
        OnTagChanged?.Invoke(
            new TagChangeEvent(type, instance, changeType));
    }

    private static string FormatSource(TagInstance instance)
    {
        if (instance == null)
            return "none";

        if (string.IsNullOrWhiteSpace(instance.SourceId))
            return instance.SourceCategory.ToString();

        return $"{instance.SourceCategory}:{instance.SourceId}";
    }
}