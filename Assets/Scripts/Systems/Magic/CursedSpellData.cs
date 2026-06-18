using UnityEngine;

[CreateAssetMenu(fileName = "NewCursedSpell", menuName = "SpellDelver/CursedSpellData")]
public class CursedSpellData : SpellData
{
    [Header("Cursed Timing")]
    [Tooltip("Ab hier beginnt Overload/Overcharge (Sekunden nach Full Charge)")]
    public float overloadDelay = 0.5f;

    [Header("Undercharge")]
    [Tooltip("Was passiert bei zu wenig Charge?")]
    public UnderchargeType underchargeType = UnderchargeType.WeakEffect;

    [Tooltip("Selbstschaden bei Overcharge Backfire oder optionalem Undercharge Backfire")]
    public float backfireSelfDamage = 15f;

    [Tooltip("Schadenmultiplikator bei Undercharge WeakEffect — z.B. 0.2 = 20% des vollen Schadens")]
    public float underchargeDamageMultiplier = 0.2f;
}

public enum UnderchargeType
{
    NoEffect,
    WeakEffect,
    Backfire
}