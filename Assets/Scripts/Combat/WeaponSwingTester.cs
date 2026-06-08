using UnityEngine;

public class WeaponSwingTester : MonoBehaviour
{
    [SerializeField] private WeaponHitbox weaponHitbox;
    [SerializeField] private bool triggerSwing;

    private void Update()
    {
        if (triggerSwing)
        {
            triggerSwing = false;
            weaponHitbox.BeginSwing();
            Debug.Log("Manual swing trigger fired.");
        }
    }
}