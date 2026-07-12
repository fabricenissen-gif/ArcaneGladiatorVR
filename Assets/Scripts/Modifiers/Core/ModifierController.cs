using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Verwaltet die ausgerüsteten Modifier eines Besitzers.
/// Diese neutrale Basis besitzt noch keine Combat-Events.
/// WeaponModifierController wird später davon ableiten.
/// </summary>
[DisallowMultipleComponent]
public class ModifierController : MonoBehaviour
{
    [Header("Scope")]
    [SerializeField] private ModifierScope supportedScope =
        ModifierScope.Weapon;

    [Header("Slots")]
    [Tooltip(
        "Technische Obergrenze. Für Spell Delver geplant: 5 " +
        "(4 Standard + 1 Slot-Erweiterung).")]
    [SerializeField, Range(1, 5)] private int maxSlots = 5;

    [Tooltip(
        "Im aktuellen Run verfügbare Slots. " +
        "Für Tests zunächst 2; später standardmäßig 4.")]
    [SerializeField, Range(1, 5)] private int unlockedSlots = 2;

    [Header("Development Loadout")]
    [Tooltip(
        "Wird beim Aktivieren automatisch ausgerüstet. " +
        "Nur für schnelle Entwicklungs- und Combat-Tests.")]
    [SerializeField] private List<ModifierDefinition> startingModifiers =
        new List<ModifierDefinition>();

    [Header("Debug")]
    [SerializeField] private bool logLifecycle = true;

    private readonly List<ModifierRuntime> activeModifiers =
        new List<ModifierRuntime>();

    private readonly Dictionary<string, ModifierRuntime> modifiersById =
        new Dictionary<string, ModifierRuntime>(
            StringComparer.Ordinal);

    public event Action<ModifierRuntime> ModifierEquipped;
    public event Action<ModifierRuntime> ModifierUnequipped;

    public ModifierScope SupportedScope => supportedScope;
    public int MaxSlots => maxSlots;
    public int UnlockedSlots => unlockedSlots;
    public int ActiveModifierCount => activeModifiers.Count;
    public bool HasFreeSlot => ActiveModifierCount < UnlockedSlots;

    public IReadOnlyList<ModifierRuntime> ActiveModifiers =>
        activeModifiers;

    private void Awake()
    {
        ClampSlotValues();
    }

    private void OnEnable()
    {
        EquipStartingModifiers();
    }

    private void OnDisable()
    {
        UnequipAll();
    }

    /// <summary>
    /// Rüstet eine Definition sicher aus.
    /// Rückgabewert false bedeutet: Die Anfrage wurde abgewiesen,
    /// ohne den bestehenden Loadout-Zustand zu verändern.
    /// </summary>
    public bool TryEquip(ModifierDefinition definition)
    {
        if (!ValidateDefinition(definition))
            return false;

        string modifierId = definition.ModifierId;

        if (!definition.AllowDuplicates &&
            modifiersById.ContainsKey(modifierId))
        {
            LogWarning(
                $"Equip rejected: '{modifierId}' is already active.");

            return false;
        }

        if (!HasFreeSlot)
        {
            LogWarning(
                $"Equip rejected: no free slot. " +
                $"Active:{ActiveModifierCount}/{UnlockedSlots}.");

            return false;
        }

        ModifierRuntime runtime = definition.CreateRuntime();

        if (runtime == null)
        {
            LogError(
                $"Equip rejected: '{modifierId}' returned no runtime.");

            return false;
        }

        if (runtime.Definition != definition)
        {
            LogError(
                $"Equip rejected: runtime for '{modifierId}' has " +
                "a mismatching definition.");

            return false;
        }

        try
        {
            activeModifiers.Add(runtime);

            if (!definition.AllowDuplicates)
                modifiersById.Add(modifierId, runtime);

            runtime.Activate(this);
        }
        catch (Exception exception)
        {
            activeModifiers.Remove(runtime);

            if (!definition.AllowDuplicates)
                modifiersById.Remove(modifierId);

            try
            {
                runtime.Deactivate();
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException, this);
            }

            LogError(
                $"Equip failed for '{modifierId}': " +
                $"{exception.GetType().Name} - {exception.Message}");

            return false;
        }

        if (logLifecycle)
        {
            Debug.Log(
                $"[ModifierController] Equipped '{modifierId}' | " +
                $"Slots:{ActiveModifierCount}/{UnlockedSlots} | " +
                $"Owner:{name}");
        }

