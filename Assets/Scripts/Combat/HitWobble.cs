using System.Collections;
using UnityEngine;

public class HitWobble : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float wobbleDistance = 0.12f;
    [SerializeField] private float wobbleDuration = 0.12f;

    private Vector3 originalLocalPosition;
    private Coroutine wobbleRoutine;

    private void Awake()
    {
        if (targetTransform == null)
            targetTransform = transform;

        originalLocalPosition = targetTransform.localPosition;
    }

    public void PlayWobble(Vector3 hitDirection)
    {
        if (wobbleRoutine != null)
            StopCoroutine(wobbleRoutine);

        wobbleRoutine = StartCoroutine(WobbleRoutine(hitDirection));
    }

    private IEnumerator WobbleRoutine(Vector3 hitDirection)
    {
        Vector3 localHitDir = targetTransform.parent != null
            ? targetTransform.parent.InverseTransformDirection(hitDirection.normalized)
            : hitDirection.normalized;

        Vector3 offset = localHitDir * wobbleDistance;

        targetTransform.localPosition = originalLocalPosition + offset;
        yield return new WaitForSeconds(wobbleDuration * 0.5f);

        targetTransform.localPosition = originalLocalPosition;
        yield return new WaitForSeconds(wobbleDuration * 0.5f);

        wobbleRoutine = null;
    }
}