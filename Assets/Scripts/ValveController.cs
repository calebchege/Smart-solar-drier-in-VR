using UnityEngine;

public class ValveController : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color onColor = Color.red;
    public Color offColor = Color.blue;
    public float colorLerpSpeed = 5f;

    [Header("Audio")]
    public AudioSource valveAudio;

    [Header("Runtime State")]
    public bool isOpen = false;

    private Renderer valveRenderer;

    private void Start()
    {
        valveRenderer = GetComponent<Renderer>();

        if (valveRenderer != null)
            valveRenderer.material.color = isOpen ? onColor : offColor;
    }

    public void SetState(bool open)
    {
        isOpen = open;

        if (open && valveAudio != null && !valveAudio.isPlaying)
            valveAudio.Play();
        else if (!open && valveAudio != null)
            valveAudio.Stop();
    }

    public void UpdateAnimation()
    {
        if (valveRenderer == null) return;

        Color target = isOpen ? onColor : offColor;

        valveRenderer.material.color = Color.Lerp(
            valveRenderer.material.color,
            target,
            Time.deltaTime * colorLerpSpeed
        );
    }
}
