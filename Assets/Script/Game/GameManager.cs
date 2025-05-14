using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Transform noteHolder;
    [SerializeField] private GameUI gameUI;

    private AudioManager audioController;
    internal GameState gameState;
    internal List<Note> notes = new();
    private int nextNoteIndex = 0;
    private int spawnTime;

    private void Awake()
    {
        Instance = this;
        gameState = new GameState();
        audioController = FindFirstObjectByType<AudioManager>();
        
        spawnTime = Values.spawnTime;
        LoadChart();
        StartCoroutine(audioController.InitGameMusic(PlayInfo.meta.id));
    }

    private void Start()
    {
        StartCoroutine(WaitForComponentsReady());
    }

    private IEnumerator WaitForComponentsReady()
    {
        while (!audioController.isAudioReady)
        {
            yield return null;
        }

        Transfer.sceneReady = true;
        StartCoroutine(GameStart());
    }

    private IEnumerator GameStart()
    {
        gameState.StartGame();
        float elapsedTime = 0f;

        while (elapsedTime < Values.waitTime / 1000f)
        {
            if (!gameState.IsPaused)
            {
                elapsedTime += Time.deltaTime;
                gameState.CurrentTime = (int)(elapsedTime * 1000) - Values.waitTime;
            }
            yield return null;
        }

        gameState.StartAudio();
        audioController.Play();
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

    private void Update()
    {
        if (!gameState.IsPlaying) return;

        SpawnNotes();
        
        if (gameState.IsAudioPlaying)
        {
            gameState.CurrentTime = (int)(audioController.CurrentTime * 1000);
            
            if (IsGameFinished())
            {
                StartCoroutine(EndGame());
            }
        }
    }

    private bool IsGameFinished()
    {
        return gameState.CurrentTime >= notes[^1].timeStamp &&
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
               gameState.CurrentTime >= notes[nextNoteIndex].timeStamp - spawnTime;
    }

    private IEnumerator EndGame()
    {
        gameState.EndGame();

        // 2秒音乐渐隐
        float startVolume = audioController.musicSource.volume;
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioController.musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        gameState.EndAudio();
        audioController.Stop();

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
        if (!gameState.IsPlaying) return;

        gameState.Pause();
        audioController.Pause();
        DOTween.PauseAll();
        gameUI.ShowPauseUI();
    }

    public void Resume()
    {
        gameUI.HidePauseUI();
                
        StartCoroutine(gameUI.ShowCountdown(2f));
        StartCoroutine(Util.DelayAction(() => {
            gameState.Resume();
            audioController.UnPause();
            DOTween.PlayAll();
        }, 2f));
    }

    public void Restart()
    {
        StopAllCoroutines();
        OnDisable();
        Util.Transition("GameScene");
    }

    public void Quit()
    {
        StopAllCoroutines();
        OnDisable();
        Util.Transition("SongSelectScene");
    }

    public void ShowScore()
    {
        Util.Transition("ScoreScene");
    }

    private void OnDisable()
    {
        // 确保清理所有动画和音频
        DOTween.KillAll();
        if (audioController != null)
        {
            audioController.Stop();
        }
    }
}