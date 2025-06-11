using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Setting : MonoBehaviour
{
    public Button returnButton;

    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValue;

    public Button sensitivityPlusButton;
    public Button sensitivityMinusButton;

    public Slider noteSpeedSlider;
    public TextMeshProUGUI noteSpeedValue;

    public Button noteSpeedPlusButton;
    public Button noteSpeedMinusButton;

    public TMP_InputField offsetInput;

    void Start()
    {
        returnButton.onClick.AddListener(Return);

        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        sensitivitySlider.value = Values.Preference.sensitivity;
        sensitivityValue.text = Values.Preference.sensitivity.ToString("F1");

        sensitivityPlusButton.onClick.AddListener(() => SetSensitivity(Values.Preference.sensitivity + 0.1f));
        sensitivityMinusButton.onClick.AddListener(() => SetSensitivity(Values.Preference.sensitivity - 0.1f));

        noteSpeedSlider.onValueChanged.AddListener(SetNoteSpeed);
        noteSpeedSlider.value = Values.Preference.noteSpeed;
        noteSpeedValue.text = (Values.Preference.noteSpeed / 10).ToString("F1");

        noteSpeedPlusButton.onClick.AddListener(() => SetNoteSpeed(Values.Preference.noteSpeed + 1f));
        noteSpeedMinusButton.onClick.AddListener(() => SetNoteSpeed(Values.Preference.noteSpeed - 1f));

        offsetInput.onValueChanged.AddListener(SetOffset);
        offsetInput.text = Values.Preference.offsetms.ToString();
    }

    void Return()
    {
        Util.Transition("SongSelectScene");
        Util.SaveData();
    }

    void SetSensitivity(float value)
    {
        value = Mathf.Clamp(value, 1f, 8f);
        Values.Preference.sensitivity = value;
        sensitivityValue.text = value.ToString("F1");
        sensitivitySlider.value = value;
    }

    void SetNoteSpeed(float value)
    {
        value = Mathf.Clamp(value, 1f, 10f);
        Values.Preference.noteSpeed = value;
        noteSpeedValue.text = (value / 10).ToString("F1");
        noteSpeedSlider.value = value;
    }
    
    void SetOffset(string value)
    {
        if (value == "")
        {
            value = "0";
            offsetInput.text = "0";
        }

        Values.Preference.offsetms = int.Parse(value);
    }
}
