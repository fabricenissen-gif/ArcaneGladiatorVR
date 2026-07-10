using UnityEngine;

public enum TagSourceCategory
{
    Unknown,
    Weapon,
    Spell,
    Modifier,
    Reaction,
    Environment
}

public readonly struct TagApplicationContext
{
    public static readonly TagApplicationContext Unknown =
        new TagApplicationContext(
            null,
            TagSourceCategory.Unknown,
            string.Empty,
            canTriggerReactions: true,
            canSpread: false,
            isConsumedOnReaction: true);

    public Object Source { get; }
    public TagSourceCategory SourceCategory { get; }
    public string SourceId { get; }
    public bool CanTriggerReactions { get; }
    public bool CanSpread { get; }
    public bool IsConsumedOnReaction { get; }

    public TagApplicationContext(
        Object source,
        TagSourceCategory sourceCategory,
        string sourceId,
        bool canTriggerReactions = true,
        bool canSpread = false,
        bool isConsumedOnReaction = true)
    {
        Source = source;
        SourceCategory = sourceCategory;
        SourceId = sourceId ?? string.Empty;
        CanTriggerReactions = canTriggerReactions;
        CanSpread = canSpread;
        IsConsumedOnReaction = isConsumedOnReaction;
    }

    public static TagApplicationContext FromWeapon(
        Object source,
        string sourceId = "")
    {
        return new TagApplicationContext(
            source,
            TagSourceCategory.Weapon,
            sourceId);
    }

    public static TagApplicationContext FromSpell(
        Object source,
        string sourceId = "")
    {
        return new TagApplicationContext(
            source,
            TagSourceCategory.Spell,
            sourceId);
    }

    public static TagApplicationContext FromModifier(
        Object source,
        string sourceId)
    {
        return new TagApplicationContext(
            source,
            TagSourceCategory.Modifier,
            sourceId);
    }

    public static TagApplicationContext FromReaction(
        Object source,
        string sourceId = "")
    {
        return new TagApplicationContext(
            source,
            TagSourceCategory.Reaction,
            sourceId,
            canTriggerReactions: false,
            canSpread: false,
            isConsumedOnReaction: false);
    }
}

public class TagInstance
{
    public TagType Type { get; }

    public float Duration { get; private set; }
    public float RemainingTime { get; private set; }
    public int StackCount { get; private set; }

    public float AppliedTime { get; }
    public float LastRefreshTime { get; private set; }

    public Object Source { get; private set; }
    public TagSourceCategory SourceCategory { get; private set; }
    public string SourceId { get; private set; }

    public bool CanTriggerReactions { get; private set; }
    public bool CanSpread { get; private set; }
    public bool IsConsumedOnReaction { get; private set; }

    public bool IsExpired => RemainingTime <= 0f;

    public TagInstance(
        TagType type,
        float duration,
        int initialStacks = 1)
        : this(
            type,
            duration,
            TagApplicationContext.Unknown,
            initialStacks)
    {
    }

    public TagInstance(
        TagType type,
        float duration,
        TagApplicationContext context,
        int initialStacks = 1)
    {
        Type = type;
        Duration = Mathf.Max(0f, duration);
        RemainingTime = Duration;
        StackCount = Mathf.Max(1, initialStacks);

        AppliedTime = Time.time;
        LastRefreshTime = AppliedTime;

        ApplyContext(context);
    }

    public void Tick(float deltaTime)
    {
        RemainingTime -= Mathf.Max(0f, deltaTime);
    }

    public void Refresh(float newDuration)
    {
        Refresh(newDuration, GetCurrentContext());
    }

    public void Refresh(
        float newDuration,
        TagApplicationContext context)
    {
        Duration = Mathf.Max(0f, newDuration);
        RemainingTime = Duration;
        LastRefreshTime = Time.time;

        ApplyContext(context);
    }

    public void AddStack()
    {
        StackCount++;
    }

    public void SetStacks(int count)
    {
        StackCount = Mathf.Max(0, count);
    }

    private TagApplicationContext GetCurrentContext()
    {
        return new TagApplicationContext(
            Source,
            SourceCategory,
            SourceId,
            CanTriggerReactions,
            CanSpread,
            IsConsumedOnReaction);
    }

    private void ApplyContext(TagApplicationContext context)
    {
        Source = context.Source;
        SourceCategory = context.SourceCategory;
        SourceId = context.SourceId ?? string.Empty;
        CanTriggerReactions = context.CanTriggerReactions;
        CanSpread = context.CanSpread;
        IsConsumedOnReaction = context.IsConsumedOnReaction;
    }
}