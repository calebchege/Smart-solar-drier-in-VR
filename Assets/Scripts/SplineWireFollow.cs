using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(SplineContainer))]
public class SplineWireFollow : MonoBehaviour
{
    [Tooltip("The plug's grabbable object (must have XRGrabInteractable)")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable plugInteractable;

    [Tooltip("The empty GameObject defining the socket's snap position")]
    public Transform socketObject;

    [Tooltip("How close the plug must be to snap (in meters)")]
    public float snapDistance = 0.1f;

    private SplineContainer m_SplineContainer;
    private Spline m_Spline;
    private int m_LastKnotIndex;

    void Start()
    {
        m_SplineContainer = GetComponent<SplineContainer>();
        m_Spline = m_SplineContainer.Spline;

        if (plugInteractable == null || socketObject == null)
        {
            Debug.LogError("Plug or Socket not assigned in the Inspector!", this);
            return;
        }

        // FIXED: Use .Count property (correct)
        m_LastKnotIndex = m_Spline.Count - 1;

        if (m_LastKnotIndex < 1)
        {
            Debug.LogError("Spline needs at least 2 knots!", this);
            return;
        }

        plugInteractable.selectEntered.AddListener(OnPickUpPlug);
        plugInteractable.selectExited.AddListener(OnReleasePlug);

        Vector3 lastKnotWorldPosition = GetLastKnotWorldPosition();
        plugInteractable.transform.position = lastKnotWorldPosition;
    }

    void Update()
    {
        if (plugInteractable.isSelected)
        {
            MoveSplineEnd(plugInteractable.transform.position);
        }
    }

    private void OnPickUpPlug(SelectEnterEventArgs args)
    {
        // Optional: unplug sound
    }

    private void OnReleasePlug(SelectExitEventArgs args)
    {
        float distanceToSocket = Vector3.Distance(
            plugInteractable.transform.position,
            socketObject.position
        );

        if (distanceToSocket <= snapDistance)
        {
            plugInteractable.transform.position = socketObject.position;
            plugInteractable.transform.rotation = socketObject.rotation;
            MoveSplineEnd(socketObject.position);
        }
    }

    void MoveSplineEnd(Vector3 targetWorldPosition)
    {
        Vector3 targetLocalPosition = m_SplineContainer.transform.InverseTransformPoint(targetWorldPosition);

        // FIXED: Use direct spline indexer and BezierKnot
        BezierKnot knot = m_Spline[m_LastKnotIndex];
        knot.Position = targetLocalPosition;
        m_Spline.SetKnot(m_LastKnotIndex, knot);
    }

    Vector3 GetLastKnotWorldPosition()
    {
        // FIXED: Use direct spline indexer and BezierKnot
        BezierKnot lastKnot = m_Spline[m_LastKnotIndex];
        return m_SplineContainer.transform.TransformPoint(lastKnot.Position);
    }
}