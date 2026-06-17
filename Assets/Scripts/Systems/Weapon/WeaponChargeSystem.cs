using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class WeaponChargeSystem : MonoBehaviour
{
    [Serializable]
    public class ChargedHitUnityEvent : UnityEvent<ChargedHitData> { }

    public struct ChargedHitData
    {
        public Health health;
        public Collider hitCollider;
        public Vector3 hitDirection;
        public Vector3 hitPoint;
        public float baseDamage;
        public float finalDamage;

        public ChargedHitData(Health h, Collider c, Vector3 dir, Vector3 pt, float bd, float fd)
        {
            health = h;
            hitCollider = c;
            hitDirection = dir;
            hitPoint = pt;
            baseDamage = bd;
            finalDamage = fd;
        }
    }

    [Header("Charge Settings")]
    [SerializeField] private float timeToCharge = 1.5f;

    [Header("Feedback (Optional)")]
    [SerializeField] private GameObject chargedEffectObject;

    [Header("Inspector Events")]
    public UnityEvent onChargeStarted;
    public UnityEvent onCharged;
    public UnityEvent onChargeConsumed;
    public ChargedHitUnityEvent onChargedHit;

    private XRGrabInteractable grabInteractable;
    private XRBaseInputInteractor currentInteractor;

    private bool isHoldingTrigger;
    private bool chargeStarted;
    private float currentChargeTimer;

    public bool IsCharged { get; private set; }
    public float ChargeProgress => Mathf.Clamp01(currentChargeTimer / Mathf.Max(timeToCharge, 0.0001f));

    public event Action ChargeStarted;
    public event Action Charged;
    public event Action ChargeConsumed;
    public event Action<ChargedHitData> ChargedHit;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        ResetState();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
        grabInteractable.activated.AddListener(OnActivatePressed);
        grabInteractable.deactivated.AddListener(OnActivateReleased);

        ResetState();
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
        grabInteractable.activated.RemoveListener(OnActivatePressed);
        grabInteractable.deactivated.RemoveListener(OnActivateReleased);
    }

    private void ResetState()
    {
        IsCharged = false;
        isHoldingTrigger = false;
        chargeStarted = false;
        currentChargeTimer = 0f;

        if (chargedEffectObject != null)
            chargedEffectObject.SetActive(false);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRBaseInputInteractor interactor)
            currentInteractor = interactor;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHoldingTrigger = false;
        chargeStarted = false;
        currentInteractor = null;

        if (args.interactorObject is XRSocketInteractor)
            ExpendCharge();
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
        chargeStarted = false;
        currentChargeTimer = 0f;
    }

    private void Update()
    {
        if (!isHoldingTrigger || IsCharged) return;

        currentChargeTimer += Time.deltaTime;

        if (currentInteractor != null && Time.frameCount % 5 == 0)
            currentInteractor.SendHapticImpulse(ChargeProgress * 0.3f, 0.05f);

        if (currentChargeTimer >= timeToCharge)
            FullyCharge();
    }

    private void FullyCharge()
    {
        IsCharged = true;
        isHoldingTrigger = false;
        chargeStarted = false;
        currentChargeTimer = timeToCharge;

        if (chargedEffectObject != null)
            chargedEffectObject.SetActive(true);

        if (currentInteractor != null)
            currentInteractor.SendHapticImpulse(1f, 0.2f);

        onCharged?.Invoke();
        Charged?.Invoke();
    }

    public void ExpendCharge()
    {
        if (!IsCharged) return;

        IsCharged = false;
        isHoldingTrigger = false;
        chargeStarted = false;
        currentChargeTimer = 0f;

        if (chargedEffectObject != null)
            chargedEffectObject.SetActive(false);

        onChargeConsumed?.Invoke();
        ChargeConsumed?.Invoke();
    }

    public void NotifyChargedHit(Health h, Collider c, Vector3 dir, Vector3 pt, float bd, float fd)
    {
        ChargedHitData data = new ChargedHitData(h, c, dir, pt, bd, fd);
        onChargedHit?.Invoke(data);
        ChargedHit?.Invoke(data);
    }
}