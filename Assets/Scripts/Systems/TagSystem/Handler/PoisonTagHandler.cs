using UnityEngine;

/// <summary>
/// Verursacht periodischen Giftschaden für jeden aktiven POISON-Stack
/// auf diesem Ziel.
/// </summary>
[DisallowMultipleComponent]
public sealed class PoisonTagHandler : TagDamageOverTimeHandler
{
    protected override TagType DamageTag => TagType.POISON;

    protected override string DebugTagName => "POISON";
}