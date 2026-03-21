using UnityEngine;

public class ButterflyValveController : MonoBehaviour
{
    [Header("Valve State")]
    [Range(0f, 1f)]
    public float currentOpening = 0f;   // 0 = closed, 1 = fully open

    [Header("Valve Dynamics")]
    [Tooltip("Maximum rotation speed (degrees/second)")]
    public float maxRotationSpeed = 90f;

    [Tooltip("Current rotation angle (0-90 degrees)")]
    public float currentAngle = 0f;

    [Header("Visual Representation")]
    public Transform valveDisk; // Visual element to rotate
    public float closedAngle = 0f;
    public float openAngle = 90f;

    [Header("Fault Simulation")]
    [Tooltip("Simulate valve sticking at certain positions")]
    public bool simulateSticking = false;

    [Tooltip("Positions where valve might stick (0-1)")]
    public float[] stickPositions = new float[] { 0f, 0.5f, 1f };

    [Tooltip("Probability of sticking at these positions")]
    public float stickProbability = 0.1f;

    private float targetOpening;
    private bool isStuck = false;
    private float stuckPosition;

    void Start()
    {
        targetOpening = currentOpening;
        currentAngle = Mathf.Lerp(closedAngle, openAngle, currentOpening);

        if (valveDisk != null)
        {
            valveDisk.localRotation = Quaternion.Euler(currentAngle, 0f, 0f);
        }
    }

    void Update()
    {
        // Smooth movement toward target
        if (!isStuck && Mathf.Abs(currentOpening - targetOpening) > 0.001f)
        {
            float maxMovement = maxRotationSpeed * Time.deltaTime / 90f; // Convert to 0-1 range
            currentOpening = Mathf.MoveTowards(currentOpening, targetOpening, maxMovement);

            // Update visual rotation
            UpdateValveVisual();
        }

        // Check for sticking
        if (simulateSticking && !isStuck)
        {
            CheckForSticking();
        }
    }

    // Called by DryerControl (MAINTAINED FOR COMPATIBILITY)
    public void SetOpening(float opening)
    {
        if (isStuck)
        {
            Debug.LogWarning($"Valve {gameObject.name} is stuck at {stuckPosition:P0}");
            return;
        }

        targetOpening = Mathf.Clamp01(opening);

        // Reset sticking check when commanded to move
        isStuck = false;
    }

    // Getter convenience (MAINTAINED FOR COMPATIBILITY)
    public float GetOpening()
    {
        return currentOpening;
    }

    // Get target opening (for diagnostics)
    public float GetTargetOpening()
    {
        return targetOpening;
    }

    // Get actual vs commanded position (for diagnostics)
    public float GetPositionError()
    {
        return targetOpening - currentOpening;
    }

    // Maintenance function to clear sticking
    public void ClearSticking()
    {
        isStuck = false;
        Debug.Log($"Valve {gameObject.name} sticking cleared");
    }

    private void UpdateValveVisual()
    {
        if (valveDisk == null) return;

        currentAngle = Mathf.Lerp(closedAngle, openAngle, currentOpening);
        valveDisk.localRotation = Quaternion.Euler(currentAngle, 0f, 0f);
    }

    private void CheckForSticking()
    {
        foreach (float stickPos in stickPositions)
        {
            if (Mathf.Abs(currentOpening - stickPos) < 0.05f)
            {
                if (Random.value < stickProbability * Time.deltaTime)
                {
                    isStuck = true;
                    stuckPosition = currentOpening;
                    Debug.LogWarning($"Valve {gameObject.name} stuck at {stuckPosition:P0}");
                    break;
                }
            }
        }
    }

    // For UI display
    public string GetStatus()
    {
        if (isStuck) return $"STUCK at {stuckPosition:P0}";
        if (Mathf.Abs(currentOpening - targetOpening) > 0.01f) return $"MOVING to {targetOpening:P0}";
        return $"STABLE at {currentOpening:P0}";
    }

    // Check if valve is stuck (for PLCBridge compatibility)
    public bool IsStuck()
    {
        return isStuck;
    }

    // Force set opening (for PLCBridge override)
    public void ForceSetOpening(float opening)
    {
        isStuck = false;
        currentOpening = Mathf.Clamp01(opening);
        targetOpening = currentOpening;
        UpdateValveVisual();
    }
}