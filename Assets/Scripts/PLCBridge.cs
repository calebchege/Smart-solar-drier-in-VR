using UnityEngine;

/// <summary>
/// Bridge between SimplePLCMapper and Unity systems
/// FIXED: Corrected method calls and array handling
/// </summary>
public class PLCBridge : MonoBehaviour
{
    [Header("System References")]
    public SimplePLCMapper plcMapper;
    public SensorManager sensorManager;
    public DryerControl dryerControl;
    public PowerManager powerManager;

    [Header("PLC Mode Control")]
    [Tooltip("When enabled, PLC controls the system. When disabled, Unity sends data to PLC for monitoring")]
    public bool enablePLCControl = false;
    [Tooltip("If true, PLC mode is forced OFF regardless of enablePLCControl (useful for UI Manual Mode).")]
    public bool forceDisablePLCMode = false;


    [Header("Update Rate")]
    [Tooltip("How often to sync with PLC (seconds)")]
    public float updateInterval = 0.1f;
    private float updateTimer = 0f;
    private bool lastSystemRunning = false;
    
    void Start()
    {
        // Auto-find references if not assigned
        if (plcMapper == null)
            plcMapper = FindFirstObjectByType<SimplePLCMapper>();

        if (sensorManager == null)
            sensorManager = FindFirstObjectByType<SensorManager>();

        if (dryerControl == null)
            dryerControl = FindFirstObjectByType<DryerControl>();

        if (powerManager == null)
            powerManager = FindFirstObjectByType<PowerManager>();

        // Validate critical references
        if (plcMapper == null)
            Debug.LogError("PLCBridge: SimplePLCMapper not found!");
        if (sensorManager == null)
            Debug.LogError("PLCBridge: SensorManager not found!");
        if (dryerControl == null)
            Debug.LogError("PLCBridge: DryerControl not found!");
    }

    void Update()
    {
        // Manual override to force PLC mode OFF
        if (forceDisablePLCMode && enablePLCControl)
            DisablePLCMode();

        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval) return;
        updateTimer = 0f;

        // ✅ HARD GATE: do nothing if PLC is not connected
        if (plcMapper == null || !plcMapper.IsConnected())
            return;

