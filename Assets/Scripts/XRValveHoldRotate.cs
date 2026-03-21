using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class XRValveHoldRotate : MonoBehaviour
{
    public ManualButterflyValve valve;

    [Header("Manual Rotation")]
    public float rotationSpeed = 0.15f; // valve units per second

    private bool isHeld = false;
    private int direction = 1;

    void OnEnable()
    {
        var grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        var grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (!ManualModeManager.Instance.manualModeEnabled)
            return;

        isHeld = true;
        direction *= -1; // reverse direction each grab
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isHeld = false;
    }

    void Update()
    {
        if (!isHeld) return;
        if (!ManualModeManager.Instance.manualModeEnabled) return;

        valve.valvePosition += direction * rotationSpeed * Time.deltaTime;
        valve.valvePosition = Mathf.Clamp01(valve.valvePosition);
    }
}
