using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
    private float spawnTime;

    private void Awake()
    {
        Instance = this;
        gameState = new GameState();
        audioManager = FindFirstObjectByType<AudioManager>();
        gameUI = FindFirstObjectByType<GameUI>();
        
        spawnTime = Values.spawnTime;
    }

    private void OnEnable()
    {
        // 每次启用时重新初始化（支持多次开启/关闭预览）
        if (PlayInfo.meta != null)
        {
            LoadChart();
            // GameUI 会在自己的 OnEnable 中设置背景
            audioManager?.InitGameMusic(PlayInfo.meta.id);
            
            StartCoroutine(WaitForComponentsReady());
        }
        else
        {
            Debug.LogWarning("GameManager: PlayInfo.meta is null, cannot initialize game.");
        }
    }

    private void OnDisable()
    {
        // 清理状态，为下次启用做准备
        StopAllCoroutines();
        nextNoteIndex = 0;
        notes.Clear();
        
        // 清理小节线管理器
        BarlineManager barlineManager = FindFirstObjectByType<BarlineManager>();
        if (barlineManager != null)
        {
            Destroy(barlineManager.gameObject);
        }
        
        // 重置游戏状态
        if (gameState != null)
        {
            gameState.EndGame();
            gameState.EndAudio();
        }
        
        // 清理动画和音频
        DOTween.KillAll();
        if (audioManager != null)
        {
            audioManager.Stop();
            audioManager.ClearMusic();
        }
    }

    private void Start()
    {
        // Start 中不再执行初始化逻辑，改为在 OnEnable 中执行
    }

    private IEnumerator WaitForComponentsReady()
    {
        while (!audioManager.isAudioReady)
        {
            yield return null;
        }

        Transfer.sceneReady = true;

        StartCoroutine(GameStart());
        StartCoroutine(Util.DelayAction(() => CursorControl.Instance.FadeInCircle(), 1f));
    }

    private IEnumerator GameStart()
    {
        gameState.StartGame();
        // Autoplay: 启用外部控制（若开启）
        if (PlayInfo.isAutoplay)
        {
            CursorControl.Instance.EnableAutoplayControl(true);
        }

        float elapsedTime = 0f;

        while (elapsedTime * 1000f < Values.waitTime)
        {
            if (!gameState.IsPaused)
            {
                elapsedTime += Time.deltaTime;
                gameState.CurrentTime = (elapsedTime * 1000f) - Values.waitTime + Values.Preference.offsetms;
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

            // 依据事件预计算小节线时间戳
            List<float> barlineTimes = Util.BuildBarlineTimestamps(chartData.events, notes);
            Debug.Log($"Built {barlineTimes.Count} barlines from events.");
            Debug.Log($"Barline times: {string.Join(", ", barlineTimes)}");

            if (barlineTimes.Count > 0)
            {
                GameObject barGo = new("BarlineManager");
                BarlineManager barlineManager = barGo.AddComponent<BarlineManager>();
                barlineManager.Init(NoteHolder, barlineTimes);
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
            gameState.CurrentTime = (audioManager.CurrentTime * 1000f) + Values.Preference.offsetms;
            
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
            notes[nextNoteIndex].Initialize(NoteHolder);
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

}