using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Setting : MonoBehaviour
{
    public Button returnButton;
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValue;

    void Start()
    {
        returnButton.onClick.AddListener(Return);
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        sensitivitySlider.value = Values.Preference.sensitivity;
        sensitivityValue.text = Values.Preference.sensitivity.ToString("F2");
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
}
