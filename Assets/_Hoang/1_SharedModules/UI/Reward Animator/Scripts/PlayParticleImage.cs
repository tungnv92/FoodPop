using AssetKits.ParticleImage;
using UnityEngine;

public class PlayParticleImage : MonoBehaviour
{
    [SerializeField] ParticleImage effect;

    public void Play()
    {
        effect.Play();
    }
}
