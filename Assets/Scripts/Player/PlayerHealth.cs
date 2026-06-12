using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float damageCooldown = 0.25f;

    [Header("Death")]
    [SerializeField] private float deathDelay = 1.5f;

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

    private void Start()
    {
        currentHealth = maxHealth;
        isDead = false;

        Debug.Log("[PlayerHealth] Start health: " + currentHealth);
    }

    public void TakeDamage(int damage, Vector3 hitDirection)
    {
        if (isDead)
        {
            Debug.Log("[PlayerHealth] Damage ignored because player is already dead.");
            return;
        }

        if (Time.time < nextDamageTime)
        {
            Debug.Log("[PlayerHealth] Damage ignored because of cooldown.");
            return;
        }

        nextDamageTime = Time.time + damageCooldown;
        currentHealth -= damage;

        PlayHurtSound();

        Debug.Log("[PlayerHealth] Current health: " + currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void PlayHurtSound()
    {
        if (hurtAudioSource == null)
        {
            Debug.LogWarning("[PlayerHealth] No hurtAudioSource assigned.");
            return;
        }

        if (hurtSound == null)
        {
            Debug.LogWarning("[PlayerHealth] No hurtSound assigned.");
            return;
        }

        hurtAudioSource.PlayOneShot(hurtSound, hurtVolume);
    }

    private void PlayDeathSound()
    {
        if (deathAudioSource == null)
        {
            Debug.LogWarning("[PlayerHealth] No deathAudioSource assigned.");
            return;
        }

        if (deathSound == null)
        {
            Debug.LogWarning("[PlayerHealth] No deathSound assigned.");
            return;
        }

        deathAudioSource.PlayOneShot(deathSound, deathVolume);
    }

    private void Die()
    {
        if (isDead)
            return;

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