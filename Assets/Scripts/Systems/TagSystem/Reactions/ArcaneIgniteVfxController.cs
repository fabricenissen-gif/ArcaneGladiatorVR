using UnityEngine;

/// Startet alle Child-ParticleSystems gleichzeitig.
/// Wird von TagReactionSystem.burningVfxPrefab Instantiate() aufgerufen.
public class ArcaneIgniteVfxController : MonoBehaviour
{
    private void Start()
    {
        ParticleSystem[] all = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in all)
            ps.Play();
    }
}