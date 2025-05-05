using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausingPage;
    [SerializeField] private TextMeshProUGUI indicator;

    public void ShowPauseUI()
    {
        pauseButton.gameObject.SetActive(false);
        pausingPage.SetActive(true);
    }

    public void ShowResumeUI()
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