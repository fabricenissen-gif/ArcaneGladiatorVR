using UnityEngine;

[CreateAssetMenu(fileName = "NewSpell", menuName = "SpellDelver/SpellData")]
public class SpellData : ScriptableObject
{
    [Header("Identität")]
    public string spellName;
    public SpellType type;
    public bool isCursed = false;

    [Header("Charge")]
    public float chargeTime = 0.8f;

    [Header("Cooldown")]
    [Tooltip("Cooldown nach dem Cast in Sekunden.")]
    public float cooldown = 0.5f;

    [Header("Schaden")]
    public float baseDamage = 5f;
    public float fullChargedDamage = 25f;

    [Header("Projektil")]
    public ArcaneBoltProjectile projectilePrefab;
    public float baseSpeed = 14f;
    public float fullChargedSpeed = 22f;
    public float baseSize = 1f;
    public float fullChargedSize = 2f;
    public float projectileLifetime = 4f;
}

public enum SpellType
{
    Projectile,
    Area,
    Beam,
    SelfBuff,
    Utility,
    Cursed
}