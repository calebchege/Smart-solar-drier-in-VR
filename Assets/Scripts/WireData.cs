using UnityEngine;

public class WireData : MonoBehaviour
{
    [Header("Wire Information")]
    public string wireName = "Main Feed";
    public string status = "Live";
    public string voltage = "120V";
    [TextArea]
    public string description = "From PV Circuit Breaker to Inverter";

    // Color code for the status (optional)
    public Color statusColor = Color.red;
}