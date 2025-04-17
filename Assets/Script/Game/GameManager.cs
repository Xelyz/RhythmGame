using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public List<Note> notes = new();
    public Transform noteHolder;

    private AudioSource AudioSource => AudioManager.Instance.musicSource;

    public Button pauseButton;
    public GameObject pausingPage;
    public TextMeshProUGUI indicator;

    public int currentTime = 0;
    int spawnTime;

    public bool isGamePlaying = false;
    public bool isAudioPlaying = false;
    public bool isPaused = false;

    void Awake()
    {
        Instance = this;
        Resources.UnloadUnusedAssets();

        spawnTime = Values.spawnTime;
        LoadChart();
        StartCoroutine(AudioManager.Instance.InitGameMusic(PlayInfo.meta.id));
    }

    void Start()
    {
        StartCoroutine(WaitForComponentsReady());
    }

    IEnumerator WaitForComponentsReady()
    {
        while (!AudioManager.Instance.isAudioReady)
        {
            yield return null;
        }

        StartCoroutine(GameStart());
    }

    IEnumerator GameStart()
    {
        currentTime = -Values.waitTime; // Set currentTime to a negative value during the initial wait period
        isGamePlaying = true;
        float elapsedTime = 0f;

        while (elapsedTime < Values.waitTime / 1000f)
        {
            if (!isPaused) // Only accumulate time when not paused
            {
                elapsedTime += Time.deltaTime;
                currentTime = (int)(elapsedTime * 1000) - Values.waitTime;
            }
            yield return null;
        }

        isAudioPlaying = true;
        AudioSource.Play();
        AudioSource.time = 0f;
    }

    private void LoadChart()
    {
        TextAsset rawChart = Resources.Load<TextAsset>($"songs/{PlayInfo.meta.id}/chart_{PlayInfo.diff}");

        if (rawChart != null)
        {
            string chart = rawChart.text;
            Chart chartData = Util.GetChart(chart);
            notes = chartData.notes;

            Debug.Log($"Loaded {notes.Count} notes.");
        }
        else
        {
            Debug.LogError($"Failed to load chart_{PlayInfo.diff} from Resources.");
        }
    }

    private int nextNoteIndex = 0;

    // Update is called once per frame
    void Update()
    {
        if (!isGamePlaying) return;

        SpawnNotes();

        if (!isAudioPlaying) return;

        currentTime = (int)(AudioSource.time * 1000);

        // 检查游戏是否结束
        if (IsGameFinished())
        {
            StartCoroutine(EndGame());
            return;
        }
    }

    private bool IsGameFinished()
    {
        return currentTime >= notes[^1].timeStamp &&
               TouchInput.Instance.judgmentQueue.Count == 0;
    }

    private void SpawnNotes()
    {
        while (ShouldSpawnNextNote())
        {
            notes[nextNoteIndex].Initialize(noteHolder);
            nextNoteIndex++;
        }
    }

    private bool ShouldSpawnNextNote()
    {
        return nextNoteIndex < notes.Count &&
               currentTime >= notes[nextNoteIndex].timeStamp - spawnTime;
    }

    private IEnumerator EndGame()
    {
        isGamePlaying = false;

        // 2秒音乐渐隐
        float startVolume = AudioSource.volume;
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            AudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        isAudioPlaying = false;
        AudioSource.Stop();

        ShowScore();
    }

    // 当应用暂停状态改变时调用
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Pause();
        }
    }

    public void Pause()
    {
        // pausing not allowed when game is not playing
        if (!isGamePlaying) return;

        isGamePlaying = false;
        isAudioPlaying = false;
        isPaused = true;
        AudioSource.Pause();
        DOTween.PauseAll();
        pauseButton.gameObject.SetActive(false);
        pausingPage.SetActive(true);
    }

    public void Restart()
    {
        DOTween.KillAll();
        Util.Transition("GameScene");
    }

    public void Resume()
    {
        pausingPage.SetActive(false);

        void func()
        {
            DOTween.PlayAll();
            isPaused = false;
            isGamePlaying = true;
            isAudioPlaying = true;
            AudioSource.UnPause();
            pauseButton.gameObject.SetActive(true);
        }

        StartCoroutine(CountDown(2f));
        StartCoroutine(Util.DelayAction(func, 2f));
    }

    private IEnumerator CountDown(float totalTime)
    {
        indicator.gameObject.SetActive(true);
        float endTime = Time.time + totalTime;

        while (Time.time < endTime)
        {
            int remainingSeconds = Mathf.CeilToInt(endTime - Time.time);
            indicator.text = remainingSeconds.ToString();
            yield return new WaitForSeconds(0.1f); // Update less frequently for better performance
        }

        indicator.gameObject.SetActive(false);
    }

    public void Quit()
    {
        DOTween.KillAll();
        Util.Transition("SongSelectScene");
    }

    public void ShowScore()
    {
        Util.Transition("ScoreScene");
    }
}