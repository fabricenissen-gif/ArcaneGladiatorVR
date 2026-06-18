using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float damageCooldown = 0.25f;

    [Header("Death")]
    [SerializeField] private float deathDelay = 1.5f;

    [Header("Feedback Visuell")]
    [Tooltip("Ziehe hier das rote Image auf dem World Space Canvas rein")]
    [SerializeField] private Image damageOverlay;
    [SerializeField] private float flashDuration = 0.4f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.6f;

    [Header("Feedback Haptisch")]
    [Tooltip("Ziehe hier den Left Controller rein")]
    [SerializeField] private HapticImpulsePlayer leftHapticPlayer;
    [Tooltip("Ziehe hier den Right Controller rein")]
    [SerializeField] private HapticImpulsePlayer rightHapticPlayer;
    [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.5f;
    [SerializeField] private float hapticDuration = 0.2f;

    [Header("Hurt Sound")]
    [SerializeField] private AudioSource hurtAudioSource;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField, Range(0f, 1f)] private float hurtVolume = 0.8f;

    [Header("Death Sound")]
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;

    private int currentHealth;
    private float nextDamageTime;
    private bool isDead;
    private Coroutine flashCoroutine;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        ForceHideOverlay();
    }

    private void OnEnable()
    {
        ForceHideOverlay();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        nextDamageTime = 0f;

        ForceHideOverlay();

        Debug.Log("[PlayerHealth] Start health: " + currentHealth);
    }

    public void TakeDamage(int damage, Vector3 hitDirection)
    {
        if (isDead) return;
        if (damage <= 0) return;
        if (Time.time < nextDamageTime) return;

        nextDamageTime = Time.time + damageCooldown;
        currentHealth -= damage;

        PlayHurtSound();
        TriggerDamageFeedback();

        Debug.Log("[PlayerHealth] Took damage: " + damage + " | Current health: " + currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (isDead) return;
        if (healAmount <= 0) return;

        currentHealth += healAmount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        Debug.Log("[PlayerHealth] Healed for " + healAmount + ". Current health: " + currentHealth);
    }

    private void TriggerDamageFeedback()
    {
        if (damageOverlay != null)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        if (leftHapticPlayer != null)
            leftHapticPlayer.SendHapticImpulse(hapticAmplitude, hapticDuration);

        if (rightHapticPlayer != null)
            rightHapticPlayer.SendHapticImpulse(hapticAmplitude, hapticDuration);
    }

    private IEnumerator FlashRoutine()
    {
        if (damageOverlay == null)
            yield break;

        Color color = damageOverlay.color;
        color.a = maxAlpha;
        damageOverlay.color = color;

        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / flashDuration);

            color.a = Mathf.Lerp(maxAlpha, 0f, t);
            damageOverlay.color = color;

            yield return null;
        }

        color.a = 0f;
        damageOverlay.color = color;
        flashCoroutine = null;
    }

    private void ForceHideOverlay()
    {
        if (damageOverlay == null)
            return;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        Color color = damageOverlay.color;
        color.a = 0f;
        damageOverlay.color = color;

        damageOverlay.enabled = true;
    }

    private void PlayHurtSound()
    {
        if (hurtAudioSource == null || hurtSound == null)
            return;

        hurtAudioSource.PlayOneShot(hurtSound, hurtVolume);
    }

    private void PlayDeathSound()
    {
        if (deathAudioSource == null || deathSound == null)
            return;

        deathAudioSource.PlayOneShot(deathSound, deathVolume);
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        if (hurtAudioSource != null && hurtAudioSource.isPlaying)
            hurtAudioSource.Stop();

        PlayDeathSound();
        Debug.Log("[PlayerHealth] Player died.");

        StartCoroutine(ReloadSceneRoutine());
    }

    private IEnumerator ReloadSceneRoutine()
    {
        yield return new WaitForSeconds(deathDelay);
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}