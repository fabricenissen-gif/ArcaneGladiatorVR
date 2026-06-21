using UnityEngine;

public class FireTagIndicator : MonoBehaviour
{
    [Header("Fire Tag VFX")]
    [Tooltip("Particle System Prefab das am Gegner klebt solange FIRE aktiv ist")]
    [SerializeField] private ParticleSystem fireVfxPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.5f, 0f);

    private TagHandler tagHandler;
    private ParticleSystem activeFireVfx;

    private void Awake()
    {
        tagHandler = GetComponent<TagHandler>();
        if (tagHandler == null)
            Debug.LogError($"[FireTagIndicator] Kein TagHandler auf {gameObject.name}");
    }

    private void OnEnable()
    {
        if (tagHandler == null) return;
        tagHandler.OnTagApplied += OnTagApplied;
        tagHandler.OnTagRemoved += OnTagRemoved;
        tagHandler.OnTagExpired += OnTagExpired;
    }

    private void OnDisable()
    {
        if (tagHandler == null) return;
        tagHandler.OnTagApplied -= OnTagApplied;
        tagHandler.OnTagRemoved -= OnTagRemoved;
        tagHandler.OnTagExpired -= OnTagExpired;
    }

    private void OnTagApplied(TagType type, TagInstance instance)
    {
        if (type == TagType.FIRE) ShowFire();
    }

    private void OnTagRemoved(TagType type)
    {
        if (type == TagType.FIRE) HideFire();
    }

    private void OnTagExpired(TagType type)
    {
        if (type == TagType.FIRE) HideFire();
    }

    private void ShowFire()
    {
        if (activeFireVfx != null) return;
        if (fireVfxPrefab == null)
        {
            Debug.LogWarning($"[FireTagIndicator] Kein fireVfxPrefab auf {gameObject.name} gesetzt.");
            return;
        }
        activeFireVfx = Instantiate(fireVfxPrefab, transform.position + spawnOffset,
                                    Quaternion.identity, transform);
        activeFireVfx.Play();
    }

    private void HideFire()
    {
        if (activeFireVfx == null) return;
        activeFireVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(activeFireVfx.gameObject, 1.5f);
        activeFireVfx = null;
    }
}