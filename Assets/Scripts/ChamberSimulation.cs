using UnityEngine;

public class ChamberSimulation : MonoBehaviour
{
    [Header("External Data")]
    public SensorManager sensorManager;
    public ButterflyValveController[] butterflyValves;
    public PLCBridge plcBridge;
    public DryerControl dryerControl;

    [Header("Chamber State")]
    public float[] chamberTemperature;
    public float[] chamberHumidity;
    private float[] condensationAccumulated = new float[3];

    [Header("Control Flags")]
    [Tooltip("DryerControl.cs uses this to stop simulation. If true, temps stay at 25C.")]
    public bool freezeChambers = false; // RESTORED AS A BOOL VARIABLE

    [Header("PLC Simulation Constants")]
    public float plcStartupDelay = 3.0f;
    public float kHeat = 0.5f;   // Rate of increase per % valve opening
    public float kLoss = 0.02f;  // Rate of cooling (insulation factor)
    private float[] delayTimers;
    private bool lastPlcState = false;
    [Header("PLC Heating Tuning & Safety")]
    [Tooltip("Multiplier applied to PLC heating rate. Set to 0.25 to reduce heating to a quarter.")]
    public float plcHeatMultiplier = 0.25f;

    [Tooltip("Safety cap for chamber temperature (°C).")]
    public float maxChamberTemp = 75f;

    [Header("Original Physics Properties (Restored)")]
    public float[] chamberThermalMass = new float[3] { 5000f, 5000f, 5000f };
    public float[] chamberVolume = new float[3] { 10f, 10f, 10f };
    public float maxAirExchangeRate = 2.0f;
    public float heatExchangerEfficiency = 0.80f;
    public float heatTransferRate = 12f;
    private float[] chamberHeatContent;

    void Awake()
    {
        // Ensure arrays exist and are length 3 (prevents serialized "empty/0" issues)
        EnsureArraySizes();
    }

    void Start()
    {
        if (dryerControl == null) dryerControl = FindFirstObjectByType<DryerControl>();

        // Prefer the same 3 valves PLCBridge is controlling
        if (dryerControl != null && dryerControl.butterflyValves != null && dryerControl.butterflyValves.Length >= 3)
        {
            butterflyValves = dryerControl.butterflyValves;
        }
        else if (butterflyValves == null || butterflyValves.Length == 0)
        {
            butterflyValves = FindObjectsByType<ButterflyValveController>(FindObjectsSortMode.None);
        }

    }

    /// <summary>
    /// Snaps temps to 25C. Called by PLC mode toggle and Weather changes.
    /// </summary>
    public void ResetChambersToBase()
    {
        EnsureArraySizes();

        for (int i = 0; i < chamberTemperature.Length; i++)
        {
            chamberTemperature[i] = 25f;

            if (delayTimers != null && i < delayTimers.Length)
                delayTimers[i] = plcStartupDelay;

            if (chamberHeatContent != null && i < chamberHeatContent.Length && i < chamberThermalMass.Length)
                chamberHeatContent[i] = 25f * chamberThermalMass[i];
        }

        // IMPORTANT: copy values into SensorManager (don’t replace its array reference)
        SyncToSensorManager();

        Debug.Log("Chamber Logic: Snapped to 25°C.");
    }

    void Update()
    {
        // If plcBridge is missing, treat as manual mode (don’t early-return)
        bool isPlcActive = (plcBridge != null) ? plcBridge.enablePLCControl : false;

        // Transition Detection: If PLC was just turned ON
        if (isPlcActive && !lastPlcState)
        {
            ResetChambersToBase();
        }
        lastPlcState = isPlcActive;

        for (int i = 0; i < chamberTemperature.Length; i++)
        {
            if (freezeChambers)
            {
                chamberTemperature[i] = 25f;
            }
            else if (isPlcActive)
            {
                RunPLCModeLogic(i);
            }
            else
            {
                RunManualModeLogic(i);
            }
        }

        // Keep SensorManager values updated for UI + PLC writes
        SyncToSensorManager();
    }

    private void RunPLCModeLogic(int i)
    {
        // Safety checks
        if (sensorManager == null || butterflyValves == null || i >= butterflyValves.Length || butterflyValves[i] == null)
            return;

        // 1. Handle the 25C Start-up Delay
        if (delayTimers != null && i < delayTimers.Length && delayTimers[i] > 0f)
        {
            chamberTemperature[i] = 25f;
            delayTimers[i] -= Time.deltaTime;
            return;
        }

        // 2. Apply your Equation: X = (kHeat * Y) - kLoss * (T_curr - T_amb)
        float Y = butterflyValves[i].currentOpening * 100f; // 0 to 100 (%)
        float T_curr = chamberTemperature[i];
        float T_amb = sensorManager.ambientTemperature;

        float X = ((kHeat * plcHeatMultiplier) * Y) - (kLoss * (T_curr - T_amb));
        chamberTemperature[i] += X * Time.deltaTime;
        chamberTemperature[i] = Mathf.Min(chamberTemperature[i], maxChamberTemp);
    }

    private void RunManualModeLogic(int i)
    {
        if (sensorManager == null) return;

        chamberTemperature[i] = Mathf.Lerp(
            chamberTemperature[i],
            sensorManager.ambientTemperature,
            Time.deltaTime * 0.1f
        );

        if (chamberHeatContent != null && i < chamberHeatContent.Length && i < chamberThermalMass.Length)
            chamberHeatContent[i] = chamberTemperature[i] * chamberThermalMass[i];
    }

    private void SyncToSensorManager()
    {
        if (sensorManager == null) return;

        // Ensure SensorManager array exists and correct size.
        // (We don’t know your SensorManager code here, so we guard anyway.)
        if (sensorManager.chamberTemperature == null || sensorManager.chamberTemperature.Length != chamberTemperature.Length)
        {
            sensorManager.chamberTemperature = new float[chamberTemperature.Length];
        }

        for (int i = 0; i < chamberTemperature.Length; i++)
        {
            sensorManager.chamberTemperature[i] = chamberTemperature[i];
        }
    }

    private void EnsureArraySizes()
    {
        // Force 3 chambers by default (your ecosystem assumes 3)
        const int N = 3;

        if (chamberTemperature == null || chamberTemperature.Length != N)
            chamberTemperature = new float[N] { 25f, 25f, 25f };

        if (chamberHumidity == null || chamberHumidity.Length != N)
            chamberHumidity = new float[N] { 50f, 50f, 50f };
    }

    // --- HELPER METHODS TO PREVENT ERRORS IN OTHER SCRIPTS ---
    public void ResetToAmbient() { ResetChambersToBase(); }
    public float[] GetCondensationAmount() { return condensationAccumulated; }
    public void ClearCondensation(int index) { if (index < 3) condensationAccumulated[index] = 0; }
}
