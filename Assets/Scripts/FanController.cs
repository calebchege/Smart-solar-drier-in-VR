using UnityEngine;

public class FanController : MonoBehaviour
{
    [Header("Fan Rotation Settings")]
    public float maxRPM = 1200f;
    public float currentSpeed = 0f; // 0.0 to 1.0

    [Header("Animation Target")]
    public Transform fanBlades;

    private void Update()
    {
        if (fanBlades == null) return;

        float rpm = currentSpeed * maxRPM;
        float rotationPerFrame = (rpm / 60f) * 360f * Time.deltaTime;

        fanBlades.Rotate(rotationPerFrame, 0f, 0f, Space.Self);
    }

    public void SetSpeed(float speed)
    {
        currentSpeed = Mathf.Clamp01(speed);
    }

    public void UpdateAnimation() { /* unused here but called from DryerControl for clarity */ }
}
