using TMPro;
using UnityEngine;

public class OverviewUpdater : MonoBehaviour
{
    public TMP_Text solarInput;
    public TMP_Text batteryStorage;
    public TMP_Text gridStatus;
    public TMP_Text airflow;
    public TMP_Text ventPump;
    public TMP_Text waterPump;

    void Start()
    {
        // Example initial values
        UpdateUI(1, 87, true, "ON (120 RPM)", true, false);
    }

    public void UpdateUI(int solarkW, int batteryPercent, bool gridAvailable, string fan, bool ventpumpOn, bool pumpOn)
    {
        if (solarInput) solarInput.text = solarkW + " kW";
        if (batteryStorage) batteryStorage.text = batteryPercent + "%";
        if (gridStatus) gridStatus.text = gridAvailable ? "AVAILABLE" : "UNAVAILABLE";
        if (airflow) airflow.text = fan;
        if (ventPump) ventPump.text = ventpumpOn ? "ON" : "OFF";
        if (waterPump) waterPump.text = pumpOn ? "ON" : "OFF";
    }
}
