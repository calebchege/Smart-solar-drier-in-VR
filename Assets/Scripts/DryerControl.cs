// ============================================================
// FILE: DryerControl.cs (PROPER HEATER & VALVE LOGIC)
// ============================================================
using UnityEngine;

public class DryerControl : MonoBehaviour
{
    [Header("System State Control")]
    public bool systemRunning = false;
    public bool plcModeActive = false;

    [Header("External Managers")]
    public SensorManager sensorManager;

    [Header("Fans")]
    public FanController inletFan;
    public FanController exhaustFan;
    public float fanSpeed = 1f;

    [Header("Pump Control")]
    public PumpController pumpController;
    public float flowThreshold = 3.0f;
    public float tankMinLevel = 15f;

    [Header("Electric Heater Logic - SIMPLE")]
    [Tooltip("Heater turns ON when water temp below this")]
    public float waterTempThreshold = 60f;

    [Tooltip("Additional: Turn ON if solar is weak")]
    public float solarThreshold = 300f;
    
    [Header("Electric Heater Manual Control")]
    public bool manualHeaterControl = false;
    public bool manualHeaterState = false;

    [Header("Chamber Control - Individual Setpoints")]
    public float[] chamberTempSetpoints = new float[3] { 45f, 45f, 45f };
    public float[] chamberHumSetpoints = new float[3] { 15f, 15f, 15f };

    [Header("PI Control Tuning - AGGRESSIVE")]
    [Tooltip("How strongly valve responds to error")]
    public float proportionalGain = 1.2f;

    [Tooltip("How fast accumulated error is corrected")]
    public float integralGain = 0.15f;

    [Tooltip("Derivative Gain (Kd)")]
    public float derivativeGain = 0.1f;

    [Tooltip("Max integral wind-up protection")]
    public float integralMax = 20f;

    [Tooltip("Error margin where no action needed")]
    public float tempDeadband = 1.0f;

    [Tooltip("Humidity error margin")]
    public float humidityDeadband = 5f;

    private float[] integralError = new float[3];
    private float[] prevError = new float[3]; // For Derivative
    private float[] lastValveCommand = new float[3];

    [Header("Chamber Simulation Reference")]
    public ChamberSimulation chamberSimulation;

    private bool wasRunning = false;
    public ButterflyValveController[] butterflyValves;
    private float[] cachedValveOpenings;

    public bool PumpRunning => pumpController != null && pumpController.pumpRunning;
    public bool FansRunning => inletFan != null && inletFan.currentSpeed > 0f;

    public float[] ValveOpenings
    {
        get
        {
            if (butterflyValves == null || butterflyValves.Length == 0)
                return new float[0];

            if (cachedValveOpenings == null || cachedValveOpenings.Length != butterflyValves.Length)
                cachedValveOpenings = new float[butterflyValves.Length];

            for (int i = 0; i < butterflyValves.Length; i++)
                cachedValveOpenings[i] = butterflyValves[i] != null ? butterflyValves[i].currentOpening : 0f;

            return cachedValveOpenings;
        }
    }

    public float[] ChamberTemps => sensorManager != null ? sensorManager.chamberTemperature : new float[0];
    public float[] ChamberHumidity => sensorManager != null ? sensorManager.chamberHumidity : new float[0];
    public bool IsInAutoMode => !systemRunning || plcModeActive;
    public bool SystemActive => systemRunning && !plcModeActive;

    void Start()
    {
        int numChambers = butterflyValves != null ? butterflyValves.Length : 3;

        prevError = new float[numChambers];
        integralError = new float[numChambers];
        lastValveCommand = new float[numChambers];
        cachedValveOpenings = new float[numChambers];

        if (chamberTempSetpoints.Length != numChambers)
            System.Array.Resize(ref chamberTempSetpoints, numChambers);
        if (chamberHumSetpoints.Length != numChambers)
            System.Array.Resize(ref chamberHumSetpoints, numChambers);

        ValidateReferences();
    }

    void Update()
    {
        if (!AreEssentialComponentsValid())
            return;

        if (systemRunning && !wasRunning)
            OnSystemStarted();
        else if (!systemRunning && wasRunning)
            OnSystemStopped();

        wasRunning = systemRunning;

        if (plcModeActive)
        {
            if (chamberSimulation != null)
                chamberSimulation.freezeChambers = false; // ✅ ALWAYS OFF

            return;
        }


        RunFans();
        ControlPump();
        ControlElectricHeater();
        ControlButterflyValves();
    }

    private void OnSystemStarted()
    {
        if (sensorManager != null)
            sensorManager.OnSystemStart();

        // Start with valves CLOSED
        if (butterflyValves != null)
        {
            foreach (var valve in butterflyValves)
            {
                if (valve != null)
                    valve.SetOpening(0f);
            }
        }

        // Reset PI states
        for (int i = 0; i < integralError.Length; i++)
        {
            integralError[i] = 0f;
            prevError[i] = 0f;
            lastValveCommand[i] = 0f;
        }

        Debug.Log("🟢 System STARTED - Valves closed, heating up...");
    }

    private void OnSystemStopped()
    {
        if (sensorManager != null)
            sensorManager.OnSystemStop();

        Debug.Log("🔴 System STOPPED");
    }

