using System.Collections;
using UnityEngine;

public class HitReaction : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float reactionDuration = 0.28f;
    [SerializeField] private float maxTiltAngle = 16f;
    [SerializeField] private float scaleImpulse = 0.12f;

    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Coroutine reactionRoutine;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        originalScale = visualRoot.localScale;
        originalRotation = visualRoot.localRotation;
    }

    public void PlayReaction(Vector3 hitDirection)
    {
        if (reactionRoutine != null)
            StopCoroutine(reactionRoutine);

        reactionRoutine = StartCoroutine(ReactionRoutine(hitDirection));
    }

    private IEnumerator ReactionRoutine(Vector3 hitDirection)
    {
        // Schritt 1: kurz in Trefferrichtung kippen + kurz gestreckt
        Vector3 tiltAxis = Vector3.Cross(Vector3.up, hitDirection).normalized;
        Quaternion tiltRotation = Quaternion.AngleAxis(maxTiltAngle, tiltAxis);

        Vector3 squishScale = new Vector3(
            originalScale.x * (1f - scaleImpulse * 0.5f),
            originalScale.y * (1f + scaleImpulse),
            originalScale.z * (1f - scaleImpulse * 0.5f)
        );

        visualRoot.localRotation = originalRotation * tiltRotation;
        visualRoot.localScale = squishScale;

        float halfDuration = reactionDuration * 0.5f;
        float elapsed = 0f;

        // Schritt 2: sanft zurück
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            visualRoot.localRotation = Quaternion.Lerp(
                originalRotation * tiltRotation,
                originalRotation,
                t
            );
            visualRoot.localScale = Vector3.Lerp(squishScale, originalScale, t);
            yield return null;
        }

        visualRoot.localRotation = originalRotation;
        visualRoot.localScale = originalScale;
        reactionRoutine = null;
    }
}