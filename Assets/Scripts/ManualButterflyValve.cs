using UnityEngine;

public class ManualButterflyValve : MonoBehaviour
{
    [Header("Valve Parts")]
    public Transform disc;
    public Transform handWheel;

    [Header("Rotation Limits")]
    public float closedAngle = 0f;
    public float openAngle = 90f;
    public float handWheelTurns = 5f;

    [Header("Valve State")]
    [Range(0f, 1f)]
    public float valvePosition = 0f; // 0 = closed, 1 = open

    void Update()
    {
        // Rotate disc
        float discAngle = Mathf.Lerp(closedAngle, openAngle, valvePosition);
        disc.localRotation = Quaternion.Euler(discAngle, 0f, 0f);

        // Rotate hand wheel
        float wheelAngle = valvePosition * handWheelTurns * 360f;
        handWheel.localRotation = Quaternion.Euler(wheelAngle, 0f, 0f);
    }
}
