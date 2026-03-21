using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRBaseInteractable))]
public class SwitchInteraction : MonoBehaviour
{
    [Header("Switch Settings")]
    public Transform switchHandle;
    public Vector3 onRotation = new Vector3(-30f, 0f, 0f);
    public Vector3 offRotation = new Vector3(30f, 0f, 0f);
    public float flipDuration = 0.2f;

    [Header("Feedback")]
    public AudioSource clickSound;          // ← Add this
    public Renderer indicatorRenderer;      // ← For glow effect
    public Color onEmissionColor = Color.green;
    public Color offEmissionColor = Color.red;
    public float emissionIntensity = 2f;

    private bool isOn = false;
    private bool isAnimating = false;

    private void Start()
    {
        XRBaseInteractable interactable = GetComponent<XRBaseInteractable>();
        interactable.activated.AddListener(OnActivated);

        // Initialize emission to OFF state
        UpdateEmission(false);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        ToggleSwitch();
    }

    public void ToggleSwitch()
    {
        if (isAnimating) return;
        StartCoroutine(FlipSwitch());
    }

    private IEnumerator FlipSwitch()
    {
        isAnimating = true;
        Quaternion startRot = switchHandle.localRotation;
        Quaternion endRot = Quaternion.Euler(isOn ? offRotation : onRotation);

        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            switchHandle.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / flipDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        switchHandle.localRotation = endRot;
        isOn = !isOn;
        isAnimating = false;

        // ✅ Play sound when switch flips
        if (clickSound != null)
            clickSound.Play();

        // ✅ Update emissive glow
        UpdateEmission(isOn);

        Debug.Log("Switch is now " + (isOn ? "ON" : "OFF"));
    }

    private void UpdateEmission(bool on)
    {
        if (indicatorRenderer == null) return;

        // Get material instance (avoid changing shared material)
        Material mat = indicatorRenderer.material;

        // Enable emission keyword
        mat.EnableKeyword("_EMISSION");

        // Set emissive color
        Color baseColor = on ? onEmissionColor : offEmissionColor;
        mat.SetColor("_EmissionColor", baseColor * emissionIntensity);
    }
}
