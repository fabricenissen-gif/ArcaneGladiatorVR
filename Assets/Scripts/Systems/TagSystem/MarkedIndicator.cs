using UnityEngine;

public class MarkedIndicator : MonoBehaviour
{
    [Tooltip("Partikel-Prefab das über dem Gegner erscheint")]
    [SerializeField] private ParticleSystem markedVfxPrefab;
    [Tooltip("Offset über dem Gegner")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 2f, 0f);

    private TagHandler tagHandler;
    private ParticleSystem activeVfx;

    private void Awake()
    {
        tagHandler = GetComponent<TagHandler>();

        if (tagHandler == null)
            Debug.LogError("[MarkedIndicator] Kein TagHandler auf " + gameObject.name);
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
        if (type == TagType.MARKED)
            ShowEffect();
    }

    private void OnTagRemoved(TagType type)
    {
        if (type == TagType.MARKED)
            HideEffect();
    }

    private void OnTagExpired(TagType type)
    {
        if (type == TagType.MARKED)
            HideEffect();
    }

    private void ShowEffect()
    {
        if (markedVfxPrefab == null)
        {
            Debug.LogWarning("[MarkedIndicator] Kein VFX Prefab zugewiesen auf " + gameObject.name);
            return;
        }

        if (activeVfx != null) return;

        activeVfx = Instantiate(markedVfxPrefab, transform.position + spawnOffset, Quaternion.identity);
        activeVfx.transform.SetParent(transform);
        activeVfx.Play();

        Debug.Log("[MarkedIndicator] " + gameObject.name + " MARKED Effekt aktiv.");
    }

    private void HideEffect()
    {
        if (activeVfx == null) return;

        activeVfx.Stop();
        Destroy(activeVfx.gameObject, 1f);
        activeVfx = null;

        Debug.Log("[MarkedIndicator] " + gameObject.name + " MARKED Effekt entfernt.");
    }
}