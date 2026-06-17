using UnityEngine;

public class ArcaneBoltAutoRotate : MonoBehaviour
{
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 360f, 720f);

    private void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
    }
}