using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// show score stored in PlayStats when the scene is loaded
public class ShowScore : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI accText;
    public TextMeshProUGUI rankText;
    public Button backButton;
    // public TextMeshProUGUI perfectCountText;
    // public TextMeshProUGUI goodCountText;
    // public TextMeshProUGUI badCountText;
    // public TextMeshProUGUI missCountText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // perfectCountText.text = PlayStats.perfectCount.ToString();
        // goodCountText.text = PlayStats.goodCount.ToString();
        // badCountText.text = PlayStats.badCount.ToString();
        // missCountText.text = PlayStats.missCount.ToString();
        titleText.text = PlayInfo.meta.title;
        
        accText.text = GameStats.acc.ToString("F2") + "%";
        rankText.text = GameStats.acc switch
        {
            100f => "P",
            > 99.5f => "SSS",
            > 99f => "SS", 
            > 98f => "S",
            > 95f => "AAA",
            > 92f => "AA",
            > 88f => "A",
            > 80f => "B",
            > 60f => "C",
            _ => "D"
        };

        backButton.onClick.AddListener(BackToTitle);
    }

    public void BackToTitle()
    {
        Util.Transition("SongSelectScene");
    }
}
