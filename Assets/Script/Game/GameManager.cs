using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System.Reflection;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Transform noteHolder;
    public Transform NoteHolder => noteHolder;
    private GameUI gameUI;
    private AudioManager audioManager;
    internal GameState gameState;
    
    internal List<Note> notes = new();
    private int nextNoteIndex = 0;
    private int spawnTime;
    private GameObject barlineManagerGO;

    private void Awake()
    {
        Instance = this;
        gameState = new GameState();
        audioManager = FindFirstObjectByType<AudioManager>();
        gameUI = FindFirstObjectByType<GameUI>();
        
        spawnTime = Values.spawnTime;

        LoadChart();
        gameUI.SetBackground(PlayInfo.meta.id);
        StartCoroutine(audioManager.InitGameMusic(PlayInfo.meta.id));
    }

    private void Start()
    {
        StartCoroutine(WaitForComponentsReady());
    }

    private IEnumerator WaitForComponentsReady()
    {
        while (!audioManager.isAudioReady)
        {
            yield return null;
        }

        Transfer.sceneReady = true;

        StartCoroutine(GameStart());
        StartCoroutine(Util.DelayAction(() => DigitalLevel.Instance.FadeInCircle(), 1f));
    }

    private IEnumerator GameStart()
    {
        gameState.StartGame();

        float elapsedTime = 0f;

        while (elapsedTime * 1000f < Values.waitTime)
        {
            if (!gameState.IsPaused)
            {
                elapsedTime += Time.deltaTime;
                gameState.CurrentTime = (int)(elapsedTime * 1000) - Values.waitTime + Values.Preference.offsetms;
            }
            yield return null;
        }

        gameState.StartAudio();
        audioManager.Play();
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
            
            // 初始化小节线管理器
            if (chartData.beatIntervalMs > 0f)
            {
                GameObject barGo = new("BarlineManager");
                barlineManagerGO = barGo;
                var asm = typeof(Note).Assembly;
                var barType = asm.GetType("BarlineManager");
                if (barType != null)
                {
                    var comp = barGo.AddComponent(barType);
                    var init = barType.GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
                    init?.Invoke(comp, new object[] { NoteHolder, chartData.beatIntervalMs });
                }
            }
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
            gameState.CurrentTime = (int)(audioManager.CurrentTime * 1000) + Values.Preference.offsetms;
            
            if (IsGameFinished())
            {
                StartCoroutine(EndGame());
            }
        }
    }

    private bool IsGameFinished()
    {
        return gameState.CurrentTime >= notes[^1].timeStamp &&
               !NoteJudge.Instance.IsJudging();
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
        float startVolume = audioManager.musicSource.volume;
        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioManager.musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        gameState.EndAudio();
        audioManager.Stop();

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
        audioManager.Pause();
        DOTween.PauseAll();
        gameUI.ShowPauseUI();
    }

    public void Resume()
    {
        gameUI.HidePauseUI();
                
        StartCoroutine(gameUI.ShowCountdown(2f));
        StartCoroutine(Util.DelayAction(() => {
            gameState.Resume();
            audioManager.UnPause();
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
        if (audioManager != null)
        {
            audioManager.Stop();
        }
    }
}