        if (enablePLCControl)
        {
            ReadFromPLCToUnity();
            WriteSensorsToPLC_Only();
        }
        else
        {
            WriteFromUnityToPLC();
        }
    }



    public void SetForceDisablePLCMode(bool disabled)
    {
        forceDisablePLCMode = disabled;

        // If user forces disable, immediately turn PLC control off
        if (forceDisablePLCMode)
            DisablePLCMode();
    }

    public bool GetForceDisablePLCMode()
    {
        return forceDisablePLCMode;
    }

    // ========================================================
    // PLC → UNITY (PLC controls the system)
    // ========================================================
    void ReadFromPLCToUnity()
    {
        if (dryerControl == null || sensorManager == null) return;

        // ============================================
        // SYSTEM STATE CONTROL
        // ============================================

        // Set PLC mode active in DryerControl
        dryerControl.plcModeActive = true;

        // Start/Stop system based on PLC buttons
        if (plcMapper.Start_btn && !dryerControl.systemRunning)
        {
            dryerControl.systemRunning = true;
            Debug.Log("🟢 System STARTED by PLC");
        }

        if (plcMapper.Stop_btn && dryerControl.systemRunning)
        {
            dryerControl.systemRunning = false;
            Debug.Log("🔴 System STOPPED by PLC");
        }

        // ============================================
        // FANS CONTROL
        // ============================================
        float fanSpeed = plcMapper.Vent_Fans ? 1f : 0f;

        if (dryerControl.inletFan != null)
            dryerControl.inletFan.SetSpeed(fanSpeed);

        if (dryerControl.exhaustFan != null)
            dryerControl.exhaustFan.SetSpeed(fanSpeed);

        // ============================================
        // PUMP CONTROL
        // ============================================
        if (dryerControl.pumpController != null)
            dryerControl.pumpController.SetState(plcMapper.W_Pump);

        // ============================================
        // ELECTRIC HEATER CONTROL (FIXED METHOD CALL)
        // ============================================
        sensorManager.SetElectricHeaterState(plcMapper.El_Heater_Switch);

        // ============================================
        // BUTTERFLY VALVES CONTROL
        // ============================================
        if (dryerControl.butterflyValves != null && dryerControl.butterflyValves.Length >= 3)
        {
            if (dryerControl.butterflyValves[0] != null)
                dryerControl.butterflyValves[0].SetOpening(plcMapper.H_Air_vent_valve_1 / 100f);

            if (dryerControl.butterflyValves[1] != null)
                dryerControl.butterflyValves[1].SetOpening(plcMapper.H_Air_vent_valve_2 / 100f);

            if (dryerControl.butterflyValves[2] != null)
                dryerControl.butterflyValves[2].SetOpening(plcMapper.H_Air_vent_valve_3 / 100f);
        }

        // ============================================
        // SETPOINTS - FIXED TO USE ARRAY
        // ============================================
        // PLC can override individual chamber setpoints
        if (dryerControl.chamberTempSetpoints != null && dryerControl.chamberTempSetpoints.Length >= 3)
        {
            if (plcMapper.Setpoint_1 > 0 && plcMapper.Setpoint_1 < 100)
                dryerControl.chamberTempSetpoints[0] = plcMapper.Setpoint_1;

            if (plcMapper.Setpoint_2 > 0 && plcMapper.Setpoint_2 < 100)
                dryerControl.chamberTempSetpoints[1] = plcMapper.Setpoint_2;

            if (plcMapper.Setpoint_3 > 0 && plcMapper.Setpoint_3 < 100)
                dryerControl.chamberTempSetpoints[2] = plcMapper.Setpoint_3;
        }
    }

    // ========================================================
    // UNITY → PLC (Unity sends monitoring data to PLC)
    // ========================================================
    void WriteFromUnityToPLC()
    {
        if (plcMapper == null || !plcMapper.IsConnected()) return;

        if (sensorManager == null || dryerControl == null) return;

        // Disable PLC mode in DryerControl (Unity is in control)
        dryerControl.plcModeActive = false;

        // Generate one-shot Start/Stop pulses when system state changes
        bool startPulse = (!lastSystemRunning && dryerControl.systemRunning);
        bool stopPulse = (lastSystemRunning && !dryerControl.systemRunning);
        lastSystemRunning = dryerControl.systemRunning;

        // ============================================
        // HEATER TEMPERATURE SENSORS (to PLC)
        // ============================================
        plcMapper.Temp_Sense_1 = Mathf.RoundToInt(sensorManager.heaterTemperature[0]);
        plcMapper.Temp_Sense_2 = Mathf.RoundToInt(sensorManager.heaterTemperature[1]);
        plcMapper.Temp_Sense_3 = Mathf.RoundToInt(sensorManager.heaterTemperature[2]);

        // ============================================
        // CHAMBER TEMPERATURES (to PLC)
        // ============================================
        plcMapper.Chamber_1_Temp = Mathf.RoundToInt(sensorManager.chamberTemperature[0]);
        plcMapper.Chamber_2_Temp = Mathf.RoundToInt(sensorManager.chamberTemperature[1]);
        plcMapper.Chamber_3_Temp = Mathf.RoundToInt(sensorManager.chamberTemperature[2]);

        // ============================================
        // VALVE POSITIONS (to PLC)
        // ============================================
        if (dryerControl.butterflyValves != null && dryerControl.butterflyValves.Length >= 3)
        {
            plcMapper.H_Air_vent_valve_1 = Mathf.RoundToInt(dryerControl.butterflyValves[0].currentOpening * 100f);
            plcMapper.H_Air_vent_valve_2 = Mathf.RoundToInt(dryerControl.butterflyValves[1].currentOpening * 100f);
            plcMapper.H_Air_vent_valve_3 = Mathf.RoundToInt(dryerControl.butterflyValves[2].currentOpening * 100f);
        }

        // ============================================
        // SYSTEM STATE (to PLC)
        // ============================================
        plcMapper.Vent_Fans = dryerControl.FansRunning;
        plcMapper.W_Pump = dryerControl.PumpRunning;
        plcMapper.El_Heater_Switch = sensorManager.electricHeaterActive;

        // ============================================
        // ENVIRONMENTAL SENSORS (to PLC)
        // ============================================
        plcMapper.photoirradiance_sensor = Mathf.RoundToInt(sensorManager.pyranometer);

        // ============================================
        // SETPOINTS (to PLC) - FIXED TO USE ARRAY
        // ============================================
        if (dryerControl.chamberTempSetpoints != null && dryerControl.chamberTempSetpoints.Length >= 3)
        {
            plcMapper.Setpoint_1 = Mathf.RoundToInt(dryerControl.chamberTempSetpoints[0]);
            plcMapper.Setpoint_2 = Mathf.RoundToInt(dryerControl.chamberTempSetpoints[1]);
            plcMapper.Setpoint_3 = Mathf.RoundToInt(dryerControl.chamberTempSetpoints[2]);
        }

        // ============================================
        // CONTROL BUTTONS (momentary pulses)
        // ============================================
        plcMapper.Start_btn = startPulse;
        plcMapper.Stop_btn = stopPulse;

        // ============================================
        // SEND ALL DATA TO PLC
        // ============================================
        plcMapper.UpdatePLCFromUnity();

        // Reset pulses immediately after write
        plcMapper.Start_btn = false;
        plcMapper.Stop_btn = false;
    }

    void WriteSensorsToPLC_Only()
    {
        if (plcMapper == null || !plcMapper.IsConnected()) return;

        if (sensorManager == null || plcMapper == null) return;

        // Push SensorManager → plcMapper variables
        plcMapper.Temp_Sense_1 = Mathf.RoundToInt(sensorManager.heaterTemperature[0]);
        plcMapper.Temp_Sense_2 = Mathf.RoundToInt(sensorManager.heaterTemperature[1]);
        plcMapper.Temp_Sense_3 = Mathf.RoundToInt(sensorManager.heaterTemperature[2]);

        plcMapper.Chamber_1_Temp = Mathf.RoundToInt(sensorManager.chamberTemperature[0]);
        plcMapper.Chamber_2_Temp = Mathf.RoundToInt(sensorManager.chamberTemperature[1]);
        plcMapper.Chamber_3_Temp = Mathf.RoundToInt(sensorManager.chamberTemperature[2]);

        plcMapper.photoirradiance_sensor = Mathf.RoundToInt(sensorManager.pyranometer);

        // IMPORTANT: do NOT overwrite PLC-controlled fields in PLC mode
        // So we do NOT touch: Vent_Fans, W_Pump, El_Heater_Switch, H_Air_vent_valve_*, Setpoint_*, Start_btn, Stop_btn

        // Write only the sensor words directly
        plcMapper.WriteInt("DB9.DBW0", plcMapper.Temp_Sense_1);
        plcMapper.WriteInt("DB9.DBW2", plcMapper.Temp_Sense_2);
        plcMapper.WriteInt("DB9.DBW4", plcMapper.Temp_Sense_3);

        plcMapper.WriteInt("DB9.DBW22", plcMapper.Chamber_1_Temp);
        plcMapper.WriteInt("DB9.DBW24", plcMapper.Chamber_2_Temp);
        plcMapper.WriteInt("DB9.DBW26", plcMapper.Chamber_3_Temp);

        plcMapper.WriteInt("DB9.DBW30", plcMapper.photoirradiance_sensor);
    }

    // ========================================================
    // PUBLIC CONTROL METHODS (for UI buttons)
    // ========================================================

    public void EnablePLCMode()
    {
        enablePLCControl = true;
        if (dryerControl != null)
            dryerControl.plcModeActive = true;
        Debug.Log("🔵 PLC Mode ENABLED - PLC controls the system");
    }

    public void DisablePLCMode()
    {
        enablePLCControl = false;
        if (dryerControl != null)
            dryerControl.plcModeActive = false;
        Debug.Log("🟢 Manual Mode ENABLED - Unity controls the system, PLC monitors");
    }

    public void TogglePLCMode()
    {
        if (enablePLCControl)
            DisablePLCMode();
        else
            EnablePLCMode();
    }

    public bool IsPLCModeActive()
    {
        return enablePLCControl && plcMapper != null && plcMapper.IsConnected();
    }

    public string GetCurrentMode()
    {
        if (plcMapper == null || !plcMapper.IsConnected())
            return "PLC DISCONNECTED";

        return enablePLCControl ? "PLC MODE" : "MANUAL MODE";
    }

    [ContextMenu("Debug PLC Bridge Status")]
    public void DebugStatus()
    {
        Debug.Log("=== PLC BRIDGE STATUS ===");
        Debug.Log($"PLC Connected: {(plcMapper != null && plcMapper.IsConnected())}");
        Debug.Log($"Mode: {GetCurrentMode()}");
        Debug.Log($"System Running: {(dryerControl != null ? dryerControl.systemRunning : false)}");
        Debug.Log($"Electric Heater: {(sensorManager != null ? sensorManager.electricHeaterActive : false)}");

        if (dryerControl != null && dryerControl.chamberTempSetpoints != null)
        {
            Debug.Log($"Chamber Setpoints: [{dryerControl.chamberTempSetpoints[0]:F0}, " +
                     $"{dryerControl.chamberTempSetpoints[1]:F0}, " +
                     $"{dryerControl.chamberTempSetpoints[2]:F0}]°C");
        }
    }
}
