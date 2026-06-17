using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement; // Korrekter Namespace in XR 3.0

public class ArmSwingSprint : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ziehe hier deinen linken Controller rein")]
    [SerializeField] private Transform leftController;
    [Tooltip("Ziehe hier deinen rechten Controller rein")]
    [SerializeField] private Transform rightController;
    [Tooltip("Ziehe hier dein Headset (Main Camera) rein")]
    [SerializeField] private Transform headset;
    
    [Tooltip("Dein normaler Continuous Move Provider")]
    // In XR 3.0 nutzen wir direkt den ContinuousMoveProvider
    [SerializeField] private ContinuousMoveProvider moveProvider;

    [Header("Sprint Settings")]
    [Tooltip("Normale Gehgeschwindigkeit (ohne Armschwung)")]
    [SerializeField] private float baseSpeed = 2f;
    [Tooltip("Maximale Sprintgeschwindigkeit (bei starkem Armschwung)")]
    [SerializeField] private float maxSprintSpeed = 6f;
    
    [Tooltip("Wie viel Controller-Bewegung braucht es, um voll zu sprinten? (niedriger = leichter zu sprinten)")]
    [SerializeField] private float swingSensitivity = 2.0f;
    [Tooltip("Wie schnell fällt der Sprint wieder ab, wenn du aufhörst zu schwingen?")]
    [SerializeField] private float sprintDecay = 3f;

    // Speichern der vorherigen Positionen RELATIV zum Headset
    private Vector3 prevLeftLocalPos;
    private Vector3 prevRightLocalPos;
    
    // Die aktuell aufgebaute Sprint-Energie
    private float currentSwingEnergy = 0f;

    private void Start()
    {
        if (headset == null && Camera.main != null) headset = Camera.main.transform;
        
        if (moveProvider == null) moveProvider = GetComponent<ContinuousMoveProvider>();
        
        if (leftController != null && rightController != null && headset != null)
        {
            prevLeftLocalPos = headset.InverseTransformPoint(leftController.position);
            prevRightLocalPos = headset.InverseTransformPoint(rightController.position);
        }
    }

    private void Update()
    {
        if (leftController == null || rightController == null || headset == null || moveProvider == null) return;

        // 1. Lokale Positionen der Controller relativ zum Kopf berechnen
        Vector3 currentLeftLocalPos = headset.InverseTransformPoint(leftController.position);
        Vector3 currentRightLocalPos = headset.InverseTransformPoint(rightController.position);

        // 2. Wie weit haben sich die Hände diesen Frame relativ zum Körper bewegt?
        float leftDistance = Vector3.Distance(currentLeftLocalPos, prevLeftLocalPos);
        float rightDistance = Vector3.Distance(currentRightLocalPos, prevRightLocalPos);

        // 3. Bewegungsgeschwindigkeit der Hände in Metern pro Sekunde
        float handVelocity = (leftDistance + rightDistance) / Time.deltaTime;

        // 4. Energie aufbauen, wenn wir die Arme schwingen
        if (handVelocity > 0.5f)
        {
            currentSwingEnergy += handVelocity * swingSensitivity * Time.deltaTime;
        }

        // 5. Energie langsam wieder abbauen
        currentSwingEnergy = Mathf.Lerp(currentSwingEnergy, 0f, Time.deltaTime * sprintDecay);
        currentSwingEnergy = Mathf.Clamp01(currentSwingEnergy);

        // 6. Die MoveSpeed im Provider dynamisch anpassen
        moveProvider.moveSpeed = Mathf.Lerp(baseSpeed, maxSprintSpeed, currentSwingEnergy);

        // 7. Für den nächsten Frame speichern
        prevLeftLocalPos = currentLeftLocalPos;
        prevRightLocalPos = currentRightLocalPos;
    }
}