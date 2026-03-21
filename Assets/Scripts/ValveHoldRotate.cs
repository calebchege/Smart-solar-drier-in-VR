using UnityEngine;

public class ValveHoldRotate : MonoBehaviour
{
    public ManualButterflyValve valve;

    [Header("Manual Control")]
    public float rotationSpeed = 0.15f; // valve units per second

    private bool isHolding = false;
    private int direction = 1; // flips each grab

    void OnMouseDown()
    {
        if (!ManualModeManager.Instance.manualModeEnabled)
            return;

        isHolding = true;
        direction *= -1; // reverse direction each grab
    }

    void OnMouseUp()
    {
        isHolding = false;
    }

    void Update()
    {
        if (!isHolding) return;
        if (!ManualModeManager.Instance.manualModeEnabled) return;

        valve.valvePosition += direction * rotationSpeed * Time.deltaTime;
        valve.valvePosition = Mathf.Clamp01(valve.valvePosition);
    }
}
