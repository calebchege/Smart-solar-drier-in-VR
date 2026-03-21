using UnityEngine;

public class HeatExchangerController : MonoBehaviour
{
    public bool isActive;
    public AudioSource exchangerAudio;

    public void SetExchangerState(bool active)
    {
        isActive = active;

        if (exchangerAudio != null)
        {
            if (active && !exchangerAudio.isPlaying)
                exchangerAudio.Play();
            else if (!active)
                exchangerAudio.Stop();
        }
    }
}
