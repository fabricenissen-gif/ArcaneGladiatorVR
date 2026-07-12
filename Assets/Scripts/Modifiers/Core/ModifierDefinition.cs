using UnityEngine;

/// <summary>
/// Daten-Asset eines Modifiers.
/// Enthält nur unveränderliche Konfigurations- und UI-Daten.
/// Laufzeit-Zustand gehört niemals in dieses ScriptableObject.
/// </summary>
[CreateAssetMenu(
    fileName = "NewModifier",
    menuName = "SpellDelver/Modifiers/Modifier Definition")]
public class ModifierDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip(
        "Stabile technische ID, z. B. 'weapon.bloodletter'. " +
        "Nach dem ersten Einsatz nicht mehr umbenennen.")]
    [SerializeField] private string modifierId = "modifier.new";

    [SerializeField] private string displayName = "New Modifier";

    [TextArea(2, 4)]
    [SerializeField] private string description;

    [Header("Classification")]
    [SerializeField] private ModifierScope scope = ModifierScope.Weapon;

    [Tooltip(
        "Für spätere UI- und Loot-Systeme. " +
        "Hat in diesem Schritt noch keine Gameplay-Wirkung.")]
    [SerializeField, Min(0)] private int rarityTier;

    [SerializeField] private Sprite icon;

    [Header("Rules")]
    [Tooltip(
        "V1 verwendet keine doppelten Modifier-IDs. " +
        "Dieses Feld ist für eine spätere, bewusste Stack-Regel vorbereitet.")]
    [SerializeField] private bool allowDuplicates;

    public string ModifierId => modifierId;
    public string DisplayName => displayName;
    public string Description => description;
    public ModifierScope Scope => scope;
    public int RarityTier => rarityTier;
    public Sprite Icon => icon;
    public bool AllowDuplicates => allowDuplicates;

    /// <summary>
    /// Erstellt die Runtime-Instanz für einen Equip-Vorgang.
    /// Spezialisierte Assets wie BloodletterDefinition überschreiben
    /// diese Methode später und liefern ihre eigene Runtime.
    /// </summary>
    public virtual ModifierRuntime CreateRuntime()
    {
        return new ModifierRuntime(this);
    }

    private void OnValidate()
    {
        modifierId = modifierId?.Trim() ?? string.Empty;
        displayName = displayName?.Trim() ?? string.Empty;
        rarityTier = Mathf.Max(0, rarityTier);
    }
}