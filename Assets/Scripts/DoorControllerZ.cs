using UnityEngine;

public class DoorControllerZ_Creaking : MonoBehaviour
{
    [Header("Door Settings")]
    public float openZ = -90f;       // Z rotation when open
    public float closedZ = 0f;       // Z rotation when closed
    public float speed = 2f;         // Rotation speed
    public AudioSource creakSound;   // Assign your creak AudioSource here

    private bool isOpen = false;             // Tracks door state
    private Quaternion targetRotation;       // Current target rotation
    private float rotationThreshold = 0.1f; // Threshold to stop creak sound

    void Start()
    {
        // Start at current rotation
        targetRotation = transform.localRotation;
    }

    void Update()
    {
        // Smoothly rotate toward target rotation
        if (Quaternion.Angle(transform.localRotation, targetRotation) > 0.01f)
        {
            // Start playing creak if not already
            if (creakSound != null && !creakSound.isPlaying)
                creakSound.Play();

            // Rotate smoothly
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * speed);
        }
        else
        {
            // Stop creak when door stops moving
            if (creakSound != null && creakSound.isPlaying)
                creakSound.Stop();

            // Snap exactly to target rotation to avoid tiny jitter
            transform.localRotation = targetRotation;
        }
    }

    // Call this when grabbing the doorknob
    public void ToggleDoor()
    {
        isOpen = !isOpen;

        // Calculate target rotation
        float zAngle = isOpen ? openZ : closedZ;
        targetRotation = Quaternion.Euler(90f, 0f, zAngle);
    }
}
