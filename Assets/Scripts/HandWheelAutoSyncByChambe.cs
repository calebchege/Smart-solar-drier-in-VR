using System;
using UnityEngine;

public class HandWheelAutoSyncByChamberValue : MonoBehaviour
{
    [Serializable]
    public class WheelPair
    {
        public HandWheelGrabRotate wheelA;
        public HandWheelGrabRotate wheelB;
    }

    [Header("Source of Chamber Valve Openings (same ones used by UI)")]
    [Tooltip("Drag your DryerControl here (recommended). It already holds butterflyValves used by the UI.")]
    public DryerControl dryerControl;

    [Header("Wheel pairs per chamber")]
    public WheelPair chamber1;
    public WheelPair chamber2;
    public WheelPair chamber3;

    void Awake()
    {
        if (dryerControl == null)
            dryerControl = FindFirstObjectByType<DryerControl>();
    }

    void Update()
    {
        // AUTO sync only when NOT in manual mode
        if (ManualModeManager.Instance != null && ManualModeManager.Instance.manualModeEnabled)
            return;

        if (dryerControl == null || dryerControl.butterflyValves == null || dryerControl.butterflyValves.Length < 3)
            return;

        // These 3 are exactly what your UI is displaying (chamberValveText[0..2]) :contentReference[oaicite:2]{index=2}
        float ch1 = dryerControl.butterflyValves[0] != null ? dryerControl.butterflyValves[0].currentOpening : 0f;
        float ch2 = dryerControl.butterflyValves[1] != null ? dryerControl.butterflyValves[1].currentOpening : 0f;
        float ch3 = dryerControl.butterflyValves[2] != null ? dryerControl.butterflyValves[2].currentOpening : 0f;

        Apply(chamber1, ch1);
        Apply(chamber2, ch2);
        Apply(chamber3, ch3);
    }

    void Apply(WheelPair pair, float opening01)
    {
        if (pair == null || pair.wheelA == null || pair.wheelB == null) return;

        opening01 = Mathf.Clamp01(opening01);

        pair.wheelA.SetAutoRotationNormalized(opening01);
        pair.wheelB.SetAutoRotationNormalized(opening01);
    }
}
