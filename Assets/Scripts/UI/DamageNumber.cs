using System.Collections;
using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshPro tmp;
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private float riseSpeed = 1.5f;
    [SerializeField] private float fadeStartAt = 0.6f;

    private Transform camTransform;
    private float elapsed;
    private Color startColor;

    private void Awake()
    {
        if (tmp == null)
            tmp = GetComponent<TextMeshPro>();

        if (tmp == null)
            Debug.LogError("[DamageNumber] Kein TextMeshPro gefunden auf " + gameObject.name);
    }

    public void Initialize(string text, Color color, bool isBig, Transform cameraTransform)
    {
        if (tmp == null)
        {
            Debug.LogError("[DamageNumber] tmp ist null — Prefab korrekt aufgebaut?");
            Destroy(gameObject);
            return;
        }

        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = isBig ? 5f : 3.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        startColor = color;
        camTransform = cameraTransform;
        elapsed = 0f;

        // Nur minimaler seitlicher Offset
        transform.position += new Vector3(Random.Range(-0.1f, 0.1f), 0f, 0f);

        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / lifetime;

            transform.position += Vector3.up * riseSpeed * Time.deltaTime;

            if (camTransform != null)
            {
                Vector3 dir = transform.position - camTransform.position;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }

            if (progress >= fadeStartAt)
            {
                float fadeFraction = (progress - fadeStartAt) / (1f - fadeStartAt);
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, fadeFraction);
                tmp.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}