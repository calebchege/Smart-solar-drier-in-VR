using UnityEngine;

public class ManualModeManager : MonoBehaviour
{
    public static ManualModeManager Instance;

    public bool manualModeEnabled = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ToggleManualMode()
    {
        manualModeEnabled = !manualModeEnabled;
        Debug.Log("Manual Mode is now " + (manualModeEnabled ? "ENABLED" : "DISABLED"));
    }
}