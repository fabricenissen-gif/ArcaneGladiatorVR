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
    [SerializeField] private AudioSource chargeAudioSource;
    [SerializeField] private AudioClip chargeLoopClip;
    [SerializeField] private AudioClip chargedReadyClip;

    public float ChargeProgress => IsCharging
        ? Mathf.Clamp01(chargeTimer / ChargeTime)
        : 0f;

    public bool IsCharging { get; private set; } = false;
    public bool IsCharged { get; private set; } = false;

    public bool IsOnCooldown => cooldownTimer > 0f;
    public float CooldownProgress => cooldownTimer > 0f
        ? Mathf.Clamp01(cooldownTimer / currentCooldownDuration)
        : 0f;

    private float chargeTimer = 0f;
    private bool reachedFullCharge = false;
    private float cooldownTimer = 0f;
    private float currentCooldownDuration = 0f;

    public System.Action OnChargeStarted;
    public System.Action OnChargeReached;
    public System.Action<float> OnChargeReleased;

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

    private void OnHoldStarted(InputAction.CallbackContext ctx) => BeginCharge();
    private void OnHoldReleased(InputAction.CallbackContext ctx) => ReleaseCharge();

    public void BeginCharge()
    {
        if (IsCharging || IsOnCooldown) return;

        IsCharging = true;
        IsCharged = false;
        reachedFullCharge = false;
        chargeTimer = 0f;

        OnChargeStarted?.Invoke();

        if (chargingVfx != null) chargingVfx.Play();
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

        float releaseProgress = ChargeProgress;

        IsCharging = false;
        IsCharged = false;
        reachedFullCharge = false;

        StopAllVfx();
        if (chargeAudioSource != null) chargeAudioSource.Stop();

        OnChargeReleased?.Invoke(releaseProgress);
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
    }
}