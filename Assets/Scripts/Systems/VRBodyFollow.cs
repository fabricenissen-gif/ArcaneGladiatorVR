using UnityEngine;

public class VRBodyFollow : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Die Main Camera (dein VR Headset)")]
    [SerializeField] private Transform headCamera;

    [Header("Settings")]
    [Tooltip("Wie weit unter den Augen soll der Schulter/Rücken-Anker sein?")]
    [SerializeField] private float heightOffset = -0.3f; 
    [Tooltip("Wie weich soll der Körper folgen? (Macht es natürlicher)")]
    [SerializeField] private float smoothSpeed = 15f;

    private void LateUpdate()
    {
        if (headCamera == null) return;

        // 1. Position: Kamera-Position + Offset nach unten
        Vector3 targetPosition = headCamera.position + Vector3.up * heightOffset;

        // 2. Rotation: Nur die Y-Drehung (Kopfdrehung links/rechts) übernehmen.
        // X und Z auf 0 setzen, damit der Rücken immer aufrecht bleibt!
        Vector3 eulerAngles = headCamera.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(0f, eulerAngles.y, 0f);

        // 3. Weich anwenden
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}