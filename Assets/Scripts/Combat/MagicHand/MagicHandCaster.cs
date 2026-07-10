using UnityEngine;

public class MagicHandCaster : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MagicChargeSystem chargeSystem;
    [SerializeField] private Transform castPoint;

    [Header("Aktiver Spell")]
    [SerializeField] private SpellData activeSpell;

    [Header("Player Health (für Backfire)")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Feedback — Normal")]
    [SerializeField] private ParticleSystem castMuzzleVfx;
    [SerializeField] private AudioSource castAudioSource;
    [SerializeField] private AudioClip castClip;
    [SerializeField] private AudioClip chargedCastClip;

    [Header("Feedback — Cursed")]
    [SerializeField] private AudioClip sweetSpotClip;
    [SerializeField] private AudioClip backfireClip;
    [SerializeField] private AudioClip underchargeClip;
    [SerializeField] private ParticleSystem sweetSpotVfx;
    [SerializeField] private ParticleSystem backfireVfx;
    [SerializeField] private ParticleSystem underchargeVfx;

    public SpellData ActiveSpell => activeSpell;

    private void Awake()
    {
        if (chargeSystem == null)
            chargeSystem = GetComponent<MagicChargeSystem>();

        if (activeSpell != null && chargeSystem != null)
            chargeSystem.ChargeTime = activeSpell.chargeTime;

        if (playerHealth == null)
        {
            Debug.LogError(
                "[MagicHandCaster] PlayerHealth ist nicht im Inspector gesetzt.");
        }
    }

    private void OnEnable()
    {
        if (chargeSystem == null)
            return;

        chargeSystem.OnChargeReleased += OnChargeReleased;
        chargeSystem.OnChargeReached += OnChargeReached;
        chargeSystem.OnAutoOverchargeTriggered +=
            OnAutoOverchargeTriggered;
    }

    private void OnDisable()
    {
        if (chargeSystem == null)
            return;

        chargeSystem.OnChargeReleased -= OnChargeReleased;
        chargeSystem.OnChargeReached -= OnChargeReached;
        chargeSystem.OnAutoOverchargeTriggered -=
            OnAutoOverchargeTriggered;
    }

    private void Update()
    {
        if (activeSpell is CursedSpellData cursedSpell &&
            chargeSystem != null)
        {
            chargeSystem.CheckAutoOvercharge(cursedSpell);
        }
    }

    public void SetSpell(SpellData newSpell)
    {
        activeSpell = newSpell;

        if (chargeSystem != null)
        {
            chargeSystem.ChargeTime =
                newSpell != null
                    ? newSpell.chargeTime
                    : 0.8f;
        }

        if (newSpell != null)
        {
            Debug.Log(
                "[MagicHandCaster] Aktiver Spell gesetzt: " +
                newSpell.spellName);
        }
        else
        {
            Debug.Log("[MagicHandCaster] Aktiver Spell entfernt.");
        }
    }

    private void OnChargeReached()
    {
    }

    private void OnChargeReleased(
        float chargeProgress,
        float overchargeTime)
    {
        if (activeSpell == null)
        {
            Debug.LogWarning(
                "[MagicHandCaster] Kein Spell zugewiesen.");

            return;
        }

        if (activeSpell is CursedSpellData cursedSpell)
        {
            FireCursed(
                cursedSpell,
                chargeProgress,
                overchargeTime,
                wasAutoOvercharged: false);
        }
        else
        {
            FireNormal(chargeProgress);
        }
    }

    private void OnAutoOverchargeTriggered(
        float chargeProgress,
        float overchargeTime)
    {
        if (activeSpell is not CursedSpellData cursedSpell)
            return;

        FireCursed(
            cursedSpell,
            chargeProgress,
            overchargeTime,
            wasAutoOvercharged: true);
    }

    private void FireNormal(float chargeProgress)
    {
        if (activeSpell == null ||
            activeSpell.projectilePrefab == null ||
            castPoint == null ||
            chargeSystem == null)
        {
            return;
        }

        float damage = Mathf.Lerp(
            activeSpell.baseDamage,
            activeSpell.fullChargedDamage,
            chargeProgress);

        float speed = Mathf.Lerp(
            activeSpell.baseSpeed,
            activeSpell.fullChargedSpeed,
            chargeProgress);

        float size = Mathf.Lerp(
            activeSpell.baseSize,
            activeSpell.fullChargedSize,
            chargeProgress);

        SpawnProjectile(
            activeSpell.projectilePrefab,
            damage,
            speed,
            size,
            activeSpell.projectileLifetime,
            activeSpell.spellTag,
            activeSpell.tagDuration,
            activeSpell);

        chargeSystem.StartCooldown(activeSpell.cooldown);

        AudioClip clip =
            chargeProgress >= 0.9f && chargedCastClip != null
                ? chargedCastClip
                : castClip;

        PlayFeedback(clip, null);

        Debug.Log(
            $"[{activeSpell.spellName}] Normal | " +
            $"{chargeProgress * 100f:F0}% " +
            $"DMG:{damage:F1} CD:{activeSpell.cooldown:F1}s");
    }

    private void FireCursed(
        CursedSpellData spell,
        float chargeProgress,
        float overchargeTime,
        bool wasAutoOvercharged)
    {
        if (spell == null || chargeSystem == null)
            return;

        CursedChargeState state = wasAutoOvercharged
            ? CursedChargeState.Overload
            : chargeSystem.GetCursedState(
                spell,
                chargeProgress,
                overchargeTime);

        Debug.Log(
            $"[{spell.spellName}] Cursed | " +
            $"{chargeProgress * 100f:F0}% " +
            $"OvT:{overchargeTime:F2}s -> {state}");

        switch (state)
        {
            case CursedChargeState.Undercharge:
                HandleUndercharge(spell);
                break;

            case CursedChargeState.SweetSpot:
                HandleSweetSpot(spell);
                break;

            case CursedChargeState.Overload:
                HandleOverchargeBackfire(spell);
                break;
        }

        chargeSystem.StartCooldown(spell.cooldown);
    }

    private void HandleUndercharge(CursedSpellData spell)
    {
        switch (spell.underchargeType)
        {
            case UnderchargeType.NoEffect:
                PlayFeedback(underchargeClip, underchargeVfx);

                Debug.Log(
                    $"[{spell.spellName}] UNDERCHARGE - kein Effekt.");
                break;

            case UnderchargeType.WeakEffect:
                if (spell.projectilePrefab != null &&
                    castPoint != null)
                {
                    float weakDamage =
                        spell.fullChargedDamage *
                        spell.underchargeDamageMultiplier;

                    float weakSize =
                        spell.fullChargedSize *
                        spell.underchargeDamageMultiplier;

                    SpawnProjectile(
                        spell.projectilePrefab,
                        weakDamage,
                        spell.baseSpeed,
                        weakSize,
                        spell.projectileLifetime,
                        spell.spellTag,
                        spell.tagDuration *
                            spell.underchargeDamageMultiplier,
                        spell);
                }

                PlayFeedback(underchargeClip, underchargeVfx);

                Debug.Log(
                    $"[{spell.spellName}] UNDERCHARGE - schwacher Effekt.");
                break;

            case UnderchargeType.Backfire:
                if (playerHealth == null)
                {
                    Debug.LogError(
                        "[MagicHandCaster] PlayerHealth fehlt - " +
                        "Undercharge Backfire konnte keinen Schaden anwenden.");

                    return;
                }

                playerHealth.TakeDamage(
                    Mathf.RoundToInt(spell.backfireSelfDamage),
                    Vector3.zero);

                PlayFeedback(backfireClip, backfireVfx);

                Debug.Log(
                    $"[{spell.spellName}] UNDERCHARGE BACKFIRE - " +
                    $"{spell.backfireSelfDamage:F1} Selbstschaden.");
                break;
        }
    }

    private void HandleSweetSpot(CursedSpellData spell)
    {
        if (spell.projectilePrefab == null || castPoint == null)
            return;

        SpawnProjectile(
            spell.projectilePrefab,
            spell.fullChargedDamage,
            spell.fullChargedSpeed,
            spell.fullChargedSize,
            spell.projectileLifetime,
            spell.spellTag,
            spell.tagDuration,
            spell);

        AudioClip clip =
            sweetSpotClip != null
                ? sweetSpotClip
                : chargedCastClip;

        PlayFeedback(clip, sweetSpotVfx);

        Debug.Log(
            $"[{spell.spellName}] SWEET SPOT - voller Effekt.");
    }

    private void HandleOverchargeBackfire(CursedSpellData spell)
    {
        if (playerHealth == null)
        {
            Debug.LogError(
                "[MagicHandCaster] PlayerHealth fehlt - " +
                "Overcharge Backfire konnte keinen Schaden anwenden.");

            return;
        }

        int selfDamage =
            Mathf.RoundToInt(spell.backfireSelfDamage);

        playerHealth.TakeDamage(selfDamage, Vector3.zero);
        PlayFeedback(backfireClip, backfireVfx);

        Debug.Log(
            $"[{spell.spellName}] OVERCHARGE BACKFIRE - " +
            $"{selfDamage} Selbstschaden.");
    }

    private void SpawnProjectile(
        ArcaneBoltProjectile prefab,
        float damage,
        float speed,
        float size,
        float lifetime,
        TagType tag,
        float tagDuration,
        SpellData sourceSpell)
    {
        if (prefab == null || castPoint == null)
            return;

        ArcaneBoltProjectile projectile = Instantiate(
            prefab,
            castPoint.position,
            castPoint.rotation);

        projectile.transform.localScale *= Mathf.Max(0f, size);

        TagApplicationContext context =
            TagApplicationContext.FromSpell(
                sourceSpell,
                GetSpellSourceId(sourceSpell));

        projectile.Initialize(
            damage,
            speed,
            lifetime,
            tag,
            tagDuration,
            context);

        if (castMuzzleVfx != null)
            castMuzzleVfx.Play();
    }

    private static string GetSpellSourceId(SpellData spell)
    {
        if (spell == null)
            return "UnknownSpell";

        if (!string.IsNullOrWhiteSpace(spell.spellName))
            return spell.spellName;

        return spell.name;
    }

    private void PlayFeedback(
        AudioClip clip,
        ParticleSystem vfx)
    {
        if (castAudioSource != null && clip != null)
            castAudioSource.PlayOneShot(clip);

        if (vfx != null)
            vfx.Play();
    }
}