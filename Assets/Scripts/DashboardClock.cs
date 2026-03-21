using TMPro;
using UnityEngine;
using System;

public class DashboardClock : MonoBehaviour
{
    public TMP_Text timeText;
    public TMP_Text dateText;

    void Start()
    {
        // Update immediately on start
        UpdateClock();
        // Update every second
        InvokeRepeating(nameof(UpdateClock), 1f, 1f);
    }

    void UpdateClock()
    {
        DateTime now = DateTime.Now;

        // Format examples:
        // 12:45 PM
        timeText.text = now.ToString("hh:mm tt");

        // 22 NOV 2025
        dateText.text = now.ToString("dd MMM yyyy").ToUpper();
    }
}
