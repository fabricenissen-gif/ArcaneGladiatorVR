using System;
using System.Collections.Generic;
using UnityEngine;

public class TagHandler : MonoBehaviour
{
    [Header("Stack Limits")]
    [Tooltip("Maximale POISON-Stacks — Standard 3, erweiterbar durch Volatile Stack Modifier")]
    [SerializeField] private int maxPoisonStacks = 3;
    [Tooltip("Maximale BLEED-Stacks")]
    [SerializeField] private int maxBleedStacks = 5;

    private readonly Dictionary<TagType, TagInstance> activeTags = new Dictionary<TagType, TagInstance>();

    public event Action<TagType, TagInstance> OnTagApplied;
    public event Action<TagType, TagInstance> OnTagRefreshed;
    public event Action<TagType> OnTagExpired;
    public event Action<TagType> OnTagRemoved;

    private void Update()
    {
        TickTags();
    }

    // --- Public API ---

    public void ApplyTag(TagType type, float duration)
    {
        if (activeTags.TryGetValue(type, out TagInstance existing))
        {
            HandleExistingTag(type, existing, duration);
            return;
        }

        TagInstance newTag = new TagInstance(type, duration);
        activeTags[type] = newTag;

        OnTagApplied?.Invoke(type, newTag);

        Debug.Log("[TagHandler] " + gameObject.name + " → TAG angewandt: " + type + " (" + duration + "s)");
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

    public void RemoveTag(TagType type)
    {
        if (!activeTags.ContainsKey(type)) return;

        activeTags.Remove(type);
        OnTagRemoved?.Invoke(type);

        Debug.Log("[TagHandler] " + gameObject.name + " → TAG entfernt: " + type);
    }

    public void RemoveAllTags()
    {
        List<TagType> keys = new List<TagType>(activeTags.Keys);

        foreach (TagType type in keys)
        {
            activeTags.Remove(type);
            OnTagRemoved?.Invoke(type);
        }

        Debug.Log("[TagHandler] " + gameObject.name + " → Alle TAGs entfernt.");
    }

    public void SetMaxPoisonStacks(int max) { maxPoisonStacks = max; }
    public void SetMaxBleedStacks(int max)  { maxBleedStacks = max; }

    // --- Intern ---

    private void HandleExistingTag(TagType type, TagInstance existing, float duration)
    {
        switch (type)
        {
            case TagType.POISON:
                if (existing.StackCount < maxPoisonStacks)
                {
                    existing.AddStack();
                    existing.Refresh(duration);
                    OnTagRefreshed?.Invoke(type, existing);
                    Debug.Log("[TagHandler] " + gameObject.name + " → POISON Stack " + existing.StackCount + "/" + maxPoisonStacks);
                }
                else
                {
                    existing.Refresh(duration);
                    Debug.Log("[TagHandler] " + gameObject.name + " → POISON max Stacks, nur Refresh");
                }
                break;

            case TagType.BLEED:
                if (existing.StackCount < maxBleedStacks)
                {
                    existing.AddStack();
                    existing.Refresh(duration);
                    OnTagRefreshed?.Invoke(type, existing);
                    Debug.Log("[TagHandler] " + gameObject.name + " → BLEED Stack " + existing.StackCount + "/" + maxBleedStacks);
                }
                else
                {
                    existing.Refresh(duration);
                    Debug.Log("[TagHandler] " + gameObject.name + " → BLEED max Stacks, nur Refresh");
                }
                break;

            case TagType.FROST:
            case TagType.FIRE:
            case TagType.ELECTRIC:
            case TagType.WIND:
            case TagType.ARCANE:
            case TagType.MARKED:
            case TagType.FROZEN:
                existing.Refresh(duration);
                OnTagRefreshed?.Invoke(type, existing);
                Debug.Log("[TagHandler] " + gameObject.name + " → " + type + " refreshed (" + duration + "s)");
                break;

            default:
                existing.Refresh(duration);
                OnTagRefreshed?.Invoke(type, existing);
                break;
        }
    }

    private void TickTags()
    {
        if (activeTags.Count == 0) return;

        List<TagType> expired = null;

        foreach (KeyValuePair<TagType, TagInstance> pair in activeTags)
        {
            pair.Value.Tick(Time.deltaTime);

            if (pair.Value.IsExpired)
            {
                if (expired == null)
                    expired = new List<TagType>();

                expired.Add(pair.Key);
            }
        }

        if (expired == null) return;

        foreach (TagType type in expired)
        {
            activeTags.Remove(type);
            OnTagExpired?.Invoke(type);
            Debug.Log("[TagHandler] " + gameObject.name + " → TAG abgelaufen: " + type);
        }
    }
}