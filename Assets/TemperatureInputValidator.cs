using TMPro;
using UnityEngine;

public class TemperatureInputValidator : MonoBehaviour
{
    public TMP_InputField inputField;

    void Start()
    {
        inputField.onEndEdit.AddListener(ValidateInput);
    }

    void ValidateInput(string text)
    {
        if (!int.TryParse(text, out int value))
        {
            inputField.text = "0";
            return;
        }

        value = Mathf.Clamp(value, 0, 100);
        inputField.text = value.ToString();
    }
}
