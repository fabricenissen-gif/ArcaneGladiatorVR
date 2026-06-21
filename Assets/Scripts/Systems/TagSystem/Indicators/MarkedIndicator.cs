using UnityEngine;

/// <summary>
/// Visualisiert MARKED und EXPOSED via Outline-Farbe:
///   MARKED   → orange Outline (+ VFX), durch Wände sichtbar
///   EXPOSED  → rote/pulsierende Outline
///   Beide weg → Outline aus
///
/// Priorität: EXPOSED überschreibt MARKED-Farbe solange aktiv.
/// </summary>
public class MarkedIndicator : MonoBehaviour
{
    [Header("Partikel VFX")]
    [SerializeField] private ParticleSystem markedVfxPrefab;
    [SerializeField] private Vector3        spawnOffset = new Vector3(0f, 2f, 0f);

    [Header("Outline — MARKED")]
    [SerializeField] private bool  outlineEnabled   = true;
    [SerializeField] private Color markedColor      = new Color(1f, 0.6f, 0f, 1f);
    [SerializeField] private float markedWidth      = 5f;
    [Tooltip("true = durch Wände sichtbar (OutlineAll), false = nur wenn sichtbar (OutlineVisible)")]
    [SerializeField] private bool  seenThroughWalls = true;

    [Header("Outline — EXPOSED")]
    [SerializeField] private Color exposedColor     = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float exposedWidth     = 8f;
    [SerializeField] private bool  exposedPulse     = true;
    [SerializeField] private float pulseSpeed       = 3f;
    [SerializeField] private float pulseMinWidth    = 3f;

    private TagHandler        tagHandler;
    private TagReactionSystem reactionSystem;
    private ParticleSystem    activeVfx;
    private Outline           outline;

    private bool isMarked  = false;
    private bool isExposed = false;

    private void Awake()
    {
        tagHandler     = GetComponent<TagHandler>();
        reactionSystem = GetComponent<TagReactionSystem>();

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

    private void Update()
    {
        if (reactionSystem == null) return;

        bool exposedNow = reactionSystem.IsExposed;

        if (exposedNow != isExposed)
        {
            isExposed = exposedNow;
            RefreshOutline();
        }

        if (isExposed && exposedPulse && outline != null && outline.enabled)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            outline.OutlineWidth = Mathf.Lerp(pulseMinWidth, exposedWidth, t);
        }
    }

    private void OnTagApplied(TagType type, TagInstance instance)
    {
        if (type != TagType.MARKED) return;
        isMarked = true;
        ShowVfx();
        RefreshOutline();
    }

    private void OnTagRemoved(TagType type)
    {
        if (type != TagType.MARKED) return;
        isMarked = false;
        HideVfx();
        RefreshOutline();
    }

    private void OnTagExpired(TagType type)
    {
        if (type != TagType.MARKED) return;
        isMarked = false;
        HideVfx();
        RefreshOutline();
    }

    private void RefreshOutline()
    {
        if (outline == null) return;

        if (isExposed)
        {
            outline.OutlineMode  = Outline.Mode.OutlineAll;
            outline.OutlineColor = exposedColor;
            outline.OutlineWidth = exposedWidth;
            outline.enabled      = true;
        }
        else if (isMarked)
        {
            outline.OutlineMode  = seenThroughWalls
                ? Outline.Mode.OutlineAll
                : Outline.Mode.OutlineVisible;
            outline.OutlineColor = markedColor;
            outline.OutlineWidth = markedWidth;
            outline.enabled      = true;
        }
        else
        {
            outline.enabled = false;
        }
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

        outline.OutlineColor = markedColor;
        outline.OutlineWidth = markedWidth;
        outline.enabled      = false;
    }
}