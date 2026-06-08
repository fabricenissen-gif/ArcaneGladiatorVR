using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.06f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Color originalBaseColor;
    private Coroutine flashRoutine;
    private Material runtimeMaterial;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogWarning("HitFlash: No renderer found.");
            return;
        }

        runtimeMaterial = targetRenderer.material;

        if (runtimeMaterial.HasProperty(BaseColorId))
        {
            originalBaseColor = runtimeMaterial.GetColor(BaseColorId);
            Debug.Log($"HitFlash using _BaseColor on: {targetRenderer.name}");
        }
        else
        {
            Debug.LogWarning($"HitFlash: Material on {targetRenderer.name} has no _BaseColor property.");
        }
    }

    public void PlayFlash()
    {
        if (runtimeMaterial == null)
            return;

        if (!runtimeMaterial.HasProperty(BaseColorId))
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        runtimeMaterial.SetColor(BaseColorId, flashColor);
        yield return new WaitForSeconds(flashDuration);
        runtimeMaterial.SetColor(BaseColorId, originalBaseColor);
        flashRoutine = null;
    }
}