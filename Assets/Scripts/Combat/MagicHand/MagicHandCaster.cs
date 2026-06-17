using UnityEngine;
using UnityEngine.InputSystem;

public class MagicHandCaster : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference castAction;

    [Header("Casting")]
    [SerializeField] private Transform castPoint;
    [SerializeField] private ArcaneBoltProjectile projectilePrefab;
    [SerializeField] private float castCooldown = 0.25f;

    [Header("Projectile Stats")]
    [SerializeField] private float projectileDamage = 12f;
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float projectileLifetime = 4f;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem castMuzzleVfx;
    [SerializeField] private AudioSource castAudioSource;
    [SerializeField] private AudioClip castClip;

    private float lastCastTime = -999f;

    private void OnEnable()
    {
        if (castAction != null && castAction.action != null)
        {
            castAction.action.Enable();
            castAction.action.performed += OnCastPerformed;
        }
    }

    private void OnDisable()
    {
        if (castAction != null && castAction.action != null)
        {
            castAction.action.performed -= OnCastPerformed;
            castAction.action.Disable();
        }
    }

    private void OnCastPerformed(InputAction.CallbackContext context)
    {
        TryCast();
    }

    public void TryCast()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("MagicHandCaster: No projectile prefab assigned.");
            return;
        }

        if (castPoint == null)
        {
            Debug.LogWarning("MagicHandCaster: No cast point assigned.");
            return;
        }

        if (Time.time < lastCastTime + castCooldown)
            return;

        lastCastTime = Time.time;

        ArcaneBoltProjectile projectile = Instantiate(
            projectilePrefab,
            castPoint.position,
            castPoint.rotation
        );

        projectile.Initialize(projectileDamage, projectileSpeed, projectileLifetime);

        if (castMuzzleVfx != null)
            castMuzzleVfx.Play();

        if (castAudioSource != null && castClip != null)
            castAudioSource.PlayOneShot(castClip);
    }
}