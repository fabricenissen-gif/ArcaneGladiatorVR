using UnityEngine;

[CreateAssetMenu(fileName = "SpellLoadout", menuName = "SpellDelver/Spell Loadout Data")]
public class SpellLoadoutData : ScriptableObject
{
    [Header("Left Hand Spell Builds")]
    public SpellData buildASpell;
    public SpellData buildBSpell;
}