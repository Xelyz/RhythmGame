using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

    public bool isGamePlaying = false;
    public bool isAudioPlaying = false;

    void Awake()
    {
        Instance = this;
        Resources.UnloadUnusedAssets();

        spawnTime = Values.spawnTime;
        LoadChart();
        audioSource.clip = Resources.Load<AudioClip>($"songs/{PlayInfo.meta.id}/track");
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
        isGamePlaying = true;
        float startTime = Time.time;
        while (Time.time - startTime < Values.waitTime / 1000f)
        {
            currentTime = (int)((Time.time - startTime) * 1000) - Values.waitTime;
            yield return null;
        }
        isAudioPlaying = true;
        audioSource.Play();
        Debug.Log("Game Start");
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
        float startVolume = audioSource.volume;
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        isAudioPlaying = false;
        audioSource.Stop();

        ShowScore();
    }

    public void Pause()
    {
        isGamePlaying = false;
        isAudioPlaying = false;
        audioSource.Pause();
        Time.timeScale = 0f;
        pauseButton.gameObject.SetActive(false);
        pausingPage.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        DOTween.KillAll();
        Util.Transition("GameScene");
    }

    public void Resume()
    {
        isGamePlaying = true;
        isAudioPlaying = true;
        audioSource.Play();
        Time.timeScale = 1f;
        pauseButton.gameObject.SetActive(true);
        pausingPage.SetActive(false);
    }

    public void Quit()
    {
        Time.timeScale = 1f;
        DOTween.KillAll();
        Util.Transition("SongSelectScene");
    }

    public void ShowScore()
    {
        Util.Transition("ScoreScene");
    }
}