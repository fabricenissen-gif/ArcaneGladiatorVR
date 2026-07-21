using UnityEngine;

/// <summary>
/// Identifiziert den Effekt, den ein späterer ReactionEffectController
/// ausführt. Die Definition enthält nur Daten und keine Gameplay-Logik.
/// </summary>
public enum ReactionEffectId
{
    None = 0,
    SteamExplosion = 1,
    Shatter = 2,
    Blizzard = 3,
    ArcaneIgnite = 4,
    VenomBurst = 5,
    Thunderstrike = 6,
    Exposed = 7,
    ToxicFumes = 8
}

/// <summary>
/// Datengetriebene Definition einer Zwei-Tag-Reaktion.
///
/// In Phase 1 wird das Asset ausschließlich vom ReactionResolver im
/// Observe Mode geprüft. Das bestehende TagReactionSystem bleibt der
/// alleinige Besitzer von Konsum und Effekt-Ausführung.
/// </summary>
[CreateAssetMenu(
    fileName = "ReactionDefinition",
    menuName = "SpellDelver/Tag System/Reaction Definition")]
public sealed class ReactionDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string reactionId = "reaction.new";

    [SerializeField] private string displayName = "New Reaction";

    [SerializeField] private ReactionEffectId effectId =
        ReactionEffectId.None;

    [Header("Required Tags")]
    [SerializeField] private TagType firstRequiredTag = TagType.FIRE;

    [SerializeField] private TagType secondRequiredTag = TagType.FROST;

    [Header("Resolution")]
    [Tooltip(
        "Höherer Wert gewinnt, wenn mehrere Reaktionen zugleich möglich sind.")]
    [SerializeField] private int priority;

    [Tooltip(
        "Beschreibt die gewünschte spätere Konsum-Regel. " +
        "In Phase 1 wird noch nichts durch diese Definition konsumiert.")]
    [SerializeField] private bool consumeRequiredTags = true;

    [Tooltip(
        "Nur aktiv, wenn diese Kombination als automatische " +
        "Systemreaktion erlaubt sein soll.")]
    [SerializeField] private bool isAutomaticSystemReaction = true;

    public string ReactionId => reactionId;
    public string DisplayName => displayName;
    public ReactionEffectId EffectId => effectId;
    public TagType FirstRequiredTag => firstRequiredTag;
    public TagType SecondRequiredTag => secondRequiredTag;
    public int Priority => priority;
    public bool ConsumeRequiredTags => consumeRequiredTags;
    public bool IsAutomaticSystemReaction =>
        isAutomaticSystemReaction;

    /// <summary>
    /// Prüft rein datenbasiert, ob genau diese Definition auf den
    /// aktuellen Tag-Trigger und den Zustand des Ziels passt.
    /// </summary>
    public bool Matches(TagHandler tagHandler, TagType triggerTag)
    {
        if (tagHandler == null)
            return false;

        if (!IsValidDefinition())
            return false;

        bool wasTriggeredByRequiredTag =
            triggerTag == firstRequiredTag ||
            triggerTag == secondRequiredTag;

        if (!wasTriggeredByRequiredTag)
            return false;

        return tagHandler.HasTag(firstRequiredTag) &&
               tagHandler.HasTag(secondRequiredTag);
    }

    public bool IsValidDefinition()
    {
        if (string.IsNullOrWhiteSpace(reactionId))
            return false;

        if (effectId == ReactionEffectId.None)
            return false;

        if (firstRequiredTag == TagType.NONE ||
            secondRequiredTag == TagType.NONE)
        {
            return false;
        }

        return firstRequiredTag != secondRequiredTag;
    }

    private void OnValidate()
    {
        reactionId = reactionId?.Trim() ?? string.Empty;
        displayName = displayName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = reactionId;
    }
}