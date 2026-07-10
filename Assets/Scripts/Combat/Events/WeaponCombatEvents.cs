using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponCombatEvents : MonoBehaviour
{
    public enum TriggerType
    {
        OnSwing,
        OnHit,
        OnThrowRelease,
        OnThrowHit
    }

    public readonly struct EventData
    {
        public TriggerType Trigger { get; }
        public GameObject Source { get; }
        public Health TargetHealth { get; }
        public Collider TargetCollider { get; }
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
        public float Damage { get; }
        public float Speed { get; }
        public bool WasCharged { get; }
        public int SwingId { get; }
        public int Frame { get; }

        public EventData(
            TriggerType trigger,
            GameObject source,
            Health targetHealth,
            Collider targetCollider,
            Vector3 point,
            Vector3 direction,
            float damage,
            float speed,
            bool wasCharged,
            int swingId)
        {
            Trigger = trigger;
            Source = source;
            TargetHealth = targetHealth;
            TargetCollider = targetCollider;
            Point = point;
            Direction = direction.sqrMagnitude > 0.000001f
                ? direction.normalized
                : Vector3.forward;
            Damage = Mathf.Max(0f, damage);
            Speed = Mathf.Max(0f, speed);
            WasCharged = wasCharged;
            SwingId = Mathf.Max(0, swingId);
            Frame = Time.frameCount;
        }
    }

    [Header("Debug")]
    [Tooltip("Nur während der Integration aktiv lassen. Danach deaktivieren, damit die Console sauber bleibt.")]
    [SerializeField] private bool logEvents = true;

    public event Action<EventData> Swing;
    public event Action<EventData> Hit;
    public event Action<EventData> ThrowReleased;
    public event Action<EventData> ThrowHit;

    public void RaiseSwing(
        Vector3 direction,
        float speed,
        bool wasCharged,
        int swingId)
    {
        Publish(new EventData(
            TriggerType.OnSwing,
            gameObject,
            null,
            null,
            transform.position,
            direction,
            0f,
            speed,
            wasCharged,
            swingId));
    }

    public void RaiseHit(
        Health targetHealth,
        Collider targetCollider,
        Vector3 point,
        Vector3 direction,
        float damage,
        float speed,
        bool wasCharged,
        int swingId)
    {
        if (targetHealth == null)
            return;

        Publish(new EventData(
            TriggerType.OnHit,
            gameObject,
            targetHealth,
            targetCollider,
            point,
            direction,
            damage,
            speed,
            wasCharged,
            swingId));
    }

    public void RaiseThrowReleased(
        Vector3 direction,
        float speed,
        bool wasCharged)
    {
        if (speed <= 0f)
            return;

        Publish(new EventData(
            TriggerType.OnThrowRelease,
            gameObject,
            null,
            null,
            transform.position,
            direction,
            0f,
            speed,
            wasCharged,
            0));
    }

    public void RaiseThrowHit(
        Health targetHealth,
        Collider targetCollider,
        Vector3 point,
        Vector3 direction,
        float damage,
        float speed,
        bool wasCharged)
    {
        if (targetHealth == null)
            return;

        Publish(new EventData(
            TriggerType.OnThrowHit,
            gameObject,
            targetHealth,
            targetCollider,
            point,
            direction,
            damage,
            speed,
            wasCharged,
            0));
    }

    private void Publish(EventData eventData)
    {
        if (logEvents)
        {
            string targetName = eventData.TargetHealth != null
                ? eventData.TargetHealth.name
                : "None";

            Debug.Log(
                $"[WeaponCombatEvents] {eventData.Trigger} | " +
                $"Source:{name} | Target:{targetName} | " +
                $"Damage:{eventData.Damage:F1} | " +
                $"Speed:{eventData.Speed:F2} | " +
                $"Charged:{eventData.WasCharged} | " +
                $"Swing:{eventData.SwingId} | Frame:{eventData.Frame}");
        }

        switch (eventData.Trigger)
        {
            case TriggerType.OnSwing:
                Swing?.Invoke(eventData);
                break;

            case TriggerType.OnHit:
                Hit?.Invoke(eventData);
                break;

            case TriggerType.OnThrowRelease:
                ThrowReleased?.Invoke(eventData);
                break;

            case TriggerType.OnThrowHit:
                ThrowHit?.Invoke(eventData);
                break;
        }
    }
}