using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Setting : MonoBehaviour
{
    public Button returnButton;
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValue;
    public Slider noteSpeedSlider;
    public TextMeshProUGUI noteSpeedValue;

    void Start()
    {
        returnButton.onClick.AddListener(Return);

        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        sensitivitySlider.value = Values.Preference.sensitivity;
        sensitivityValue.text = Values.Preference.sensitivity.ToString("F2");

        noteSpeedSlider.onValueChanged.AddListener(SetNoteSpeed);
        noteSpeedSlider.value = Values.Preference.noteSpeed;
        noteSpeedValue.text = Values.Preference.noteSpeed.ToString("0");
    }

    void Return()
    {
        Util.Transition("SongSelectScene");
    }

    void SetSensitivity(float value)
    {
        Values.Preference.sensitivity = value;
        sensitivityValue.text = value.ToString("F2");
    }

    void SetNoteSpeed(float value)
    {
        Values.Preference.noteSpeed = value;
        noteSpeedValue.text = value.ToString("0");
    }
}
