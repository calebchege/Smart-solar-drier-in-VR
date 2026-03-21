using UnityEngine;

public class ThreeWayValveIndicator : MonoBehaviour
{
    [Range(0f, 1f)]
    public float valvePosition;
    // 0.0 → next heater
    // 1.0 → heat exchanger

    [Header("Visual")]
    public Renderer indicatorRenderer;

    public Color toNextHeaterColor = Color.blue;
    public Color toHeatExchangerColor = Color.red;
    public Color partialFlowColor = new Color(0.6f, 0f, 0.6f); // purple

    [Header("Thresholds")]
    [Range(0f, 0.5f)]
    public float partialRange = 0.15f;

    public void SetValvePosition(float position)
    {
        valvePosition = Mathf.Clamp01(position);
        UpdateColor();
    }

    void UpdateColor()
    {
        if (indicatorRenderer == null) return;

        if (valvePosition <= partialRange)
        {
            indicatorRenderer.material.color = toNextHeaterColor;
        }
        else if (valvePosition >= 1f - partialRange)
        {
            indicatorRenderer.material.color = toHeatExchangerColor;
        }
        else
        {
            indicatorRenderer.material.color = partialFlowColor;
        }
    }
}