    private void ValidateReferences()
    {
        if (sensorManager == null) Debug.LogWarning("DryerControl: SensorManager missing!");
        if (inletFan == null) Debug.LogWarning("DryerControl: InletFan missing!");
        if (exhaustFan == null) Debug.LogWarning("DryerControl: ExhaustFan missing!");
        if (pumpController == null) Debug.LogWarning("DryerControl: PumpController missing!");
        if (chamberSimulation == null) Debug.LogWarning("DryerControl: ChamberSimulation missing!");
        if (butterflyValves == null || butterflyValves.Length == 0)
            Debug.LogWarning("DryerControl: ButterflyValves missing!");
    }

    private bool AreEssentialComponentsValid()
    {
        return sensorManager != null && inletFan != null && exhaustFan != null &&
               pumpController != null && chamberSimulation != null &&
               butterflyValves != null && butterflyValves.Length > 0;
    }

    private void StopDryer()
    {
        if (inletFan != null) inletFan.SetSpeed(0f);
        if (exhaustFan != null) exhaustFan.SetSpeed(0f);
        if (pumpController != null) pumpController.SetState(false);
        if (sensorManager != null && !plcModeActive) sensorManager.TurnOffElectricHeater();
        if (chamberSimulation != null) chamberSimulation.freezeChambers = true;

        if (butterflyValves != null)
        {
            foreach (var valve in butterflyValves)
            {
                if (valve != null) valve.SetOpening(0f);
            }
        }
    }

    private void RunFans()
    {
        if (inletFan != null) inletFan.SetSpeed(fanSpeed);
        if (exhaustFan != null) exhaustFan.SetSpeed(fanSpeed);
        if (chamberSimulation != null) chamberSimulation.freezeChambers = false;
    }

    private void ControlPump()
    {
        if (sensorManager == null || pumpController == null) return;

        bool pumpShouldRun = (sensorManager.tankLevelPercent > tankMinLevel) &&
                            (sensorManager.flowBeforePump < flowThreshold);

        pumpController.SetState(pumpShouldRun);
    }

    // ============================================================
    // SIMPLIFIED HEATER LOGIC: If water temp low OR solar weak → ON
    // ============================================================
    private void ControlElectricHeater()
    {
        if (sensorManager == null || sensorManager.heaterTemperature == null) return;
        
        if (manualHeaterControl)
        {
            sensorManager.SetElectricHeaterState(manualHeaterState);
            return;
        }

        float avgWaterTemp = (sensorManager.heaterTemperature[0] +
                             sensorManager.heaterTemperature[1] +
                             sensorManager.heaterTemperature[2]) / 3f;

        // Simple logic: Turn ON if water is cold OR solar is weak
        bool needHeater = (avgWaterTemp < waterTempThreshold) ||
                         (sensorManager.pyranometer < solarThreshold);

        if (needHeater)
            sensorManager.TurnOnElectricHeater();
        else
            sensorManager.TurnOffElectricHeater();
    }

    // ============================================================
    // PID CONTROL: Valves respond to Chamber Temperature
    // ============================================================
    private void ControlButterflyValves()
    {
        if (sensorManager == null || butterflyValves == null) return;
        if (sensorManager.chamberTemperature == null || sensorManager.chamberHumidity == null) return;

        float dt = Time.deltaTime;

        for (int i = 0; i < butterflyValves.Length; i++)
        {
            if (butterflyValves[i] == null) continue;
            if (i >= sensorManager.chamberTemperature.Length || i >= chamberTempSetpoints.Length) continue;

            // 1. GET INPUTS
            float currentTemp = sensorManager.chamberTemperature[i];
            float setpoint = chamberTempSetpoints[i];

            // 2. CALCULATE ERROR (Setpoint - ProcessVariable)
            float error = setpoint - currentTemp;

            // 3. PID CALCULATIONS
            // Proportional Term
            float pTerm = proportionalGain * error;

            // Integral Term (Accumulate error over time)
            // Only accumulate if we are not already saturated or if error opposes saturation
            integralError[i] += error * dt;
            integralError[i] = Mathf.Clamp(integralError[i], -integralMax, integralMax);
            float iTerm = integralGain * integralError[i];

            // Derivative Term (Rate of change of error)
            float dTerm = derivativeGain * ((error - prevError[i]) / dt);
            prevError[i] = error; // Store for next frame

            // 4. COMBINE OUTPUT
            float pidOutput = pTerm + iTerm + dTerm;

            // 5. LOGIC: "Once we reach the set point the valve is closed"
            // If temperature is above setpoint (error <= 0), we force close.
            // We also reset integral to prevent wind-up keeping it open.
            if (currentTemp >= setpoint)
            {
                pidOutput = 0f;
                integralError[i] = 0f; // Reset integral to ensure immediate closing
            }

            // 6. NORMALIZE TO 0-1 (Valve Opening)
            // PID output is usually in "control units", here we map it to 0-1.
            // Assuming gain values are tuned such that output roughly maps to 0-1 or higher.
            // We clamp it to ensure valid range.
            float targetOpening = Mathf.Clamp01(pidOutput);

            // 7. APPLY TO VALVE (with smoothing)
            float currentOpening = butterflyValves[i].currentOpening;
            float valveSpeed = 2.0f; // Speed of actuator
            float newOpening = Mathf.MoveTowards(currentOpening, targetOpening, valveSpeed * dt);

            butterflyValves[i].SetOpening(newOpening);
            lastValveCommand[i] = newOpening;
        }
    }
}
