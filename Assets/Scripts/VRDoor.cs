using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public Transform doorPivot;               // Assign the hinge/pivot of the door
    public Vector3 closedRotation = Vector3.zero;
    public Vector3 openRotation = new Vector3(0f, 90f, 0f);
    public float openCloseSpeed = 2f;         // Rotation speed

    [Header("Audio Settings")]
    public AudioSource doorAudioSource;       // Optional audio source
    public AudioClip openClip;                // Sound when opening
    public AudioClip closeClip;               // Sound when closing

    private bool isOpen = false;
    private bool isAnimating = false;

    private XRBaseInteractable interactable;

    private void Awake()
    {
        // Ensure we have an interactable
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<XRSimpleInteractable>();
        }

        // If no pivot is assigned, use the object itself
        if (doorPivot == null)
            doorPivot = this.transform;
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnDoorSelected);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnDoorSelected);
    }

    private void OnDoorSelected(SelectEnterEventArgs args)
    {
        if (!isAnimating)
            StartCoroutine(RotateDoor());
    }

    private System.Collections.IEnumerator RotateDoor()
    {
        isAnimating = true;

        Quaternion startRotation = doorPivot.localRotation;
        Quaternion targetRotation = Quaternion.Euler(isOpen ? closedRotation : openRotation);

        // Play sound if assigned
        if (doorAudioSource != null)
        {
            doorAudioSource.clip = isOpen ? closeClip : openClip;
            doorAudioSource.Play();
        }

        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * openCloseSpeed;
            doorPivot.localRotation = Quaternion.Slerp(startRotation, targetRotation, Mathf.Clamp01(elapsed));
            yield return null;
        }

        doorPivot.localRotation = targetRotation;
        isOpen = !isOpen;
        isAnimating = false;
    }
}
