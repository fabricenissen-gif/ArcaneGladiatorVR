using UnityEngine;

public class WeaponSwingDetector : MonoBehaviour
{
    [SerializeField] private Transform trackedWeaponTransform;

    [SerializeField] private float swingSpeedThreshold = 1.0f;
    [SerializeField] private float resetSpeedThreshold = 0.25f;
    [SerializeField] private float minTimeBetweenSwings = 0.30f;
    [SerializeField] private float minSwingDuration = 0.14f;

    private Vector3 lastPosition;
    private Vector3 currentVelocity;
    private bool swingActive;
    private float lastSwingStartTime = -999f;
    private float lastSwingTime = -999f;
    private int swingId;

    public bool IsSwinging => swingActive;
    public float CurrentSwingSpeed => currentVelocity.magnitude;
    public int CurrentSwingId => swingId;
    public Vector3 CurrentSwingDirection => currentVelocity.sqrMagnitude > 0.000001f
        ? currentVelocity.normalized
        : Vector3.zero;

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

        currentVelocity = frameDelta / Mathf.Max(Time.deltaTime, 0.0001f);
        float speed = currentVelocity.magnitude;

        if (!swingActive &&
            speed >= swingSpeedThreshold &&
            Time.time >= lastSwingTime + minTimeBetweenSwings)
        {
            swingActive = true;
            lastSwingStartTime = Time.time;
            lastSwingTime = Time.time;
            swingId++;

            Debug.Log($"Swing started. ID: {swingId}, Speed: {speed:F2}");
        }
        else if (swingActive &&
                 Time.time >= lastSwingStartTime + minSwingDuration &&
                 speed <= resetSpeedThreshold)
        {
            swingActive = false;
            Debug.Log("Swing reset.");
        }

        lastPosition = currentPosition;
    }
}