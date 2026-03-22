// ============================================================
// FILE: SkyboxManager2.cs 
// ============================================================
using UnityEngine;

public class SkyboxManager2 : MonoBehaviour
{
    [Header("References")]
    public SensorManager sensorManager;
    public Light directionalLight;

    [Header("Skyboxes (Materials)")]
    public Material clearMorningSky;
    public Material clearNoonSky;
    public Material clearDuskSky;
    public Material cloudySky;
    public Material rainySky;
    public Material nightSky;

    [Header("Light Intensities")]
    public float morningIntensity = 1.2f;
    public float noonIntensity = 4.0f;
    public float duskIntensity = 0.8f;
    public float cloudyIntensity = 1.0f;
    public float rainyIntensity = 0.6f;
    public float nightIntensity = 0.2f;

    [Header("Transition Settings")]
    public float lightTransitionSpeed = 2.0f;

    [Header("Current State (Read Only)")]
    public string currentWeather = "None";

    private float targetIntensity;
    private Material currentSkybox;

    void Awake()
    {
        if (sensorManager == null)
            sensorManager = FindFirstObjectByType<SensorManager>();

        if (directionalLight == null)
            directionalLight = FindFirstObjectByType<Light>();

        if (sensorManager == null)
            Debug.LogError("❌ SkyboxManager2: SensorManager not found!");

        if (directionalLight == null)
            Debug.LogError("❌ SkyboxManager2: Directional Light not found!");
    }

    void Start()
    {
        SetClearNoon();
    }

    void Update()
    {
        if (directionalLight != null)
        {
            directionalLight.intensity = Mathf.Lerp(
                directionalLight.intensity,
                targetIntensity,
                Time.deltaTime * lightTransitionSpeed
            );
        }
    }

    // -----------------------------------------------------------
    //  WEATHER STATES
    // -----------------------------------------------------------
    public void SetClearMorning()
    {
        if (sensorManager == null) return;

        ApplySkybox(clearMorningSky);
        targetIntensity = morningIntensity;
        currentWeather = "Clear Morning";

        sensorManager.SetEnvironmentalRanges(15, 17, 80, 95, 50, 300);

        Debug.Log("🌤️ Weather Changed: Clear Morning");
    }

    public void SetClearNoon()
    {
        if (sensorManager == null) return;

        ApplySkybox(clearNoonSky);
        targetIntensity = noonIntensity;
        currentWeather = "Clear Noon";

        sensorManager.SetEnvironmentalRanges(25, 30, 40, 65, 800, 1050);

        Debug.Log("☀️ Weather Changed: Clear Noon");
    }

    public void SetClearDusk()
    {
        if (sensorManager == null) return;

        ApplySkybox(clearDuskSky);
        targetIntensity = duskIntensity;
        currentWeather = "Clear Dusk";

        sensorManager.SetEnvironmentalRanges(20, 25, 60, 80, 100, 400);

        Debug.Log("🌅 Weather Changed: Clear Dusk");
    }

    public void SetCloudy()
    {
        if (sensorManager == null) return;

        ApplySkybox(cloudySky);
        targetIntensity = cloudyIntensity;
        currentWeather = "Cloudy";

        sensorManager.SetEnvironmentalRanges(17, 19, 70, 90, 200, 600);

        Debug.Log("☁️ Weather Changed: Cloudy");
    }

    public void SetRainy()
    {
        if (sensorManager == null) return;

        ApplySkybox(rainySky);
        targetIntensity = rainyIntensity;
        currentWeather = "Rainy";

        sensorManager.SetEnvironmentalRanges(15, 17, 85, 100, 50, 300);

        Debug.Log("🌧️ Weather Changed: Rainy");
    }

    public void SetNight()
    {
        if (sensorManager == null) return;

        ApplySkybox(nightSky);
        targetIntensity = nightIntensity;
        currentWeather = "Night";

        sensorManager.SetEnvironmentalRanges(10, 14, 85, 100, 0, 0);

        Debug.Log("🌙 Weather Changed: Night");
    }

    // -----------------------------------------------------------
    //  OPTIMIZED SKYBOX APPLICATION
    // -----------------------------------------------------------
    private void ApplySkybox(Material skyboxMaterial)
    {
        if (skyboxMaterial == null)
        {
            Debug.LogWarning($"⚠️ Skybox material is null for weather: {currentWeather}");
            return;
        }

        if (currentSkybox != skyboxMaterial)
        {
            RenderSettings.skybox = skyboxMaterial;
            currentSkybox = skyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }
    }
}