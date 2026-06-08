using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f);

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;
        transform.rotation = Quaternion.identity;
    }
}