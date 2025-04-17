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
        noteSpeedSlider.value = Values.Preference.noteSpeed * 2;
        noteSpeedValue.text = (Values.Preference.noteSpeed / 5).ToString("F1");
    }

    void Return()
    {
        Util.Transition("SongSelectScene");
        Util.SaveData();
    }

    void SetSensitivity(float value)
    {
        Values.Preference.sensitivity = value;
        sensitivityValue.text = value.ToString("F2");
    }

    void SetNoteSpeed(float value)
    {
        Values.Preference.noteSpeed = value / 2;
        noteSpeedValue.text = (value / 10).ToString("F1");
    }
}
