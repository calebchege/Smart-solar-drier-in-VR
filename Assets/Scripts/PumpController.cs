// ============================================================
// FILE: PumpController.cs (MOMENTUM-BASED CLOSED LOOP)
// ============================================================
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PumpController : MonoBehaviour
{
    [Header("Pump Runtime State")]
    public bool pumpRunning = false;

    [Header("Pump Performance - Momentum-Based")]
    [Tooltip("How much flow momentum the pump adds per second")]
    public float pumpBoostRate = 2.5f;        // L/min per second acceleration

    [Tooltip("Maximum flow boost from pump")]
    public float maxPumpBoost = 3.5f;         // Maximum additional flow

    [Tooltip("Current pump contribution to flow")]
    public float currentPumpContribution = 0f; // Smoothly builds up

    [Header("External Data Reference")]
    public SensorManager sensorManager;

    [Header("Audio")]
    private AudioSource pumpAudio;

    void Start()
    {
        pumpAudio = GetComponent<AudioSource>();

        if (sensorManager == null)
            sensorManager = FindFirstObjectByType<SensorManager>();
    }

    /// <summary>
    /// Unified pump ON/OFF method used by DryerControl
    /// </summary>
    public void SetState(bool on)
    {
        pumpRunning = on;

        if (pumpAudio != null)
            pumpAudio.mute = !on;
    }

    void Update()
    {
        if (sensorManager == null)
            return;

        if (pumpRunning)
            BoostFlowMomentum();
        else
            DecayPumpContribution();
    }

    /// <summary>
    /// Pump adds momentum to the water circuit
    /// </summary>
    void BoostFlowMomentum()
    {
        float dt = Time.deltaTime;

        // Pump contribution builds up over time (acceleration)
        currentPumpContribution += pumpBoostRate * dt;
        currentPumpContribution = Mathf.Min(currentPumpContribution, maxPumpBoost);

        // Add pump contribution to the flow momentum in SensorManager
        // This increases the kinetic energy in the closed loop
        sensorManager.flowMomentum += currentPumpContribution * dt;

        // Clamp to maximum flow rate
        sensorManager.flowMomentum = Mathf.Min(sensorManager.flowMomentum, sensorManager.maxFlowRate);
    }

    /// <summary>
    /// When pump stops, its contribution decays
    /// </summary>
    void DecayPumpContribution()
    {
        float dt = Time.deltaTime;

        // Pump contribution decays quickly when off
        currentPumpContribution -= pumpBoostRate * 2f * dt;
        currentPumpContribution = Mathf.Max(0f, currentPumpContribution);

        // Flow momentum continues from inertia (handled in SensorManager)
    }
}