using UnityEngine;

/// <summary>
/// Laufzeitdaten einer bereits validierten und konsumierten Reaktion.
/// Effekt-Controller erhalten ausschließlich diesen Context und müssen
/// selbst weder Tag-Matching noch Tag-Konsum implementieren.
/// </summary>
public readonly struct ReactionContext
{
    public ReactionDefinition Definition { get; }
    public TagHandler TagHandler { get; }
    public Health Health { get; }
    public TagType TriggerTag { get; }
    public TagInstance FirstTagSnapshot { get; }
    public TagInstance SecondTagSnapshot { get; }

    public GameObject TargetGameObject =>
        Health != null ? Health.gameObject : null;

    public Transform TargetTransform =>
        Health != null ? Health.transform : null;

    public ReactionContext(
        ReactionDefinition definition,
        TagHandler tagHandler,
        Health health,
        TagType triggerTag,
        TagInstance firstTagSnapshot,
        TagInstance secondTagSnapshot)
    {
        Definition = definition;
        TagHandler = tagHandler;
        Health = health;
        TriggerTag = triggerTag;
        FirstTagSnapshot = firstTagSnapshot;
        SecondTagSnapshot = secondTagSnapshot;
    }
}