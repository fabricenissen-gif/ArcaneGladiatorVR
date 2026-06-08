using UnityEngine;

public class WeaponSwingDetector : MonoBehaviour
{
    [SerializeField] private WeaponHitbox weaponHitbox;
    [SerializeField] private Transform trackedWeaponTransform;

    [SerializeField] private float swingSpeedThreshold = 1.6f;
    [SerializeField] private float resetSpeedThreshold = 0.3f;
    [SerializeField] private float minTimeBetweenSwings = 0.20f;

    [SerializeField] private Vector3 localAttackDirection = Vector3.right;
    [SerializeField] private float directionDotThreshold = 0.15f;

    private Vector3 lastPosition;
    private bool swingActive;
    private float lastSwingTime = -999f;

    private void Start()
    {
        if (trackedWeaponTransform == null)
            trackedWeaponTransform = transform;

        lastPosition = trackedWeaponTransform.position;
    }

    private void Update()
    {
        Vector3 currentPosition = trackedWeaponTransform.position;
        Vector3 frameDelta = currentPosition - lastPosition;
        float speed = frameDelta.magnitude / Time.deltaTime;

        Vector3 movementDirection = frameDelta.sqrMagnitude > 0.000001f
            ? frameDelta.normalized
            : Vector3.zero;

        Vector3 desiredAttackDirection = trackedWeaponTransform.TransformDirection(localAttackDirection.normalized);
        float directionDot = Vector3.Dot(movementDirection, desiredAttackDirection);

        lastPosition = currentPosition;

        if (!swingActive &&
            speed >= swingSpeedThreshold &&
            directionDot >= directionDotThreshold &&
            Time.time >= lastSwingTime + minTimeBetweenSwings)
        {
            swingActive = true;
            lastSwingTime = Time.time;
            weaponHitbox.BeginSwing();
            Debug.Log($"Swing started. Speed: {speed:F2}, Dot: {directionDot:F2}");
        }
        else if (swingActive && speed <= resetSpeedThreshold)
        {
            swingActive = false;
            Debug.Log("Swing reset.");
        }
    }
}