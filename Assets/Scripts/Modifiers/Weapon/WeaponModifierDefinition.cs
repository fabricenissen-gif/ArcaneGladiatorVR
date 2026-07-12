using UnityEngine;

/// <summary>
/// Basis-Asset für Modifier, die einem WeaponModifierRuntime
/// auf einer Waffe zugeordnet werden.
/// </summary>
public abstract class WeaponModifierDefinition : ModifierDefinition
{
    public sealed override ModifierRuntime CreateRuntime()
    {
        return CreateWeaponRuntime();
    }

    protected abstract WeaponModifierRuntime CreateWeaponRuntime();
}