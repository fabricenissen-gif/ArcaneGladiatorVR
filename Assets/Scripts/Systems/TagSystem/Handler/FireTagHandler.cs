using UnityEngine;

/// <summary>
/// Verursacht periodischen Brandschaden, solange FIRE auf dem Ziel aktiv ist.
/// FIRE hat derzeit nur einen Stack und wird bei erneuter Anwendung
/// in seiner Dauer refreshed.
/// </summary>
[DisallowMultipleComponent]
public sealed class FireTagHandler : TagDamageOverTimeHandler
{
    protected override TagType DamageTag => TagType.FIRE;

    protected override string DebugTagName => "FIRE";
}