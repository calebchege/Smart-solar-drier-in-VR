using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Core System References")]
    public SensorManager sensorManager;
    public DryerControl dryerControl;
    public PowerManager powerManager;
    public PLCBridge plcBridge; // ✅ NEW

    [Header("System State Text Boxes")]
    public TMP_Text solarStatusText;
    public TMP_Text batteryStorageText;
    public TMP_Text gridStatusText;
    public TMP_Text airflowStatusText;
    public TMP_Text heatVentPumpStatusText;
    public TMP_Text systemRunningText;
    public TMP_Text modeText;

    [Header("Chamber UI Displays")]
    public TMP_Text[] chamberTempText;
    public TMP_Text[] chamberValveText;

    [Header("Chamber Setpoint Inputs")]
    public TMP_InputField[] chamberSetpointInputs;
    public void DisablePLCModeButton()
    {
        if (plcBridge != null)
            plcBridge.DisablePLCMode();
    }

    void Start()
    {
        if (sensorManager == null) sensorManager = FindFirstObjectByType<SensorManager>();
        if (dryerControl == null) dryerControl = FindFirstObjectByType<DryerControl>();
        if (powerManager == null) powerManager = FindFirstObjectByType<PowerManager>();
        if (plcBridge == null) plcBridge = FindFirstObjectByType<PLCBridge>(); // ✅ NEW

        ValidateUIReferences();

        // Hook setpoint input events
        if (chamberSetpointInputs != null && dryerControl != null)
        {
            for (int i = 0; i < chamberSetpointInputs.Length; i++)
            {
                int index = i;
                if (chamberSetpointInputs[i] != null)
                {
                    chamberSetpointInputs[i].text = dryerControl.chamberTempSetpoints[i].ToString("0");
                    chamberSetpointInputs[i].onEndEdit.AddListener(value => OnSetpointChanged(index, value));
                }
            }
        }
    }

    public void OnSetpointChanged(int chamberIndex, string value)
    {
        if (dryerControl == null) return;

        if (float.TryParse(value, out float newSetpoint))
        {
            dryerControl.chamberTempSetpoints[chamberIndex] = newSetpoint;
        }
    }

    void Update()
    {
        if (dryerControl == null) return;

        if (!dryerControl.systemRunning)
        {
            DisplayIdleState();
            return;
        }

        UpdateMode();
        UpdatePowerStatus();
        UpdateFansAndPumps();
        UpdateChambers();
    }

    void ValidateUIReferences()
    {
        if (chamberTempText == null || chamberTempText.Length < 3)
            Debug.LogWarning("UIManager: chamberTempText needs 3 elements!");
        if (chamberValveText == null || chamberValveText.Length < 3)
            Debug.LogWarning("UIManager: chamberValveText needs 3 elements!");
    }

    void UpdateMode()
    {
        if (modeText == null) return;

        if (dryerControl.plcModeActive)
            modeText.text = "PLC MODE";
        else
            modeText.text = "MANUAL MODE";
    }

    void UpdatePowerStatus()
    {
        if (powerManager == null) return;

        if (solarStatusText != null)
            solarStatusText.text = powerManager.solarActive ? "In Use" : "Out of Use";

        if (batteryStorageText != null)
            batteryStorageText.text = $"{powerManager.batteryChargePercent:0}%";

        if (gridStatusText != null)
            gridStatusText.text = powerManager.gridActive ? "In Use" : "Available";
    }

    void UpdateFansAndPumps()
    {
        if (airflowStatusText != null && dryerControl.exhaustFan != null)
            airflowStatusText.text = dryerControl.exhaustFan.currentSpeed > 0.1f ? "ON" : "OFF";

        if (heatVentPumpStatusText != null && dryerControl.inletFan != null)
            heatVentPumpStatusText.text = dryerControl.inletFan.currentSpeed > 0.1f ? "ON" : "OFF";

        if (systemRunningText != null)
            systemRunningText.text = dryerControl.systemRunning ? "RUNNING" : "STOPPED";
    }

    void UpdateChambers()
    {
        if (sensorManager == null || dryerControl == null) return;
        if (chamberTempText == null || chamberValveText == null) return;
        if (dryerControl.butterflyValves == null) return;

        int chamberCount = Mathf.Min(3,
            Mathf.Min(chamberTempText.Length,
            Mathf.Min(chamberValveText.Length,
            Mathf.Min(sensorManager.chamberTemperature.Length,
                     dryerControl.butterflyValves.Length))));

        for (int i = 0; i < chamberCount; i++)
        {
            if (chamberTempText[i] != null)
                chamberTempText[i].text = $"{sensorManager.chamberTemperature[i]:0}°C";

            if (dryerControl.butterflyValves[i] != null && chamberValveText[i] != null)
            {
                float pct = dryerControl.butterflyValves[i].currentOpening * 100f;
                chamberValveText[i].text = $"{pct:0}%";
            }
        }
    }

    void DisplayIdleState()
    {
        if (systemRunningText != null) systemRunningText.text = "STOPPED";
        if (modeText != null) modeText.text = dryerControl != null && dryerControl.plcModeActive ? "PLC MODE" : "MANUAL MODE";
    }

    // ===========================
    // UI BUTTON FUNCTIONS
    // ===========================

    public void StartSystem()
    {
        if (dryerControl == null) return;
        dryerControl.systemRunning = true;
    }

    public void StopSystem()
    {
        if (dryerControl == null) return;
        dryerControl.systemRunning = false;

        // Safety: stop should force PLC mode OFF as well
        if (plcBridge != null)
            plcBridge.DisablePLCMode();
    }

    // ✅ Hook Auto button to this
    public void AutoMode()
    {
        if (plcBridge != null)
            plcBridge.EnablePLCMode();
    }

    // ✅ Hook Manual button to this
    public void ManualMode()
    {
        if (plcBridge != null)
            plcBridge.DisablePLCMode();
    }
}
