using UnityEngine;
using Ami.BroAudio;

public class OnStartEnabler : MonoBehaviour
{
    public SoundSource mainMusic;
    public SoundSource telegraphAmbient;
    public SoundSource voda;
    void Start()
    {
        mainMusic.Play();
        telegraphAmbient.Play();
        voda.Play();
    }
}
