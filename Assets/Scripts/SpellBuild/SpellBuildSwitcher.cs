using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpellBuildSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MagicHandCaster magicHandCaster;
    [SerializeField] private SpellLoadoutData loadoutData;

    [Header("Input")]
    [SerializeField] private InputActionReference switchBuildAction;

    [Header("Current Build")]
    [SerializeField] private SpellBuildType currentBuild = SpellBuildType.BuildA;

    public SpellBuildType CurrentBuild => currentBuild;
    public SpellData CurrentSpell => GetSpellForBuild(currentBuild);

    public Action<SpellBuildType, SpellData> OnBuildSwitched;

    private void Awake()
    {
        if (magicHandCaster == null)
            magicHandCaster = GetComponent<MagicHandCaster>();
    }

    private void OnEnable()
    {
        if (switchBuildAction?.action == null) return;
        switchBuildAction.action.Enable();
        switchBuildAction.action.performed += OnSwitchBuildPerformed;
    }

    private void OnDisable()
    {
        if (switchBuildAction?.action == null) return;
        switchBuildAction.action.performed -= OnSwitchBuildPerformed;
        switchBuildAction.action.Disable();
    }

    private void Start()
    {
        ApplyCurrentBuild();
    }

    private void OnSwitchBuildPerformed(InputAction.CallbackContext context)
    {
        SwitchBuild();
    }

    public void SwitchBuild()
    {
        currentBuild = currentBuild == SpellBuildType.BuildA
            ? SpellBuildType.BuildB
            : SpellBuildType.BuildA;

        ApplyCurrentBuild();
    }

    public void SetBuild(SpellBuildType targetBuild)
    {
        currentBuild = targetBuild;
        ApplyCurrentBuild();
    }

    public SpellData GetSpellForBuild(SpellBuildType buildType)
    {
        if (loadoutData == null) return null;

        return buildType == SpellBuildType.BuildA
            ? loadoutData.buildASpell
            : loadoutData.buildBSpell;
    }

    public void SetSpellForBuild(SpellBuildType buildType, SpellData newSpell)
    {
        if (loadoutData == null)
        {
            Debug.LogError("[SpellBuildSwitcher] Kein LoadoutData zugewiesen.");
            return;
        }

        if (buildType == SpellBuildType.BuildA)
            loadoutData.buildASpell = newSpell;
        else
            loadoutData.buildBSpell = newSpell;

        if (currentBuild == buildType)
            ApplyCurrentBuild();
    }

    private void ApplyCurrentBuild()
    {
        if (magicHandCaster == null)
        {
            Debug.LogError("[SpellBuildSwitcher] MagicHandCaster fehlt.");
            return;
        }

        SpellData spellToEquip = GetSpellForBuild(currentBuild);

        if (spellToEquip == null)
        {
            Debug.LogWarning("[SpellBuildSwitcher] Kein Spell für " + currentBuild + " gesetzt.");
            magicHandCaster.SetSpell(null);
            OnBuildSwitched?.Invoke(currentBuild, null);
            return;
        }

        magicHandCaster.SetSpell(spellToEquip);
        OnBuildSwitched?.Invoke(currentBuild, spellToEquip);

        Debug.Log("[SpellBuildSwitcher] Aktiver Build: " + currentBuild + " | Spell: " + spellToEquip.spellName);
    }
}