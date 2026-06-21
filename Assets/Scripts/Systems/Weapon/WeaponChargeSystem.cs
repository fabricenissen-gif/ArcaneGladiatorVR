using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Charge-Zustand einer XR-Waffe via Trigger-Hold.
/// SetEffectActive() steuert ParticleSystems korrekt über Play/Stop
/// statt SetActive — das war der Root-Cause des "VFX geht nicht mehr"-Bugs.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponChargeSystem : MonoBehaviour
{
    [Serializable]
    public class ChargedHitUnityEvent : UnityEvent<ChargedHitData> { }

    public struct ChargedHitData
    {
        public Health   health;
        public Collider hitCollider;
        public Vector3  hitDirection;
        public Vector3  hitPoint;
        public float    baseDamage;
        public float    finalDamage;

        public ChargedHitData(Health h, Collider c, Vector3 dir, Vector3 pt, float bd, float fd)
        { health = h; hitCollider = c; hitDirection = dir; hitPoint = pt; baseDamage = bd; finalDamage = fd; }
    }

    [Header("Charge Settings")]
    [SerializeField] private float timeToCharge = 1.5f;

    [Header("VFX")]
    [Tooltip("GameObject mit optionalem ParticleSystem — wird über Play/Stop gesteuert, nicht SetActive")]
    [SerializeField] private GameObject chargedEffectObject;

    [Header("Events")]
    public UnityEvent              onChargeStarted;
    public UnityEvent              onCharged;
    public UnityEvent              onChargeConsumed;
    public ChargedHitUnityEvent    onChargedHit;

    public event Action               ChargeStarted;
    public event Action               Charged;
    public event Action               ChargeConsumed;
    public event Action<ChargedHitData> ChargedHit;

    public bool  IsCharged      { get; private set; }
    public float ChargeProgress => Mathf.Clamp01(chargeTimer / Mathf.Max(timeToCharge, 0.0001f));

    private XRGrabInteractable    grabInteractable;
    private XRBaseInputInteractor currentInteractor;

    private bool  isHoldingTrigger;
    private bool  chargeStarted;
    private float chargeTimer;

    // ─────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        HardReset();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
        grabInteractable.activated.AddListener(OnActivatePressed);
        grabInteractable.deactivated.AddListener(OnActivateReleased);
        HardReset();
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
        grabInteractable.activated.RemoveListener(OnActivatePressed);
        grabInteractable.deactivated.RemoveListener(OnActivateReleased);
        HardReset();
    }

    private void Update()
    {
        if (!isHoldingTrigger || IsCharged) return;

        chargeTimer += Time.deltaTime;

        if (currentInteractor != null && Time.frameCount % 5 == 0)
            currentInteractor.SendHapticImpulse(ChargeProgress * 0.3f, 0.05f);

        if (chargeTimer >= timeToCharge)
            FullyCharge();
    }

    // ─────────────────────────────────────────
    // XR Events
    // ─────────────────────────────────────────

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRBaseInputInteractor inp)
            currentInteractor = inp;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHoldingTrigger  = false;
        chargeStarted     = false;
        currentInteractor = null;

        if (args.interactorObject is XRSocketInteractor)
            ExpendCharge();  // Socket → Charge konsumieren
        else
            HardReset();     // Loslassen → alles zurücksetzen
    }

    private void OnActivatePressed(ActivateEventArgs args)
    {
        if (IsCharged) return;
        isHoldingTrigger = true;

        if (!chargeStarted)
        {
            chargeStarted = true;
            onChargeStarted?.Invoke();
            ChargeStarted?.Invoke();
        }
    }

    private void OnActivateReleased(DeactivateEventArgs args)
    {
        if (IsCharged) return;
        isHoldingTrigger = false;
        chargeStarted    = false;
        chargeTimer      = 0f;
    }

    // ─────────────────────────────────────────
    // Charge State
    // ─────────────────────────────────────────

    private void FullyCharge()
    {
        IsCharged        = true;
        isHoldingTrigger = false;
        chargeStarted    = false;
        chargeTimer      = timeToCharge;

        SetEffectActive(true);
        currentInteractor?.SendHapticImpulse(1f, 0.2f);

        onCharged?.Invoke();
        Charged?.Invoke();
        Debug.Log($"[WeaponChargeSystem] {gameObject.name} FULLY CHARGED");
    }

    public void ExpendCharge()
    {
        if (!IsCharged) return;

        IsCharged        = false;
        isHoldingTrigger = false;
        chargeStarted    = false;
        chargeTimer      = 0f;

        SetEffectActive(false);
        onChargeConsumed?.Invoke();
        ChargeConsumed?.Invoke();
        Debug.Log($"[WeaponChargeSystem] {gameObject.name} charge EXPENDED");
    }

    public void NotifyChargedHit(Health h, Collider c, Vector3 dir, Vector3 pt, float bd, float fd)
    {
        ChargedHitData data = new ChargedHitData(h, c, dir, pt, bd, fd);
        onChargedHit?.Invoke(data);
        ChargedHit?.Invoke(data);
    }

    // ─────────────────────────────────────────
    // Helper
    // ─────────────────────────────────────────

    private void HardReset()
    {
        IsCharged        = false;
        isHoldingTrigger = false;
        chargeStarted    = false;
        chargeTimer      = 0f;
        SetEffectActive(false);
    }

    /// ParticleSystem korrekt über Play/Stop steuern — SetActive allein
    /// reicht nicht da PlayOnAwake den Effekt beim nächsten Activate neu startet.
    private void SetEffectActive(bool active)
    {
        if (chargedEffectObject == null) return;

        ParticleSystem ps = chargedEffectObject.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            if (active)
            {
                chargedEffectObject.SetActive(true);
                ps.Play();
            }
            else
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                chargedEffectObject.SetActive(false);
            }
        }
        else
        {
            chargedEffectObject.SetActive(active);
        }
    }
}