/// <summary>
/// Gemeinsame Schnittstelle für modulare Reaction-Effekte.
/// Ein Effekt erhält nur einen validierten ReactionContext und gibt
/// zurück, ob er erfolgreich ausgeführt wurde.
/// </summary>
public interface IReactionEffect
{
    ReactionEffectId EffectId { get; }

    bool Execute(ReactionContext context);
}