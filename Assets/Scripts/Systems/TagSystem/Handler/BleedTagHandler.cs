using UnityEngine;

/// <summary>
/// Verursacht periodischen physischen Schaden für jeden aktiven
/// BLEED-Stack auf diesem Ziel.
/// </summary>
[DisallowMultipleComponent]
public sealed class BleedTagHandler : TagDamageOverTimeHandler
{
    protected override TagType DamageTag => TagType.BLEED;

    protected override string DebugTagName => "BLEED";
}