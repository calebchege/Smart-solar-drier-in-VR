// ============================================================
// FILE: SensorManager.cs (PROPER PHYSICS - INDEPENDENT SYSTEMS)
// ============================================================
using UnityEngine;

public class SensorManager : MonoBehaviour
{
    [Header("Skybox Manager Reference")]
    public SkyboxManager2 skyboxManager;

    [Header("Ambient Environmental Readings")]
    public float ambientTemperature = 20f;
    public float ambientHumidity = 50f;
    public float pyranometer = 300f;

    [Header("Ranges")]
    public float tempMin = 20f, tempMax = 30f;
    public float humMin = 40f, humMax = 70f;
    public float irrMin = 0f, irrMax = 1100f;

    [Header("Smoothing")]
    public float smoothSpeed = 2.0f;
    public float targetChangeInterval = 2f;

    private float targetTemp, targetHum, targetIrr;
    private float timeSinceLastTargetChange = 0f;

    [Header("Water System Sensors")]
    public float flowBeforePump = 0f;
    public float flowBeforeHeatExchanger = 0f;
    public float tankLevelPercent = 100f;

    [Header("Closed-Loop Water Circuit Dynamics")]
    public float flowMomentum = 0f;
    public float flowDecayRate = 0.8f;
    public float maxFlowRate = 5.0f;
    public float minFlowRate = 0.3f;

    [Header("Tank Behavior")]
    public float waterConsumptionRate = 2.5f;
    public float waterRefillRate = 1.5f;
    public float evaporationRate = 0.1f;

    private bool systemIsRunning = false;

    [Header("Electric Heater Control")]
    public bool electricHeaterActive = false;
    public float electricHeaterBoost = 2.5f;        // Stronger heating rate
    public float electricHeaterMaxTemp = 90f;
    [Tooltip("Irradiance threshold to reset electric heater")]
    public float solarResetThreshold = 600f;

    [Header("Solar Water Heater Configuration")]
    [Tooltip("Target temperature for water heaters")]
    public float heaterSetpoint = 65f;
    // --- NEW: Weather-driven SWH "sensor visibility" and targets ---

    // PLC/UI "null" sentinel for inactive heaters
    [Header("SWH Target Approach Speed")]
    [Tooltip("How quickly active heaters move toward their target (°C/sec at full effect).")]
    public float targetApproachRate = 6f; // increase for faster rise (try 6–12)

    [Tooltip("Minimum heating drive during Clear Noon for SWH1 even if solar is low (°C/sec).")]
    public float clearNoonMinBoost = 4f;  // forces SWH1 up (try 3–8)

    private float[] heaterTarget = new float[3];
    private bool[] heaterActive = new bool[3];

    // PLC-friendly "null" sentinel
    private const float INACTIVE_SENTINEL = -1f;

    [Tooltip("Temperature buffer before turning off heater")]
    public float heaterDeadband = 3f;

    [Tooltip("Solar heating effectiveness (W/m² to °C/sec)")]
    public float heaterPassiveGainFactor = 0.002f;  // Increased for faster solar heating

    [Tooltip("Heat loss rate to environment")]
    public float heaterLossRate = 0.08f;            // Reduced loss for better heat retention

    [Tooltip("Current water heater temperatures - INDEPENDENT of ambient")]
    public float[] heaterTemperature = new float[3] { 30f, 30f, 30f };  // Start slightly warm

    [Header("Chamber Sensors")]
    public float[] chamberTemperature = new float[3] { 25f, 25f, 25f };
    public float[] chamberHumidity = new float[3] { 60f, 60f, 60f };

    void Awake()
    {
        if (skyboxManager == null)
            skyboxManager = FindFirstObjectByType<SkyboxManager2>();
    }

    void Start()
    {
        targetTemp = ambientTemperature;
        targetHum = ambientHumidity;
        targetIrr = pyranometer;

        flowMomentum = 0f;
        flowBeforePump = 0f;
        flowBeforeHeatExchanger = 0f;

        // Initialize heaters at starting temperature (not ambient!)
        for (int i = 0; i < heaterTemperature.Length; i++)
        {
            heaterTemperature[i] = 30f;  // Water system has residual heat
        }
    }

    void Update()
    {
        SimulateEnvironmentValues();
        SimulateWaterSystem();
        SimulateSolarHeaters();
    }

    public void SetEnvironmentalRanges(float tMin, float tMax, float hMin, float hMax, float iMin, float iMax)
    {
        tempMin = tMin; tempMax = tMax;
        humMin = hMin; humMax = hMax;
        irrMin = iMin; irrMax = iMax;

        ambientTemperature = tempMin;
        ambientHumidity = humMin;
        pyranometer = irrMin;

        targetTemp = Random.Range(tempMin, tempMax);
        targetHum = Random.Range(humMin, humMax);
        targetIrr = Random.Range(irrMin, irrMax);

        timeSinceLastTargetChange = 0f;

        // ✅ NEW: Snap SWH temps based on selected weather state
        SnapSolarHeaterTempsToWeather();
    }

