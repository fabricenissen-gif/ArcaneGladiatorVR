using UnityEngine;
using UnityEngine.InputSystem;

public class MagicChargeSystem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference chargeAction;

    [Header("Charge Settings")]
    [field: SerializeField] public float ChargeTime { get; set; } = 0.8f;

    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem chargingVfx;
    [SerializeField] private ParticleSystem chargedReadyVfx;
    [SerializeField] private ParticleSystem overloadVfx;
    [SerializeField] private AudioSource chargeAudioSource;
    [SerializeField] private AudioClip chargeLoopClip;
    [SerializeField] private AudioClip chargedReadyClip;
    [SerializeField] private AudioClip overloadWarningClip;

    public float ChargeProgress => IsCharging
        ? Mathf.Clamp01(chargeTimer / ChargeTime)
        : 0f;

    public float OverchargeTime => IsCharging && reachedFullCharge
        ? chargeTimer - ChargeTime
        : 0f;

    public bool IsCharging { get; private set; } = false;
    public bool IsCharged { get; private set; } = false;

    public bool IsOnCooldown => cooldownTimer > 0f;
    public float CooldownProgress => cooldownTimer > 0f
        ? Mathf.Clamp01(cooldownTimer / currentCooldownDuration)
        : 0f;

    private float chargeTimer = 0f;
    private bool reachedFullCharge = false;
    private bool overloadWarningPlayed = false;
    private bool autoOverchargeTriggered = false;
    private float cooldownTimer = 0f;
    private float currentCooldownDuration = 0f;

    public System.Action OnChargeStarted;
    public System.Action OnChargeReached;
    public System.Action OnOverloadWarning;
    public System.Action<float, float> OnChargeReleased;
    public System.Action<float, float> OnAutoOverchargeTriggered;

    private void OnEnable()
    {
        if (chargeAction?.action == null) return;
        chargeAction.action.Enable();
        chargeAction.action.started += OnHoldStarted;
        chargeAction.action.canceled += OnHoldReleased;
    }

    private void OnDisable()
    {
        if (chargeAction?.action == null) return;
        chargeAction.action.started -= OnHoldStarted;
        chargeAction.action.canceled -= OnHoldReleased;
        chargeAction.action.Disable();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (!IsCharging) return;

        chargeTimer += Time.deltaTime;

        if (!reachedFullCharge && chargeTimer >= ChargeTime)
        {
            reachedFullCharge = true;
            IsCharged = true;
            OnChargeReached?.Invoke();
            PlayChargedVfx();
        }
    }

    public void StartCooldown(float duration)
    {
        cooldownTimer = duration;
        currentCooldownDuration = duration;
    }

    public CursedChargeState GetCursedState(CursedSpellData spell, float chargeProgress, float overchargeTime)
    {
        if (chargeProgress < 1f)
            return CursedChargeState.Undercharge;

        if (overchargeTime >= spell.overloadDelay)
            return CursedChargeState.Overload;

        return CursedChargeState.SweetSpot;
    }

    public void CheckAutoOvercharge(CursedSpellData spell)
    {
        if (!IsCharging) return;
        if (!reachedFullCharge) return;
        if (autoOverchargeTriggered) return;

        if (OverchargeTime >= spell.overloadDelay)
        {
            autoOverchargeTriggered = true;
            TriggerOverloadWarning();
            ForceAutoOverchargeRelease();
        }
    }

    public void TriggerOverloadWarning()
    {
        if (overloadWarningPlayed) return;
        overloadWarningPlayed = true;

        OnOverloadWarning?.Invoke();

        if (chargedReadyVfx != null) chargedReadyVfx.Stop();
        if (overloadVfx != null) overloadVfx.Play();

        if (chargeAudioSource != null && overloadWarningClip != null)
            chargeAudioSource.PlayOneShot(overloadWarningClip);
    }

    private void ForceAutoOverchargeRelease()
    {
        if (!IsCharging) return;

        float releaseProgress = Mathf.Clamp01(chargeTimer / ChargeTime);
        float releaseOverchargeTime = reachedFullCharge ? chargeTimer - ChargeTime : 0f;

        IsCharging = false;
        IsCharged = false;
        reachedFullCharge = false;
        overloadWarningPlayed = false;

        StopAllVfx();

        if (chargeAudioSource != null)
            chargeAudioSource.Stop();

        OnAutoOverchargeTriggered?.Invoke(releaseProgress, releaseOverchargeTime);
        chargeTimer = 0f;
    }

    private void OnHoldStarted(InputAction.CallbackContext ctx) => BeginCharge();
    private void OnHoldReleased(InputAction.CallbackContext ctx) => ReleaseCharge();

    public void BeginCharge()
    {
        if (IsCharging || IsOnCooldown) return;

        IsCharging = true;
        IsCharged = false;
        reachedFullCharge = false;
        overloadWarningPlayed = false;
        autoOverchargeTriggered = false;
        chargeTimer = 0f;

        OnChargeStarted?.Invoke();

        if (chargingVfx != null)
            chargingVfx.Play();

        if (chargeAudioSource != null && chargeLoopClip != null)
        {
            chargeAudioSource.clip = chargeLoopClip;
            chargeAudioSource.loop = true;
            chargeAudioSource.Play();
        }
    }

    public void ReleaseCharge()
    {
        if (!IsCharging) return;
        if (autoOverchargeTriggered) return;

        float releaseProgress = Mathf.Clamp01(chargeTimer / ChargeTime);
        float releaseOverchargeTime = reachedFullCharge ? chargeTimer - ChargeTime : 0f;

        IsCharging = false;
        IsCharged = false;
        reachedFullCharge = false;
        overloadWarningPlayed = false;

        StopAllVfx();

        if (chargeAudioSource != null)
            chargeAudioSource.Stop();

        OnChargeReleased?.Invoke(releaseProgress, releaseOverchargeTime);
        chargeTimer = 0f;
    }

    private void PlayChargedVfx()
    {
        if (chargingVfx != null) chargingVfx.Stop();
        if (chargedReadyVfx != null) chargedReadyVfx.Play();

        if (chargeAudioSource != null)
        {
            chargeAudioSource.Stop();
            if (chargedReadyClip != null)
                chargeAudioSource.PlayOneShot(chargedReadyClip);
        }
    }

    private void StopAllVfx()
    {
        if (chargingVfx != null) chargingVfx.Stop();
        if (chargedReadyVfx != null) chargedReadyVfx.Stop();
        if (overloadVfx != null) overloadVfx.Stop();
    }
}