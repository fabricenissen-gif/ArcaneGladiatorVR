using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LightningEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.15f;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    public void Initialize(Vector3 start, Vector3 end)
    {
        if (line == null)
            line = GetComponent<LineRenderer>();

        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        Destroy(gameObject, lifetime);
    }
}