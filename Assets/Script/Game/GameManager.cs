using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public List<Note> notes = new();
    public Transform noteHolder;

    public AudioSource audioSource;

    public Button pauseButton;
    public GameObject pausingPage;

    public int currentTime = 0;
    int spawnTime;

    public bool isPlaying = false;

    void Awake()
    {
        Instance = this;
        Resources.UnloadUnusedAssets();

        spawnTime = Values.spawnTime;
        LoadChart();
        audioSource.clip = Resources.Load<AudioClip>($"songs/{PlayStats.meta.id}/track");
        audioSource.clip.LoadAudioData();
    }

    void Start()
    {
        audioSource.Pause();
        audioSource.time = 0f;

        StartCoroutine(GameStart());
    }

    IEnumerator GameStart()
    {
        currentTime = -Values.waitTime; // Set currentTime to a negative value during the initial wait period
        float startTime = Time.time;
        while (Time.time - startTime < Values.waitTime / 1000f)
        {
            currentTime = (int)((Time.time - startTime) * 1000) - Values.waitTime;
            yield return null;
        }
        isPlaying = true;
        audioSource.Play();
        Debug.Log("Game Start");
    }

    private void LoadChart()
    {
        TextAsset rawChart = Resources.Load<TextAsset>($"songs/{PlayStats.meta.id}/chart_{PlayStats.diff}");

        if (rawChart != null)
        {
            string chart = rawChart.text;
            Chart chartData = Util.GetChart(chart);
            notes = chartData.notes;

            Debug.Log($"Loaded {notes.Count} notes.");
        }
        else
        {
            Debug.LogError($"Failed to load chart_{PlayStats.diff} from Resources.");
        }
    }

    private int nextNoteIndex = 0;

    // Update is called once per frame
    void Update()
    {
        SpawnNotes();

        if (!isPlaying) return;

        currentTime = (int)(audioSource.time * 1000);

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
               TouchInput.Instance.holdJudgmentQueue.Count == 0;
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
        isPlaying = false;

        // 2秒音乐渐隐
        float startVolume = audioSource.volume;
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        audioSource.Stop();

        ShowScore();
    }

    public void Pause()
    {
        isPlaying = false;
        audioSource.Pause();
        pauseButton.gameObject.SetActive(false);
        pausingPage.SetActive(true);
    }

    public void Restart()
    {
        Util.Transition("GameScene");
    }

    public void Resume()
    {
        isPlaying = true;
        audioSource.Play();
        pauseButton.gameObject.SetActive(true);
        pausingPage.SetActive(false);
    }

    public void Quit()
    {
        Util.Transition("SongSelectScene");
    }

    public void ShowScore()
    {
        Util.Transition("ScoreScene");
    }
}