    // ✅ NEW: Weather selection snap logic
    private void SnapSolarHeaterTempsToWeather()
    {
        if (heaterTemperature == null || heaterTemperature.Length < 3)
            return;

        // Default: all inactive
        for (int i = 0; i < 3; i++)
        {
            heaterActive[i] = false;
            heaterTarget[i] = INACTIVE_SENTINEL;
            heaterTemperature[i] = INACTIVE_SENTINEL;
        }

        string weather = (skyboxManager != null) ? skyboxManager.currentWeather : "None";

        // Targets (20°C steps where applicable)
        if (weather == "Clear Noon")
        {
            heaterActive[0] = true;
            heaterTarget[0] = 90f;
        }
        else if (weather == "Clear Morning" || weather == "Clear Dusk")
        {
            heaterActive[0] = true; heaterTarget[0] = 70f;
            heaterActive[1] = true; heaterTarget[1] = 90f;
        }
        else if (weather == "Cloudy" || weather == "Rainy")
        {
            heaterActive[0] = true; heaterTarget[0] = 50f;
            heaterActive[1] = true; heaterTarget[1] = 70f;
            heaterActive[2] = true; heaterTarget[2] = 90f;
        }
        else if (weather == "Night")
        {
            heaterActive[0] = true; heaterTarget[0] = 30f;
            heaterActive[1] = true; heaterTarget[1] = 50f;
            heaterActive[2] = true; heaterTarget[2] = 70f;
        }

        // Snap initial temps when weather selected:
        // Start a bit below each target (looks realistic + gives visible rise)
        for (int i = 0; i < 3; i++)
        {
            if (!heaterActive[i]) { heaterTemperature[i] = INACTIVE_SENTINEL; continue; }

            float tgt = heaterTarget[i];
            float snap = Mathf.Max(ambientTemperature - 5f, tgt - 20f); // start 20°C below target
            heaterTemperature[i] = snap;
        }

        Debug.Log($"☀️ Weather '{weather}': SWH targets [{heaterTarget[0]}, {heaterTarget[1]}, {heaterTarget[2]}], snapped [{heaterTemperature[0]}, {heaterTemperature[1]}, {heaterTemperature[2]}]");
    }



    void SimulateEnvironmentValues()
    {
        if (skyboxManager == null) return;

        timeSinceLastTargetChange += Time.deltaTime;

        if (timeSinceLastTargetChange >= targetChangeInterval)
        {
            targetTemp = Random.Range(tempMin, tempMax);
            targetHum = Random.Range(humMin, humMax);
            targetIrr = Random.Range(irrMin, irrMax);
            timeSinceLastTargetChange = 0f;
        }

        ambientTemperature = Mathf.Lerp(ambientTemperature, targetTemp, Time.deltaTime * smoothSpeed);
        ambientHumidity = Mathf.Lerp(ambientHumidity, targetHum, Time.deltaTime * smoothSpeed);
        pyranometer = Mathf.Lerp(pyranometer, targetIrr, Time.deltaTime * smoothSpeed);
    }

    void SimulateWaterSystem()
    {
        float dt = Time.deltaTime;

        if (systemIsRunning)
        {
            flowMomentum -= flowDecayRate * dt;
            flowMomentum = Mathf.Max(minFlowRate, flowMomentum);
            flowMomentum = Mathf.Min(flowMomentum, maxFlowRate);

            // Tank dynamics
            if (electricHeaterActive)
            {
                tankLevelPercent -= (waterConsumptionRate / 60f) * dt;
            }

            tankLevelPercent -= (evaporationRate / 60f) * dt;

            if (!electricHeaterActive)
            {
                tankLevelPercent += (waterRefillRate / 60f) * dt;
            }
        }
        else
        {
            flowMomentum -= flowDecayRate * 1.5f * dt;
            flowMomentum = Mathf.Max(0f, flowMomentum);
            tankLevelPercent += (waterRefillRate * 0.5f / 60f) * dt;
        }

        flowBeforePump = flowMomentum;
        flowBeforeHeatExchanger = flowMomentum * 0.95f;
        tankLevelPercent = Mathf.Clamp(tankLevelPercent, 0f, 100f);
    }

