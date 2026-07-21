using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TagHandler))]
[RequireComponent(typeof(Health))]
public sealed class ReactionResolver : MonoBehaviour
{
    [Header("Definitions")]
    [SerializeField] private List<ReactionDefinition> definitions =
        new List<ReactionDefinition>();

    [Header("Effects")]
    [Tooltip(
        "Komponenten auf diesem Objekt, die Reaction-Effekte ausführen.")]
    [SerializeField] private List<MonoBehaviour> effectComponents =
        new List<MonoBehaviour>();

    [Header("Migration")]
    [Tooltip(
        "Nur hier aufgeführte Effekte werden aktuell vom neuen Resolver " +
        "ausgeführt. Alle übrigen Definitionen bleiben Observe-only.")]
    [SerializeField] private List<ReactionEffectId> liveEffectIds =
        new List<ReactionEffectId>
        {
            ReactionEffectId.SteamExplosion
        };

    [Header("Debug")]
    [SerializeField] private bool logMatches = true;
    [SerializeField] private bool logSkippedDefinitions = true;

    private readonly List<ReactionDefinition> sortedDefinitions =
        new List<ReactionDefinition>();

    private readonly Dictionary<ReactionEffectId, IReactionEffect> effects =
        new Dictionary<ReactionEffectId, IReactionEffect>();

    private TagHandler tagHandler;
    private Health health;
    private bool isResolving;

    private void Awake()
    {
        tagHandler = GetComponent<TagHandler>();
        health = GetComponent<Health>();

        RebuildDefinitionCache();
        RebuildEffectCache();
    }

    private void OnEnable()
    {
        if (tagHandler != null)
            tagHandler.OnTagChanged += OnTagChanged;
    }

    private void OnDisable()
    {
        if (tagHandler != null)
            tagHandler.OnTagChanged -= OnTagChanged;
    }

    private void OnValidate()
    {
        RebuildDefinitionCache();
        RebuildEffectCache();
    }

    private void OnTagChanged(TagHandler.TagChangeEvent tagEvent)
    {
        if (isResolving || health == null || health.IsDead)
            return;

        if (tagEvent.ChangeType != TagHandler.TagChangeType.Applied &&
            tagEvent.ChangeType != TagHandler.TagChangeType.Refreshed)
        {
            return;
        }

        TagInstance triggerInstance = tagEvent.Instance;

        if (triggerInstance == null ||
            !triggerInstance.CanTriggerReactions)
        {
            return;
        }

        if (!TryFindBestReaction(
                tagEvent.TagType,
                out ReactionDefinition definition))
        {
            return;
        }

        if (!liveEffectIds.Contains(definition.EffectId))
        {
            if (logSkippedDefinitions)
            {
                Debug.Log(
                    $"[ReactionResolver] Observe match on " +
                    $"'{gameObject.name}' | " +
                    $"{definition.ReactionId} | " +
                    $"Effect:{definition.EffectId}");
            }

            return;
        }

        ResolveLiveReaction(definition, tagEvent.TagType);
    }

    public bool TryFindBestReaction(
        TagType triggerTag,
        out ReactionDefinition matchedDefinition)
    {
        matchedDefinition = null;

        if (tagHandler == null || health == null || health.IsDead)
            return false;

        for (int i = 0; i < sortedDefinitions.Count; i++)
        {
            ReactionDefinition definition = sortedDefinitions[i];

            if (definition == null ||
                !definition.IsAutomaticSystemReaction)
            {
                continue;
            }

            if (!definition.Matches(tagHandler, triggerTag))
                continue;

            matchedDefinition = definition;
            return true;
        }

        return false;
    }

    private void ResolveLiveReaction(
        ReactionDefinition definition,
        TagType triggerTag)
    {
        if (!effects.TryGetValue(
                definition.EffectId,
                out IReactionEffect effect))
        {
            Debug.LogError(
                $"[ReactionResolver] No effect component for " +
                $"'{definition.EffectId}' on '{gameObject.name}'. " +
                "Tags were not consumed.",
                this);

            return;
        }

        TagInstance firstTag =
            tagHandler.GetTag(definition.FirstRequiredTag);

        TagInstance secondTag =
            tagHandler.GetTag(definition.SecondRequiredTag);

        if (firstTag == null || secondTag == null)
            return;

        if (definition.ConsumeRequiredTags &&
            (!firstTag.IsConsumedOnReaction ||
             !secondTag.IsConsumedOnReaction))
        {
            Debug.Log(
                $"[ReactionResolver] '{definition.ReactionId}' was " +
                "blocked because one required tag may not be consumed.");

            return;
        }

        ReactionContext context = new ReactionContext(
            definition,
            tagHandler,
            health,
            triggerTag,
            firstTag,
            secondTag);

        isResolving = true;

        try
        {
            if (definition.ConsumeRequiredTags)
            {
                tagHandler.RemoveTag(definition.FirstRequiredTag);
                tagHandler.RemoveTag(definition.SecondRequiredTag);
            }

            bool wasExecuted = effect.Execute(context);

            if (logMatches)
            {
                Debug.Log(
                    $"[ReactionResolver] Live resolution on " +
                    $"'{gameObject.name}' | " +
                    $"{definition.ReactionId} | " +
                    $"Effect:{definition.EffectId} | " +
                    $"Executed:{wasExecuted}");
            }
        }
        finally
        {
            isResolving = false;
        }
    }

    private void RebuildDefinitionCache()
    {
        sortedDefinitions.Clear();

        if (definitions == null)
            return;

        HashSet<string> seenReactionIds =
            new HashSet<string>();

        for (int i = 0; i < definitions.Count; i++)
        {
            ReactionDefinition definition = definitions[i];

            if (definition == null)
                continue;

            if (!definition.IsValidDefinition())
            {
                Debug.LogWarning(
                    $"[ReactionResolver] '{name}' ignores invalid " +
                    $"definition '{definition.name}'.",
                    this);

                continue;
            }

            if (!seenReactionIds.Add(definition.ReactionId))
            {
                Debug.LogWarning(
                    $"[ReactionResolver] Duplicate ReactionId " +
                    $"'{definition.ReactionId}'. Only the first is used.",
                    this);

                continue;
            }

            sortedDefinitions.Add(definition);
        }

        sortedDefinitions.Sort(CompareDefinitions);
    }

    private void RebuildEffectCache()
    {
        effects.Clear();

        if (effectComponents == null)
            return;

        for (int i = 0; i < effectComponents.Count; i++)
        {
            MonoBehaviour component = effectComponents[i];

            if (!(component is IReactionEffect effect))
                continue;

            if (effects.ContainsKey(effect.EffectId))
            {
                Debug.LogWarning(
                    $"[ReactionResolver] Multiple effect components " +
                    $"for '{effect.EffectId}' on '{name}'. " +
                    "Only the first is used.",
                    this);

                continue;
            }

            effects.Add(effect.EffectId, effect);
        }
    }

    private static int CompareDefinitions(
        ReactionDefinition first,
        ReactionDefinition second)
    {
        int priorityComparison =
            second.Priority.CompareTo(first.Priority);

        if (priorityComparison != 0)
            return priorityComparison;

        return string.CompareOrdinal(
            first.ReactionId,
            second.ReactionId);
    }
}