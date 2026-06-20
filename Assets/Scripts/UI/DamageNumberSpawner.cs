using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private DamageNumber damageNumberPrefab;

    [Header("Kamera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Farben pro TagType")]
    [SerializeField] private Color colorDefault   = new Color(1.00f, 1.00f, 1.00f);
    [SerializeField] private Color colorFire      = new Color(1.00f, 0.42f, 0.00f);
    [SerializeField] private Color colorFrost     = new Color(0.45f, 0.81f, 1.00f);
    [SerializeField] private Color colorElectric  = new Color(1.00f, 0.90f, 0.00f);
    [SerializeField] private Color colorWind      = new Color(0.66f, 1.00f, 0.82f);
    [SerializeField] private Color colorArcane    = new Color(0.78f, 0.49f, 1.00f);
    [SerializeField] private Color colorPoison    = new Color(0.50f, 1.00f, 0.00f);
    [SerializeField] private Color colorBleed     = new Color(0.80f, 0.10f, 0.10f);
    [SerializeField] private Color colorMarked    = new Color(1.00f, 0.60f, 0.00f);
    [SerializeField] private Color colorReaction  = new Color(1.00f, 0.84f, 0.00f); // Gold
    [SerializeField] private Color colorExposed   = new Color(1.00f, 0.13f, 0.13f); // Kritisch

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                cameraTransform = cam.transform;
            else
                Debug.LogWarning("[DamageNumberSpawner] Keine Kamera gefunden — Billboard funktioniert nicht.");
        }
    }

    // Normaler Schaden ohne Tag
    public void Spawn(Vector3 position, float damage)
    {
        SpawnInternal(position, damage, colorDefault, false);
    }

    // Schaden durch einen bestimmten Tag-Typ
    public void SpawnTagged(Vector3 position, float damage, TagType tag)
    {
        Color color = GetTagColor(tag);
        SpawnInternal(position, damage, color, false);
    }

    // Reaktionsschaden (Combo) — größer und Gold
    public void SpawnReaction(Vector3 position, float damage, bool isExposed = false)
    {
        Color color = isExposed ? colorExposed : colorReaction;
        SpawnInternal(position, damage, color, true);
    }

private void SpawnInternal(Vector3 position, float damage, Color color, bool isBig)
{
    if (damageNumberPrefab == null)
    {
        Debug.LogError("[DamageNumberSpawner] damageNumberPrefab ist nicht gesetzt!");
        return;
    }

    DamageNumber instance = Instantiate(damageNumberPrefab, position + Vector3.up * 0.5f, Quaternion.identity);
    string text = Mathf.RoundToInt(damage).ToString();
    instance.Initialize(text, color, isBig, cameraTransform);
}

    private Color GetTagColor(TagType tag)
    {
        switch (tag)
        {
            case TagType.FIRE:     return colorFire;
            case TagType.FROST:    return colorFrost;
            case TagType.ELECTRIC: return colorElectric;
            case TagType.WIND:     return colorWind;
            case TagType.ARCANE:   return colorArcane;
            case TagType.POISON:   return colorPoison;
            case TagType.BLEED:    return colorBleed;
            case TagType.MARKED:   return colorMarked;
            default:               return colorDefault;
        }
    }
}