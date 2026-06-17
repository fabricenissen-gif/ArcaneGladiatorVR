using UnityEngine;

public class MagicHandCaster : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MagicChargeSystem chargeSystem;
    [SerializeField] private Transform castPoint;

    [Header("Aktiver Spell")]
    [SerializeField] private SpellData activeSpell;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem castMuzzleVfx;
    [SerializeField] private AudioSource castAudioSource;
    [SerializeField] private AudioClip castClip;
    [SerializeField] private AudioClip chargedCastClip;

    private void Awake()
    {
        if (chargeSystem == null)
            chargeSystem = GetComponent<MagicChargeSystem>();

        if (activeSpell != null)
            chargeSystem.ChargeTime = activeSpell.chargeTime;
    }

    private void OnEnable()
    {
        if (chargeSystem != null)
            chargeSystem.OnChargeReleased += OnChargeReleased;
    }

    private void OnDisable()
    {
        if (chargeSystem != null)
            chargeSystem.OnChargeReleased -= OnChargeReleased;
    }

    public void SetSpell(SpellData newSpell)
    {
        activeSpell = newSpell;
        if (chargeSystem != null && newSpell != null)
            chargeSystem.ChargeTime = newSpell.chargeTime;
    }

    private void OnChargeReleased(float chargeProgress)
    {
        if (activeSpell == null)
        {
            Debug.LogWarning("MagicHandCaster: Kein Spell zugewiesen.");
            return;
        }

        Fire(chargeProgress);
    }

    private void Fire(float chargeProgress)
    {
        if (activeSpell.projectilePrefab == null || castPoint == null) return;

        float damage = Mathf.Lerp(activeSpell.baseDamage, activeSpell.fullChargedDamage, chargeProgress);
        float speed  = Mathf.Lerp(activeSpell.baseSpeed,  activeSpell.fullChargedSpeed,  chargeProgress);
        float size   = Mathf.Lerp(activeSpell.baseSize,   activeSpell.fullChargedSize,   chargeProgress);

        ArcaneBoltProjectile projectile = Instantiate(
            activeSpell.projectilePrefab,
            castPoint.position,
            castPoint.rotation
        );

        projectile.transform.localScale *= size;
        projectile.Initialize(damage, speed, activeSpell.projectileLifetime);

        chargeSystem.StartCooldown(activeSpell.cooldown);

        AudioClip clip = chargeProgress >= 0.9f && chargedCastClip != null
            ? chargedCastClip
            : castClip;

        if (castMuzzleVfx != null) castMuzzleVfx.Play();
        if (castAudioSource != null && clip != null)
            castAudioSource.PlayOneShot(clip);

        Debug.Log($"[{activeSpell.spellName}] Fired at {chargeProgress * 100f:F0}% " +
                  $"| DMG:{damage:F1} SPD:{speed:F1} SIZE:{size:F2} " +
                  $"| Cooldown: {activeSpell.cooldown}s");
    }
}