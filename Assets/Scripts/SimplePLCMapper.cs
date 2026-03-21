using UnityEngine;
using S7.Net;
using System;

public class SimplePLCMapper : MonoBehaviour
{
    [Header("PLC Settings")]
    public string plcIP = "192.168.0.1"; // PLCSIM Advanced IP
    private Plc plc;

    [Header("Inputs from Unity / HMI")]
    public int Temp_Sense_1;
    public int Temp_Sense_2;
    public int Temp_Sense_3;

    public bool Start_btn; // Renamed from Start to avoid Unity method conflict
    public bool Stop_btn;  // Renamed from Stop to avoid Unity method conflict

    public int Chamber_1_Temp;
    public int Chamber_2_Temp;
    public int Chamber_3_Temp;

    public int Setpoint_1;
    public int Setpoint_2;
    public int Setpoint_3;

    public bool Valv_1_1;
    public bool Valv_1_2;
    public bool Valv_2_1;
    public bool Valv_2_2;
    public bool Valv_3;

    public bool El_Heater_Switch;
    public bool W_Pump;
    public bool Vent_Fans;

    public int H_Air_vent_valve_1;
    public int H_Air_vent_valve_2;
    public int H_Air_vent_valve_3;

    public int photoirradiance_sensor;

    [Header("Connection Status")]
    public bool PLCConnected = false;

    // Singleton instance
    public static SimplePLCMapper Instance { get; private set; }

    private float reconnectTimer = 0f;
    public float reconnectInterval = 5f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        ConnectToPLC();
    }

    void Update()
    {
        // Update live status (this is what your inspector checkbox should reflect)
        PLCConnected = (plc != null && plc.IsConnected);

        if (!PLCConnected)
        {
            reconnectTimer += Time.deltaTime;
            if (reconnectTimer >= reconnectInterval)
            {
                reconnectTimer = 0f;
                ConnectToPLC();
            }
            return;
        }

        // If connected, keep reading PLC
        ReadPLC();
    }

    void ConnectToPLC()
    {
        try
        {
            // Close old connection if needed
            if (plc != null)
            {
                try { if (plc.IsConnected) plc.Close(); } catch { }
            }

            plc = new Plc(CpuType.S71500, plcIP, 0, 1);
            plc.Open();

            PLCConnected = plc.IsConnected;

            if (PLCConnected)
                Debug.Log($"PLC Connected Successfully! IP={plcIP}");
            else
                Debug.LogWarning($"PLC connection attempted but not verified. IP={plcIP}");
        }
        catch (Exception ex)
        {
            PLCConnected = false;
            Debug.LogError($"PLC Connection Failed (IP={plcIP}): {ex.Message}");
        }
    }

    // ---------------------- READ PLC VALUES ----------------------
    void ReadPLC()
    {
        try
        {
            // DIGITAL
            Valv_1_1 = ReadBool("DB9.DBX6.0");
            Valv_1_2 = ReadBool("DB9.DBX6.1");
            Valv_2_1 = ReadBool("DB9.DBX6.2");
            Valv_2_2 = ReadBool("DB9.DBX6.3");
            Valv_3 = ReadBool("DB9.DBX6.4");

            El_Heater_Switch = ReadBool("DB9.DBX6.5");
            W_Pump = ReadBool("DB9.DBX28.0");
            Vent_Fans = ReadBool("DB9.DBX20.0");

            Start_btn = ReadBool("DB9.DBX32.0");
            Stop_btn = ReadBool("DB9.DBX32.1");

            // ANALOG (WORDS)
            H_Air_vent_valve_1 = ReadInt("DB9.DBW16");
            H_Air_vent_valve_2 = ReadInt("DB9.DBW18");
            H_Air_vent_valve_3 = ReadInt("DB9.DBW14");

            Temp_Sense_1 = ReadInt("DB9.DBW0");
            Temp_Sense_2 = ReadInt("DB9.DBW2");
            Temp_Sense_3 = ReadInt("DB9.DBW4");

            Chamber_1_Temp = ReadInt("DB9.DBW22");
            Chamber_2_Temp = ReadInt("DB9.DBW24");
            Chamber_3_Temp = ReadInt("DB9.DBW26");

            Setpoint_1 = ReadInt("DB9.DBW8");
            Setpoint_2 = ReadInt("DB9.DBW10");
            Setpoint_3 = ReadInt("DB9.DBW12");

            photoirradiance_sensor = ReadInt("DB9.DBW30");
        }
        catch (Exception ex)
        {
            // Don’t kill the connection flag on one bad address read.
            // Just report occasionally.
            if (Time.frameCount % 300 == 0)
                Debug.LogWarning($"ReadPLC warning: {ex.Message}");
        }
    }

    // ---------------------- HELPER READ FUNCTIONS ----------------------
    private bool ReadBool(string address)
    {
        try { return Convert.ToBoolean(plc.Read(address)); }
        catch (Exception ex)
        {
            if (Time.frameCount % 300 == 0)
                Debug.LogWarning($"Error reading bool from {address}: {ex.Message}");
            return false;
        }
    }

    private int ReadInt(string address)
    {
        try { return Convert.ToInt16(plc.Read(address)); }
        catch (Exception ex)
        {
            if (Time.frameCount % 300 == 0)
                Debug.LogWarning($"Error reading int from {address}: {ex.Message}");
            return 0;
        }
    }

    // ---------------------- WRITE FUNCTIONS ----------------------
    public void WriteBool(string address, bool value)
    {
        if (!IsConnected()) return;

        try { plc.Write(address, value); }
        catch (Exception ex)
        {
            Debug.LogError($"Error writing bool to {address}: {ex.Message}");
        }
    }

    public void WriteInt(string address, int value)
    {
        if (!IsConnected()) return;

        try { plc.Write(address, (short)value); }
        catch (Exception ex)
        {
            Debug.LogError($"Error writing int to {address}: {ex.Message}");
        }
    }

    // ---------------------- UPDATE PLC FROM UNITY ----------------------
    public void UpdatePLCFromUnity()
    {
        if (!IsConnected()) return;

        // DIGITAL
        WriteBool("DB9.DBX32.0", Start_btn);
        WriteBool("DB9.DBX32.1", Stop_btn);

        // ANALOG (actuators & setpoints)
        WriteInt("DB9.DBW16", H_Air_vent_valve_1);
        WriteInt("DB9.DBW18", H_Air_vent_valve_2);
        WriteInt("DB9.DBW14", H_Air_vent_valve_3);

        WriteInt("DB9.DBW8", Setpoint_1);
        WriteInt("DB9.DBW10", Setpoint_2);
        WriteInt("DB9.DBW12", Setpoint_3);

        // SENSOR values
        WriteInt("DB9.DBW0", Temp_Sense_1);
        WriteInt("DB9.DBW2", Temp_Sense_2);
        WriteInt("DB9.DBW4", Temp_Sense_3);

        WriteInt("DB9.DBW22", Chamber_1_Temp);
        WriteInt("DB9.DBW24", Chamber_2_Temp);
        WriteInt("DB9.DBW26", Chamber_3_Temp);

        WriteInt("DB9.DBW30", photoirradiance_sensor);
    }

    // ---------------------- PUBLIC HELPER METHODS ----------------------
    public bool IsConnected()
    {
        return plc != null && plc.IsConnected;
    }

    public bool GetConnectionStatus()
    {
        return PLCConnected;
    }

    void OnDestroy()
    {
        try
        {
            if (plc != null && plc.IsConnected)
                plc.Close();
        }
        catch { }
    }

    void OnApplicationQuit()
    {
        OnDestroy();
    }
}
