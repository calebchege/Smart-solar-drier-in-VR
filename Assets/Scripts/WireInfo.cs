using UnityEngine;

public class WireInfo : MonoBehaviour
{
    [Header("Wire Properties")]
    public string wireName;
    public string wireType;           // Live, Neutral, Ground, Signal
    public string purpose;            // Power, Signal
    public float voltage;             // 230, 120, 24 V

    [Header("Component Connections")]
    public string connectedFrom;
    public string connectedTo;

    [Header("Materials")]
    public Material normalMaterial;
    public Material highlightMaterial;

    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (normalMaterial == null)
            normalMaterial = rend.material;
    }

    public void Highlight()
    {
        if (highlightMaterial != null)
            rend.material = highlightMaterial;
    }

    public void Unhighlight()
    {
        if (normalMaterial != null)
            rend.material = normalMaterial;
    }
}
