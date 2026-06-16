using UnityEngine;

public class BodySocketTracker : MonoBehaviour
{
    [Tooltip("Die VR Kamera (Main Camera)")]
    public Transform headTransform;
    
    [Tooltip("Offset relativ zum Kopf (X = Rechts/Links, Y = Hoch/Runter, Z = Vor/Zurück)")]
    public Vector3 positionOffset = new Vector3(0.2f, -0.5f, 0f);
    
    [Tooltip("Soll sich der Gürtel mit der Kopfdrehung mitdrehen?")]
    public bool trackRotation = true;

    private void Start()
    {
        if (headTransform == null && Camera.main != null)
        {
            headTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (headTransform == null) return;

        // Wir ignorieren das Nicken (X) und Neigen (Z) des Kopfes. Der Gürtel bleibt waagerecht!
        Vector3 eulerAngles = headTransform.eulerAngles;
        Quaternion headYaw = Quaternion.Euler(0, eulerAngles.y, 0);
        
        // Position: Kopfposition + berechneter Offset (dreht sich mit der Blickrichtung mit)
        transform.position = headTransform.position + (headYaw * positionOffset);

        if (trackRotation)
        {
            transform.rotation = headYaw;
        }
    }
}