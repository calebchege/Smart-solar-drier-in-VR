using UnityEngine;

public class SolarWaterHeatingLoop : MonoBehaviour
{
    [Header("Pipe Segments (in flow direction)")]
    public Renderer pipe_TankToHeater1;
    public Renderer pipe_Heater1ToHeater2;
    public Renderer pipe_Heater2ToHeater3;
    public Renderer pipe_Heater3ToHeatExchanger;
    public Renderer pipe_HeatExchangerToTank;

    [Header("Temperature Settings (°C)")]
    public float tankTemperature = 25f;
    public float heater1OutputTemp = 35f;
    public float heater2OutputTemp = 45f;
    public float heater3OutputTemp = 60f;
    public float heatExchangerExitTemp = 40f;

    [Header("Flow Settings")]
    public float flowSpeed = 0.5f;                // Texture scroll speed

    [Header("Color Gradients for Each Stage")]
    public Gradient tankToHeater1Color;
    public Gradient heater1ToHeater2Color;
    public Gradient heater2ToHeater3Color;
    public Gradient heater3ToHeatExchangerColor;
    public Gradient heatExchangerToTankColor;

    private Material mat_T1;
    private Material mat_12;
    private Material mat_23;
    private Material mat_3HX;
    private Material mat_HXTank;

    private float scrollValue;

    void Start()
    {
        // Instantiate materials so we don't alter the originals globally
        mat_T1 = pipe_TankToHeater1.material;
        mat_12 = pipe_Heater1ToHeater2.material;
        mat_23 = pipe_Heater2ToHeater3.material;
        mat_3HX = pipe_Heater3ToHeatExchanger.material;
        mat_HXTank = pipe_HeatExchangerToTank.material;
    }

    void Update()
    {
        SimulateFlow();
        UpdatePipeColors();
    }

    void SimulateFlow()
    {
        scrollValue += Time.deltaTime * flowSpeed;

        Vector2 offset = new Vector2(scrollValue, 0);

        mat_T1.mainTextureOffset = offset;
        mat_12.mainTextureOffset = offset;
        mat_23.mainTextureOffset = offset;
        mat_3HX.mainTextureOffset = offset;
        mat_HXTank.mainTextureOffset = offset;
    }

    void UpdatePipeColors()
    {
        // Normalize temperature values (0–1) for gradient evaluation
        float t0 = NormalizeTemp(tankTemperature);
        float t1 = NormalizeTemp(heater1OutputTemp);
        float t2 = NormalizeTemp(heater2OutputTemp);
        float t3 = NormalizeTemp(heater3OutputTemp);
        float tHX = NormalizeTemp(heatExchangerExitTemp);

        mat_T1.color = tankToHeater1Color.Evaluate(t0);
        mat_12.color = heater1ToHeater2Color.Evaluate(t1);
        mat_23.color = heater2ToHeater3Color.Evaluate(t2);
        mat_3HX.color = heater3ToHeatExchangerColor.Evaluate(t3);
        mat_HXTank.color = heatExchangerToTankColor.Evaluate(tHX);
    }

    float NormalizeTemp(float temp)
    {
        // Expected temp range (you can adjust it)
        float minTemp = 20f;
        float maxTemp = 70f;
        return Mathf.InverseLerp(minTemp, maxTemp, temp);
    }
}
