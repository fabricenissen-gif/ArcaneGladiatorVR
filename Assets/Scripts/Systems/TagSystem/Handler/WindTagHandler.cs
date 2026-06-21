using UnityEngine;

[RequireComponent(typeof(TagHandler))]
public class WindTagHandler : MonoBehaviour
{
    [Header("Wind Knockback Amp")]
    [SerializeField] private float knockbackMultiplier = 2.5f;

    private TagHandler tagHandler;

    public float KnockbackMultiplier => knockbackMultiplier;

    private void Awake() { tagHandler = GetComponent<TagHandler>(); }

    /// Gibt true + Multiplikator zurück wenn WIND aktiv ist.
    /// Wind wird NICHT verbraucht — anhaltender Debuff.
    public bool TryGetWindKnockback(out float multiplier)
    {
        if (!tagHandler.HasTag(TagType.WIND))
        {
            multiplier = 1f;
            return false;
        }
        multiplier = knockbackMultiplier;
        return true;
    }
}