    // ============================================================
    // UPDATED: Solar Water Heater Physics
    // - Maintain fixed 10°C steps between SWHs at all times
    // - Heating rate proportional to pyranometer
    // - Heat loss to ambient still applied
    // - Electric heater still applies, but offsets remain fixed
    // ============================================================
    void SimulateSolarHeaters()
    {
        float dt = Time.deltaTime;

        if (heaterTemperature == null || heaterTemperature.Length < 3)
            return;

        string weather = (skyboxManager != null) ? skyboxManager.currentWeather : "None";

        // Electric heater logic (only meaningful when heater 3 is active & target is 90)
        bool heater3Active = heaterActive[2] && heaterTarget[2] > 0f;
        if (heater3Active)
        {
            // If below target and solar is weak, electric heater can assist
            if (pyranometer < solarResetThreshold &&
                heaterTemperature[2] >= 0f &&
                heaterTemperature[2] < (heaterTarget[2] - heaterDeadband))
            {
                electricHeaterActive = true;
            }

            // If solar strong OR reached target, turn it off
            if (pyranometer > solarResetThreshold ||
                (heaterTemperature[2] >= 0f && heaterTemperature[2] >= heaterTarget[2]))
            {
                electricHeaterActive = false;
            }
        }
        else
        {
            electricHeaterActive = false;
        }

        for (int i = 0; i < 3; i++)
        {
            if (!heaterActive[i] || heaterTarget[i] == INACTIVE_SENTINEL)
            {
                heaterTemperature[i] = INACTIVE_SENTINEL;
                continue;
            }

            float temp = heaterTemperature[i];
            float target = heaterTarget[i];

            if (temp < 0f) temp = Mathf.Max(ambientTemperature - 5f, target - 20f);

            // --- Solar contribution (still proportional to pyranometer) ---
            float solarDrive = pyranometer * heaterPassiveGainFactor; // °C/sec-ish

            // --- Target approach drive (makes it rise fast toward target) ---
            // Scales down as it gets close to target
            float error = Mathf.Max(0f, target - temp);
            float approachDrive = Mathf.Min(targetApproachRate, error * 0.6f); // ramps down near target

            // --- Clear Noon FORCE behavior for heater 1 ---
            // Even if solar low, heater 0 gets a minimum boost
            float forced = 0f;
            if (weather == "Clear Noon" && i == 0)
            {
                forced = clearNoonMinBoost; // °C/sec
            }

            // Stage realism (later heaters slightly less solar effective)
            float stageSolarFactor = (i == 0) ? 1.0f : (i == 1) ? 0.9f : 0.8f;

            // Apply heating
            temp += (solarDrive * stageSolarFactor + approachDrive + forced) * dt;

            // Electric heater only to heater 3
            if (i == 2 && electricHeaterActive)
            {
                temp += electricHeaterBoost * dt;
            }

            // Heat loss to ambient
            float dAmb = temp - ambientTemperature;
            if (dAmb > 0f)
            {
                temp -= heaterLossRate * dAmb * dt;
            }

            // Clamp to target "maximum threshold"
            temp = Mathf.Min(temp, target);
            temp = Mathf.Clamp(temp, ambientTemperature - 5f, electricHeaterMaxTemp);

            heaterTemperature[i] = temp;
        }
    }



    // ========================================================
    // ELECTRIC HEATER CONTROL METHODS
    // ========================================================

    public void TurnOnElectricHeater()
    {
        if (!electricHeaterActive)
        {
            electricHeaterActive = true;
            Debug.Log("⚡ Electric Heater ACTIVATED");
        }
    }

    public void TurnOffElectricHeater()
    {
        if (electricHeaterActive)
        {
            electricHeaterActive = false;
            Debug.Log("🔌 Electric Heater DEACTIVATED");
        }
    }

    public void SetElectricHeaterState(bool state)
    {
        electricHeaterActive = state;
    }

    // ========================================================
    // SYSTEM START/STOP HANDLERS
    // ========================================================

    public void OnSystemStart()
    {
        tankLevelPercent = 100f;
        flowMomentum = minFlowRate;
        systemIsRunning = true;

        Debug.Log($"🔵 System Started - Tank: 100%, Heaters: [{heaterTemperature[0]:F1}, {heaterTemperature[1]:F1}, {heaterTemperature[2]:F1}]°C");
    }

    public void OnSystemStop()
    {
        systemIsRunning = false;
        electricHeaterActive = false;
        Debug.Log("🔴 System Stopped");
    }

    [ContextMenu("Debug Current Values")]
    public void DebugCurrentValues()
    {
        float avgWater = (heaterTemperature[0] + heaterTemperature[1] + heaterTemperature[2]) / 3f;
        float avgChamber = (chamberTemperature[0] + chamberTemperature[1] + chamberTemperature[2]) / 3f;

        Debug.Log($"=== SENSOR MANAGER ===");
        Debug.Log($"Ambient: {ambientTemperature:F1}°C, {ambientHumidity:F1}%, Solar: {pyranometer:F0}W/m²");
        Debug.Log($"Water: Flow={flowBeforePump:F2}L/min, Tank={tankLevelPercent:F1}%");
        Debug.Log($"Water Heaters: Avg={avgWater:F1}°C (Setpoint={heaterSetpoint:F1}°C) | Electric={electricHeaterActive}");
        Debug.Log($"Chambers: Avg Temp={avgChamber:F1}°C, Avg Hum={(chamberHumidity[0] + chamberHumidity[1] + chamberHumidity[2]) / 3f:F1}%");
        Debug.Log($"System: {(systemIsRunning ? "RUNNING" : "STOPPED")}");
    }
}
