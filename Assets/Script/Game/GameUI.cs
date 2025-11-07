using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausingPage;
    [SerializeField] private TextMeshProUGUI indicator;
    [SerializeField] private Image background;

    private void OnEnable()
    {
        // 每次启用时设置背景（支持 PlayInfo 在 session 内变化）
        if (PlayInfo.meta != null)
        {
            SetBackground(PlayInfo.meta.id);
        }
    }

    public void SetBackground(string songId)
    {
        Sprite jacket = Resources.Load<Sprite>($"songs/{songId}/jacket");

        if (jacket != null)
        {
            background.sprite = jacket;
        }
        else
        {
            background.color = Color.black;
            Debug.LogError($"Jacket not found for song {songId}");
        }
    }

    public void ShowPauseUI()
    {
        pauseButton.gameObject.SetActive(false);
        pausingPage.SetActive(true);
    }

    public void HidePauseUI()
    {
        pausingPage.SetActive(false);
        pauseButton.gameObject.SetActive(true);
    }

    public IEnumerator ShowCountdown(float totalTime)
    {
        indicator.gameObject.SetActive(true);
        float endTime = Time.time + totalTime;

        while (Time.time < endTime)
        {
            indicator.text = Mathf.CeilToInt(endTime - Time.time).ToString();
            yield return new WaitForSeconds(0.1f);
        }

        indicator.gameObject.SetActive(false);
    }
}