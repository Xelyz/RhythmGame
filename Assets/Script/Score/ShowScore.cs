using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

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
    public Image jacket;

    void Start()
    {
        // perfectCountText.text = PlayStats.perfectCount.ToString();
        // goodCountText.text = PlayStats.goodCount.ToString();
        // badCountText.text = PlayStats.badCount.ToString();
        // missCountText.text = PlayStats.missCount.ToString();
        titleText.text = PlayInfo.meta.title;
        
        // jacket sprite defaults to placeholder
        Sprite sprite = Resources.Load<Sprite>($"songs/{PlayInfo.meta.id}/jacket");
        if (sprite != null)
        {
            jacket.sprite = sprite;
        }

        accText.text = GameStats.acc.ToString("F2") + "%";
        rankText.text = GetRating(GameStats.acc);

        backButton.onClick.AddListener(BackToTitle);

        UpdateResult();
        Util.SaveData();
    }

    private string GetRating(float acc)
    {
        return acc switch
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
    }

    public void BackToTitle()
    {
        Util.Transition("SongSelectScene");
    }

    public void UpdateResult()
    {
        PlayResult playResult = GetPlayResult();

        if (!Values.playerData.songResults.TryGetValue(PlayInfo.meta.id, out var data))
        {
            Values.playerData.songResults[PlayInfo.meta.id] = data = new();
        }
        
        PlayResult oldScore = data.GetValueOrDefault(PlayInfo.diff, new());
        
        if (playResult.accuracy > oldScore.accuracy)
        {
            data[PlayInfo.diff] = playResult;
        }
    }

    private PlayResult GetPlayResult()
    {
        return new()
        {
            accuracy = GameStats.acc,
            rating = GetRating(GameStats.acc)
        };
    }
}
