using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HandWheelGrabRotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Transform rotationAxis;       // The Transform to rotate
    public Vector3 rotationDirection = Vector3.forward;  // Rotation axis, editable in Inspector
    public float rotationSpeed = 120f;   // Degrees per second
    public float maxRotation = 360f;     // Maximum rotation in one directionprivate Quaternion initialLocalRotation;
    private Quaternion initialLocalRotation;
    private Quaternion initialLocalRotationQuaternion;
    private XRGrabInteractable grabInteractable;
    private bool isGrabbed = false;
    private int directionMultiplier = 1; // 1 for normal, -1 for reverse

    private float currentRotation = 0f;  // Tracks accumulated rotation

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (rotationAxis == null)
        {
            rotationAxis = this.transform; // Default to self if not assigned
        }
        initialLocalRotation = rotationAxis.localRotation;

    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    private void Update()
    {
        if (!ManualModeManager.Instance.manualModeEnabled || !isGrabbed)
            return;

        // Calculate rotation for this frame
        float rotationThisFrame = rotationSpeed * Time.deltaTime * directionMultiplier;

        // Predict new rotation
        float newRotation = currentRotation + rotationThisFrame;

        // Clamp rotation between 0 and maxRotation
        if (newRotation > maxRotation)
        {
            rotationThisFrame = maxRotation - currentRotation;
            currentRotation = maxRotation;
        }
        else if (newRotation < 0f)
        {
            rotationThisFrame = -currentRotation; // go back to 0
            currentRotation = 0f;
        }
        else
        {
            currentRotation = newRotation;
        }

        // Apply rotation
        rotationAxis.Rotate(rotationDirection.normalized * rotationThisFrame, Space.Self);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (!ManualModeManager.Instance.manualModeEnabled)
        {
            grabInteractable.interactionManager.CancelInteractableSelection(grabInteractable as IXRSelectInteractable);
            return;
        }

        isGrabbed = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;

        // Flip direction for next grab
        directionMultiplier *= -1;
    }
    public void SetAutoRotationNormalized(float normalized01)
    {
        normalized01 = Mathf.Clamp01(normalized01);

        currentRotation = normalized01 * maxRotation;

        Vector3 axis = rotationDirection.normalized;
        rotationAxis.localRotation =
            initialLocalRotation * Quaternion.AngleAxis(currentRotation, axis);
    }

}
