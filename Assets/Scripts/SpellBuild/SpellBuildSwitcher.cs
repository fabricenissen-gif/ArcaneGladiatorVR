using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpellBuildSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MagicHandCaster magicHandCaster;
    [SerializeField] private SpellLoadoutData loadoutData;

    [Header("Input")]
    [Tooltip("z.B. <XRController>{LeftHand}/secondaryButton")]
    [SerializeField] private string switchButtonPath = "<XRController>{LeftHand}/secondaryButton";

    [Header("Switch Sound")]
    [SerializeField] private AudioSource switchAudioSource;
    [SerializeField] private AudioClip switchToAClip;
    [SerializeField] private AudioClip switchToBClip;

    [Header("Current Build")]
    [SerializeField] private SpellBuildType currentBuild = SpellBuildType.BuildA;

    public SpellBuildType CurrentBuild => currentBuild;
    public SpellData CurrentSpell => GetSpellForBuild(currentBuild);

    public Action<SpellBuildType, SpellData> OnBuildSwitched;

    private InputAction switchAction;

    private void Awake()
    {
        if (magicHandCaster == null)
            magicHandCaster = GetComponent<MagicHandCaster>();

        switchAction = new InputAction(
            name: "SwitchBuild",
            type: InputActionType.Button,
            binding: switchButtonPath
        );
    }

    private void OnEnable()
    {
        if (switchAction == null) return;
        switchAction.Enable();
        switchAction.performed += OnSwitchBuildPerformed;
    }

    private void OnDisable()
    {
        if (switchAction == null) return;
        switchAction.performed -= OnSwitchBuildPerformed;
        switchAction.Disable();
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

        PlaySwitchSound();

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

    private void PlaySwitchSound()
    {
        if (switchAudioSource == null) return;

        AudioClip clip = currentBuild == SpellBuildType.BuildA
            ? switchToAClip
            : switchToBClip;

        // Fallback: wenn nur ein Clip gesetzt ist, nimm den für beide
        if (clip == null)
            clip = switchToAClip != null ? switchToAClip : switchToBClip;

        if (clip == null) return;

        switchAudioSource.PlayOneShot(clip);
    }
}