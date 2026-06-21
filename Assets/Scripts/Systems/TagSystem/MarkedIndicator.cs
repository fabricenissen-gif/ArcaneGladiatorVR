using UnityEngine;

/// <summary>
/// Visualisiert MARKED:
///   Option B — Health.cs appliziert +markedDamageAmp% passiv
///   Option C — Outline-Glow (Quick Outline by Chris Nolet)
///              seenThroughWalls = true  → Outline.Mode.OutlineAll
///              seenThroughWalls = false → Outline.Mode.OutlineVisible
/// </summary>
public class MarkedIndicator : MonoBehaviour
{
    [Header("Partikel VFX")]
    [SerializeField] private ParticleSystem markedVfxPrefab;
    [SerializeField] private Vector3        spawnOffset = new Vector3(0f, 2f, 0f);

    [Header("Outline (Quick Outline)")]
    [SerializeField] private bool  outlineEnabled    = true;
    [SerializeField] private Color outlineColor      = new Color(1f, 0.6f, 0f, 1f);
    [SerializeField] private float outlineWidth      = 5f;
    [Tooltip("true = durch Wände sichtbar (OutlineAll), false = nur wenn sichtbar (OutlineVisible)")]
    [SerializeField] private bool  seenThroughWalls  = true;

    private TagHandler     tagHandler;
    private ParticleSystem activeVfx;
    private Outline        outline;

    private void Awake()
    {
        tagHandler = GetComponent<TagHandler>();
        if (tagHandler == null)
            Debug.LogError($"[MarkedIndicator] Kein TagHandler auf {gameObject.name}");

        SetupOutline();
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
        if (type == TagType.MARKED) ShowEffect();
    }

    private void OnTagRemoved(TagType type)
    {
        if (type == TagType.MARKED) HideEffect();
    }

    private void OnTagExpired(TagType type)
    {
        if (type == TagType.MARKED) HideEffect();
    }

    private void ShowEffect()
    {
        ShowVfx();
        if (outline != null) outline.enabled = true;
        Debug.Log($"[MarkedIndicator] {gameObject.name} — MARKED aktiv");
    }

    private void HideEffect()
    {
        HideVfx();
        if (outline != null) outline.enabled = false;
        Debug.Log($"[MarkedIndicator] {gameObject.name} — MARKED entfernt");
    }

    private void ShowVfx()
    {
        if (markedVfxPrefab == null || activeVfx != null) return;
        activeVfx = Instantiate(markedVfxPrefab, transform.position + spawnOffset, Quaternion.identity);
        activeVfx.transform.SetParent(transform);
        activeVfx.Play();
    }

    private void HideVfx()
    {
        if (activeVfx == null) return;
        activeVfx.Stop();
        Destroy(activeVfx.gameObject, 1f);
        activeVfx = null;
    }

    private void SetupOutline()
    {
        if (!outlineEnabled) return;

        outline = GetComponent<Outline>();
        if (outline == null)
            outline = gameObject.AddComponent<Outline>();

        outline.OutlineMode  = seenThroughWalls
            ? Outline.Mode.OutlineAll
            : Outline.Mode.OutlineVisible;

        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = outlineWidth;
        outline.enabled      = false; // startet aus — nur bei aktivem MARKED ein
    }
}