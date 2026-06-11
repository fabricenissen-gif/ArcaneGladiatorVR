using System.Collections;
using UnityEngine;

public class HitWobble : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float wobbleDistance = 0.12f;
    [SerializeField] private float wobbleDuration = 0.12f;

    private Vector3 restLocalPosition;
    private Coroutine wobbleRoutine;

    private void Awake()
    {
        if (targetTransform == null)
        {
            Debug.LogWarning($"HitWobble on {gameObject.name} has no targetTransform assigned. Using own transform as fallback.");
            targetTransform = transform;
        }

        restLocalPosition = targetTransform.localPosition;
    }

    public void PlayWobble(Vector3 hitDirection)
    {
        if (targetTransform == null)
            return;

        if (wobbleRoutine != null)
        {
            StopCoroutine(wobbleRoutine);
            targetTransform.localPosition = restLocalPosition;
        }

        wobbleRoutine = StartCoroutine(WobbleRoutine(hitDirection));
    }

    private IEnumerator WobbleRoutine(Vector3 hitDirection)
    {
        Vector3 localHitDir = targetTransform.parent != null
            ? targetTransform.parent.InverseTransformDirection(hitDirection.normalized)
            : hitDirection.normalized;

        localHitDir.y = 0f;
        if (localHitDir.sqrMagnitude < 0.0001f)
            localHitDir = Vector3.forward;

        localHitDir.Normalize();

        Vector3 hitOffset = restLocalPosition + localHitDir * wobbleDistance;

        float halfDuration = wobbleDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            targetTransform.localPosition = Vector3.Lerp(restLocalPosition, hitOffset, t);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            targetTransform.localPosition = Vector3.Lerp(hitOffset, restLocalPosition, t);
            yield return null;
        }

        targetTransform.localPosition = restLocalPosition;
        wobbleRoutine = null;
    }

    public void ResetToRestPose()
    {
        if (wobbleRoutine != null)
            StopCoroutine(wobbleRoutine);

        if (targetTransform != null)
            targetTransform.localPosition = restLocalPosition;

        wobbleRoutine = null;
    }
}