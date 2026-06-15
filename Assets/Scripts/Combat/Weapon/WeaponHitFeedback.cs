using UnityEngine;

public class WeaponHitFeedback : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private float vfxLifetime = 1.0f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] [Range(0f, 1f)] private float hitVolume = 0.9f;

    public void PlayHitFeedback(Vector3 hitPosition, Vector3 hitDirection)
    {
        PlayVfx(hitPosition, hitDirection);
        PlaySound();
    }

    private void PlayVfx(Vector3 hitPosition, Vector3 hitDirection)
    {
        if (hitVfxPrefab == null)
            return;

        Quaternion rotation = hitDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(hitDirection)
            : Quaternion.identity;

        GameObject vfxInstance = Instantiate(hitVfxPrefab, hitPosition, rotation);
        Destroy(vfxInstance, vfxLifetime);
    }

    private void PlaySound()
    {
        if (audioSource == null || hitSounds == null || hitSounds.Length == 0)
            return;

        int index = Random.Range(0, hitSounds.Length);
        AudioClip clip = hitSounds[index];

        if (clip != null)
            audioSource.PlayOneShot(clip, hitVolume);
    }
}