        ModifierEquipped?.Invoke(runtime);
        return true;
    }

    /// <summary>
    /// Entfernt exakt einen Modifier über seine stabile technische ID.
    /// </summary>
    public bool TryUnequip(string modifierId)
    {
        if (string.IsNullOrWhiteSpace(modifierId))
        {
            LogWarning("Unequip rejected: modifier ID is empty.");
            return false;
        }

        modifierId = modifierId.Trim();

        ModifierRuntime runtime = FindRuntimeById(modifierId);

        if (runtime == null)
        {
            LogWarning(
                $"Unequip rejected: '{modifierId}' is not active.");

            return false;
        }

        activeModifiers.Remove(runtime);

        if (runtime.Definition != null &&
            !runtime.Definition.AllowDuplicates)
        {
            modifiersById.Remove(runtime.ModifierId);
        }

        try
        {
            runtime.Deactivate();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }

        if (logLifecycle)
        {
            Debug.Log(
                $"[ModifierController] Unequipped '{modifierId}' | " +
                $"Slots:{ActiveModifierCount}/{UnlockedSlots} | " +
                $"Owner:{name}");
        }

        ModifierUnequipped?.Invoke(runtime);
        return true;
    }

    /// <summary>
    /// Entfernt alle Modifier in umgekehrter Slot-Reihenfolge.
    /// Das ist sicher für spätere Abhängigkeiten zwischen Modifiern.
    /// </summary>
    public void UnequipAll()
    {
        for (int i = activeModifiers.Count - 1; i >= 0; i--)
        {
            ModifierRuntime runtime = activeModifiers[i];

            if (runtime == null)
                continue;

            string modifierId = runtime.ModifierId;

            activeModifiers.RemoveAt(i);

            if (runtime.Definition != null &&
                !runtime.Definition.AllowDuplicates)
            {
                modifiersById.Remove(modifierId);
            }

            try
            {
                runtime.Deactivate();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (logLifecycle)
            {
                Debug.Log(
                    $"[ModifierController] Unequipped '{modifierId}' | " +
                    $"Owner:{name}");
            }

            ModifierUnequipped?.Invoke(runtime);
        }

        modifiersById.Clear();
    }

    public bool IsEquipped(string modifierId)
    {
        if (string.IsNullOrWhiteSpace(modifierId))
            return false;

        return FindRuntimeById(modifierId.Trim()) != null;
    }

    public bool TrySetUnlockedSlots(int newUnlockedSlots)
    {
        newUnlockedSlots = Mathf.Clamp(
            newUnlockedSlots,
            1,
            maxSlots);

        if (newUnlockedSlots < ActiveModifierCount)
        {
            LogWarning(
                $"Slot change rejected: {ActiveModifierCount} active " +
                $"modifier(s) do not fit into {newUnlockedSlots} slot(s).");

            return false;
        }

        unlockedSlots = newUnlockedSlots;

        if (logLifecycle)
        {
            Debug.Log(
                $"[ModifierController] Unlocked slots set to " +
                $"{unlockedSlots}/{maxSlots} | Owner:{name}");
        }

        return true;
    }

    private void EquipStartingModifiers()
    {
        if (startingModifiers == null)
            return;

        foreach (ModifierDefinition definition in startingModifiers)
        {
            if (definition == null)
                continue;

            TryEquip(definition);
        }
    }

    private bool ValidateDefinition(ModifierDefinition definition)
    {
        if (definition == null)
        {
            LogWarning("Equip rejected: definition is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.ModifierId))
        {
            LogWarning(
                $"Equip rejected: '{definition.name}' has no modifier ID.");

            return false;
        }

        if (definition.Scope != supportedScope)
        {
            LogWarning(
                $"Equip rejected: '{definition.ModifierId}' has scope " +
                $"{definition.Scope}, but this controller supports " +
                $"{supportedScope}.");

            return false;
        }

        return true;
    }

    private ModifierRuntime FindRuntimeById(string modifierId)
    {
        if (modifiersById.TryGetValue(
                modifierId,
                out ModifierRuntime uniqueRuntime))
        {
            return uniqueRuntime;
        }

        for (int i = 0; i < activeModifiers.Count; i++)
        {
            ModifierRuntime runtime = activeModifiers[i];

            if (runtime != null &&
                string.Equals(
                    runtime.ModifierId,
                    modifierId,
                    StringComparison.Ordinal))
            {
                return runtime;
            }
        }

        return null;
    }

    private void ClampSlotValues()
    {
        maxSlots = Mathf.Clamp(maxSlots, 1, 5);
        unlockedSlots = Mathf.Clamp(unlockedSlots, 1, maxSlots);
    }

    private void OnValidate()
    {
        ClampSlotValues();
    }

    private void LogWarning(string message)
    {
        if (logLifecycle)
            Debug.LogWarning($"[ModifierController] {message}", this);
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ModifierController] {message}", this);
    }
}