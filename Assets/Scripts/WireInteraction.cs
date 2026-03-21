using UnityEngine;

public class WireInteraction : MonoBehaviour
{
    [Header("UI")]
    public GameObject infoCanvas;        // must start inactive
    public TMPro.TextMeshProUGUI infoText;

    public float canvasOffset = 0.15f;   // distance from wire

    private WireInfo lastWire;

    public void ShowWireInfo(WireInfo wire)
    {
        // Remove highlight from previous wire
        if (lastWire != null)
            lastWire.Unhighlight();

        lastWire = wire;
        wire.Highlight();

        // Move canvas near the wire
        Debug.Log("ShowWireInfo CALLED with wire: " + wire);

        PositionCanvasNearWire(wire);

        // Enable UI
        infoCanvas.SetActive(true);

        // Fill text
        infoText.text =
            $"<b>{wire.wireName}</b>\n\n" +
            $"Type: {wire.wireType}\n" +
            $"Purpose: {wire.purpose}\n" +
            $"Voltage: {wire.voltage} V\n\n" +
            $"Connected From:\n{wire.connectedFrom}\n\n" +
            $"Connected To:\n{wire.connectedTo}";
    }

    private void PositionCanvasNearWire(WireInfo wire)
    {
        if (Camera.main == null) return;

        Vector3 wirePos = wire.transform.position;

        // direction FROM wire TO camera
        Vector3 directionToCamera = (Camera.main.transform.position - wirePos).normalized;

        // place canvas between user and wire
        infoCanvas.transform.position = wirePos + directionToCamera * canvasOffset;

        // face camera
        infoCanvas.transform.rotation = Quaternion.LookRotation(directionToCamera);
        infoCanvas.transform.Rotate(0,180f,0f,Space.Self); // flip to face user
    }

    public void HideInfo()
    {
        if (lastWire != null)
            lastWire.Unhighlight();

        infoCanvas.SetActive(false);
    }
}
