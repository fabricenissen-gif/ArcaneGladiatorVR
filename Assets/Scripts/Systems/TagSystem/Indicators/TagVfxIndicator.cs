using System.Collections.Generic;
using UnityEngine;

public class TagVfxIndicator : MonoBehaviour
{
    [System.Serializable]
    public class TagVfxEntry
    {
        public TagType        tagType;
        public ParticleSystem vfxPrefab;
        public Vector3        spawnOffset = new Vector3(0f, 0.5f, 0f);
    }

    [SerializeField] private TagVfxEntry[] entries;

    private TagHandler tagHandler;
    private readonly Dictionary<TagType, ParticleSystem> activeVfx = new Dictionary<TagType, ParticleSystem>();
    private readonly Dictionary<TagType, TagVfxEntry>    entryMap  = new Dictionary<TagType, TagVfxEntry>();

    private void Awake()
    {
        tagHandler = GetComponent<TagHandler>();
        if (tagHandler == null)
        {
            Debug.LogError($"[TagVfxIndicator] Kein TagHandler auf {gameObject.name}");
            return;
        }

        foreach (TagVfxEntry entry in entries)
        {
            if (entry.vfxPrefab == null)
            {
                Debug.LogWarning($"[TagVfxIndicator] Kein VFX-Prefab für {entry.tagType} auf {gameObject.name} gesetzt.");
                continue;
            }
            entryMap[entry.tagType] = entry;
        }
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
        if (!entryMap.ContainsKey(type)) return;
        ShowVfx(type);
    }

    private void OnTagRemoved(TagType type) => HideVfx(type);
    private void OnTagExpired(TagType type) => HideVfx(type);

    private void ShowVfx(TagType type)
    {
        if (activeVfx.ContainsKey(type) && activeVfx[type] != null) return;

        TagVfxEntry entry = entryMap[type];
        ParticleSystem ps = Instantiate(entry.vfxPrefab,
                                        transform.position + entry.spawnOffset,
                                        Quaternion.identity, transform);
        ps.Play();
        activeVfx[type] = ps;
    }

    private void HideVfx(TagType type)
    {
        if (!activeVfx.TryGetValue(type, out ParticleSystem ps) || ps == null)
        {
            activeVfx.Remove(type);
            return;
        }
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(ps.gameObject, 1.5f);
        activeVfx.Remove(type);
    }
}