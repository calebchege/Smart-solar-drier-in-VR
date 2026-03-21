using UnityEngine;

public class DamperController : MonoBehaviour
{
    [Range(0f, 1f)]
    public float currentOpening; // 0 = closed, 1 = fully open

    public Transform damperBlade;
    public float maxAngle = 90f;

    public void SetOpening(float opening)
    {
        currentOpening = Mathf.Clamp01(opening);
        UpdateDamperVisual();
    }

    void UpdateDamperVisual()
    {
        if (damperBlade != null)
        {
            damperBlade.localRotation =
                Quaternion.Euler(0f, 0f, maxAngle * currentOpening);
        }
    }
}