using UnityEngine;

public class PowerManager : MonoBehaviour
{
    [Header("References")]
    public SensorManager sensorManager;

    [Header("Current Power Source Status (Read Only)")]
    public bool solarActive;
    public bool batteryActive;
    public bool gridActive;

    [Header("Battery State")]
    [Range(0f, 100f)]
    public float batteryChargePercent = 100f;
    public float batteryLowCutoff = 20f;       // Switch to grid at 20%

    [Header("Solar Logic")]
    public float solarThresholdIrradiance = 300f;  // Minimum usable solar (W/m2)
    public float batteryAssistStart = 250f;         // Irradiance where battery supports solar

    [Header("Battery Charging")]
    public float maxIrradiance = 1100f;       // Peak sun
    public float batteryChargeRate = 5f;      // % per minute at full irradiance

    [Header("Battery Discharge (Simple Mode)")]
    public float batteryDrainPerSecond = 0.1f;   // Adjustable later

    void Start()
    {
        TryAttachSensor();
    }

    void TryAttachSensor()
    {
        if (sensorManager == null)
        {
            sensorManager = FindFirstObjectByType<SensorManager>();
            if (sensorManager == null)
                Debug.LogWarning("PowerManager: SensorManager not assigned and not found.");
        }
    }

    void Update()
    {
        if (sensorManager == null) return;

        float irradiance = sensorManager.pyranometer;
        DecidePowerSource(irradiance);
        UpdateBattery(irradiance);
    }

    // -------------------------------------------------------------
    // POWER PRIORITY LOGIC
    // -------------------------------------------------------------
    void DecidePowerSource(float irradiance)
    {
        // Reset all states
        solarActive = batteryActive = gridActive = false;

        // 1. Strong sun available → Solar only
        if (irradiance >= solarThresholdIrradiance)
        {
            solarActive = true;
            return;
        }

        // 2. Weak sun but still usable → Solar + Battery assist
        if (irradiance >= batteryAssistStart && batteryChargePercent > batteryLowCutoff)
        {
            solarActive = true;
            batteryActive = true;
            return;
        }

        // 3. No useful sun → Battery only (if above cutoff)
        if (batteryChargePercent > batteryLowCutoff)
        {
            batteryActive = true;
            return;
        }

        // 4. Battery is too low → Switch to grid to protect battery
        gridActive = true;
    }

    // -------------------------------------------------------------
    // BATTERY BEHAVIOR — simple charging/discharging
    // -------------------------------------------------------------
    void UpdateBattery(float irradiance)
    {
        // If solar is primary source (and not assisting)
        if (solarActive && !batteryActive)
        {
            // Charge proportionally to irradiance
            float solarRatio = Mathf.Clamp01(irradiance / maxIrradiance);
            batteryChargePercent += (batteryChargeRate / 60f) * solarRatio * Time.deltaTime;
        }

        // If battery is being used alone
        if (batteryActive && !solarActive)
        {
            batteryChargePercent -= batteryDrainPerSecond * Time.deltaTime;
        }

        // Clamp to 0–100%
        batteryChargePercent = Mathf.Clamp(batteryChargePercent, 0f, 100f);
    }
}
