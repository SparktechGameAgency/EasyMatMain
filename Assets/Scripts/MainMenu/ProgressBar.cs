using UnityEngine;
using UnityEngine.UI;
using TMPro; // Make sure you are using TextMeshPro

public class ProgressBar : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI textDisplay;

    public void UpdateProgress(float current, float max)
    {
        // Update the slider fill
        slider.maxValue = max;
        slider.value = current;

        // Update the text display
        if (textDisplay != null)
        {
            textDisplay.text = $"{current}/{max}";
        }
    }
}