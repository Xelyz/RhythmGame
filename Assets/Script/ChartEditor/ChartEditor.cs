using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Globalization;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

public class ChartEditor : MonoBehaviour
{
    // Constants
    private const float TIME_THRESHOLD = 50f; // 查找音符的时间容差（毫秒）
    private const float MAX_CHART_TIME = 600000f; // 最大谱面时间（10分钟，毫秒）
    private const float CLEANUP_MARGIN = 500f; // 清理窗口的边距（毫秒）
    private const float TIMELINE_DRAG_SENSITIVITY = 0.5f; // 时间轴拖拽灵敏度
    private const float AUTO_SAVE_INTERVAL = 30f; // 自动保存间隔（秒）
    
    [Header("UI References")]
    public RectTransform chartPreviewArea;
    public Slider timelineSlider;
    public InputField bpmInput;
    public InputField offsetInput;
    public Text currentTimeText;
    public Text currentBeatText;
    
    [Header("Note Type Selection")]
    public Button tapNoteButton;
    public Button dragNoteButton;
    public Button blockNoteButton;
    
    [Header("File Operations")]
    public Button importButton;
    public Button exportButton;
    public Button newChartButton;
    
    [Header("Difficulty Selection")]
    public Dropdown difficultyDropdown;
    
    [Header("Preview Settings")]
    public Transform noteHolder;
    
    [Header("Timeline Control")]
    public bool snapTo16thBeat = true;
    public float currentTime = 0f;
    public float currentBPM = 120f;
    public float offset = 0f;
    
    // Chart data
    private Chart currentChart;
    private NoteType selectedNoteType = NoteType.Tap;
    private bool isPlaying = false;
    private bool isPaused = false;
    
    // Preview display
    private Dictionary<Note, GameObject> displayedNoteObjects = new Dictionary<Note, GameObject>();
    private int nextNoteIndex = 0;
    
    // Audio
    private AudioSource audioSource;
    private AudioManager audioManager;
    private string audioFilePath;
    private AudioClip loadedAudioClip;
    private float maxChartTime = MAX_CHART_TIME; // 根据音频长度动态设置
    
    // Input
    private bool isDraggingTimeline = false;
    private Vector2 lastMousePosition;
    
    // Auto save
    private int currentDifficulty = 0; // 当前难度，用于追踪难度变化
    private Coroutine autoSaveCoroutine;
    
    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActionAsset;
    private InputActionMap chartEditorMap;
    private InputAction leftClickAction;
    private InputAction rightClickAction;
    private InputAction rightHoldAction;
    private InputAction mousePositionAction;
    private InputAction togglePlaybackAction;
    private Vector2 mousePosition;
    
    // Note type buttons for easy access
    private Button[] noteTypeButtons;
    private NoteType[] noteTypes = { NoteType.Tap, NoteType.Drag, NoteType.Block };
    
    public static ChartEditor Instance { get; private set; }
    
    void Awake()
    {
        Instance = this;
        InitializeEditor();
        InitializeInputSystem();
    }
    
    void OnDisable()
    {
        if (chartEditorMap != null)
        {
            leftClickAction.performed -= OnLeftClick;
            rightClickAction.performed -= OnRightClickStart;
            rightClickAction.canceled -= OnRightClickEnd;
            rightHoldAction.performed -= OnRightHold;
            rightHoldAction.canceled -= OnRightHoldEnd;
            togglePlaybackAction.performed -= OnTogglePlayback;
            chartEditorMap.Disable();
        }
    }
    
    void Start()
    {
        noteTypeButtons = new[] { tapNoteButton, dragNoteButton, blockNoteButton };
        SetupUI();
        CreateNewChart();
        UpdateTimelineDisplay();
        
        // 启动自动保存协程
        StartAutoSave();
    }
    
    void OnDestroy()
    {
        // 停止自动保存
        StopAutoSave();
        
        // 在销毁前保存当前谱面
        if (!string.IsNullOrEmpty(audioFilePath))
        {
            SaveChartForCurrentDifficulty();
        }
    }
    
    void Update()
    {
        UpdateMousePosition();
        
        if (isDraggingTimeline)
        {
            HandleTimelineDrag();
        }
        
        if (isPlaying && !isPaused)
        {
            UpdatePlayback();
        }
        
        UpdatePreviewAtTime(currentTime);
        UpdateTimelineDisplay();
    }
    
    void UpdateMousePosition()
    {
        if (mousePositionAction != null)
        {
            mousePosition = mousePositionAction.ReadValue<Vector2>();
        }
    }
    
    void InitializeEditor()
    {
        currentChart = new Chart
        {
            notes = new List<Note>(),
            events = new List<ChartEvent>()
        };
        
        audioManager = FindAnyObjectByType<AudioManager>();
        // 如果 AudioManager 不存在，尝试通过其他方式获取（编辑器兼容性）
        if (audioManager == null)
        {
            GameObject audioManagerGO = GameObject.Find("AudioManager");
            if (audioManagerGO != null)
            {
                audioManager = audioManagerGO.GetComponent<AudioManager>();
            }
        }
        // 保留 audioSource 引用用于直接访问（如果 AudioManager 不存在时）
        audioSource = audioManager?.musicSource;
    }
    
    void InitializeInputSystem()
    {
        if (inputActionAsset == null)
        {
            Debug.LogError("Input Action Asset 未设置！请在Inspector中分配ChartEditorInputActions资源");
            return;
        }
        
        // 获取ChartEditor action map
        chartEditorMap = inputActionAsset.FindActionMap("ChartEditor");
        if (chartEditorMap == null)
        {
            Debug.LogError("未找到ChartEditor Action Map！");
            return;
        }
        
        // 获取各个输入动作
        leftClickAction = chartEditorMap.FindAction("LeftClick");
        rightClickAction = chartEditorMap.FindAction("RightClick");
        rightHoldAction = chartEditorMap.FindAction("RightHold");
        mousePositionAction = chartEditorMap.FindAction("MousePosition");
        togglePlaybackAction = chartEditorMap.FindAction("TogglePlayback");
        
        // 设置回调函数
        leftClickAction.performed += OnLeftClick;
        rightClickAction.performed += OnRightClickStart;
        rightClickAction.canceled += OnRightClickEnd;
        rightHoldAction.performed += OnRightHold;
        rightHoldAction.canceled += OnRightHoldEnd;
        togglePlaybackAction.performed += OnTogglePlayback;
        
        // 启用输入
        chartEditorMap.Enable();
        
        Debug.Log("输入系统初始化完成");
    }
    
    void SetupUI()
    {
        // Note类型选择
        tapNoteButton?.onClick.AddListener(() => SelectNoteType(NoteType.Tap));
        dragNoteButton?.onClick.AddListener(() => SelectNoteType(NoteType.Drag));
        blockNoteButton?.onClick.AddListener(() => SelectNoteType(NoteType.Block));
        
        // 文件操作
        importButton?.onClick.AddListener(ImportAudioAndChart);
        exportButton?.onClick.AddListener(ExportChart);
        newChartButton?.onClick.AddListener(CreateNewChart);
        
        // 难度选择框
        SetupDifficultyDropdown();
        currentDifficulty = difficultyDropdown != null ? difficultyDropdown.value : 0;
        
        // BPM和Offset输入框
        if (bpmInput != null) 
        {
            bpmInput.text = currentBPM.ToString();
            bpmInput.onEndEdit.AddListener(OnBPMChanged);
        }
        if (offsetInput != null)
        {
            offsetInput.text = offset.ToString();
            offsetInput.onEndEdit.AddListener(OnOffsetChanged);
        }
        
        // 时间轴滑条
        timelineSlider?.onValueChanged.AddListener(OnTimelineChanged);
        
        // 默认选择Tap note
        SelectNoteType(NoteType.Tap);
    }
    
    void SetupDifficultyDropdown()
    {
        if (difficultyDropdown != null)
        {
            // 添加难度选项：0-3
            difficultyDropdown.ClearOptions();
            List<string> options = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                options.Add($"难度 {i}");
            }
            difficultyDropdown.AddOptions(options);
            difficultyDropdown.value = 0;
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }
    }
    
    void OnDifficultyChanged(int difficulty)
    {
        // 如果已经有音频文件，先保存当前难度的谱面，然后加载新难度的谱面
        if (!string.IsNullOrEmpty(audioFilePath))
        {
            // 保存当前难度的谱面（防止丢失）
            // 注意：这里保存的是切换前的难度（currentDifficulty），而不是dropdown的新值
            SaveChartForDifficulty(currentDifficulty);
            
            // 更新当前难度
            currentDifficulty = difficulty;
            
            // 加载新难度的谱面
            LoadChartForDifficulty(difficulty);
        }
        else
        {
            // 如果没有音频文件，只更新难度记录
            currentDifficulty = difficulty;
        }
    }
    
    void SetButtonText(Button button, string text)
    {
        Text buttonText = button?.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = text;
            buttonText.fontSize = 14;
            buttonText.color = Color.black;
            buttonText.alignment = TextAnchor.MiddleCenter;
        }
    }
    
    // Input System 事件处理方法
    void OnLeftClick(InputAction.CallbackContext context)
    {
        HandleLeftClick();
    }
    
    void OnRightClickStart(InputAction.CallbackContext context)
    {
        isDraggingTimeline = true;
        lastMousePosition = mousePosition;
    }
    
    void OnRightClickEnd(InputAction.CallbackContext context)
    {
        isDraggingTimeline = false;
    }
    
    void OnRightHold(InputAction.CallbackContext context)
    {
        // 右键按住事件 - 在Update中处理拖拽
    }
    
    void OnRightHoldEnd(InputAction.CallbackContext context)
    {
        isDraggingTimeline = false;
    }
    
    void OnTogglePlayback(InputAction.CallbackContext context)
    {
        TogglePlayback();
    }
    
    void HandleLeftClick()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            chartPreviewArea, mousePosition, Camera.main, out Vector2 localPoint);
        
        // 转换为网格坐标
        Vector2Int gridPos = Values.LocalToCellIndex(localPoint);
        
        // 检查是否在有效网格范围内
        if (gridPos.x >= 0 && gridPos.x < Values.gridColumns && 
            gridPos.y >= 0 && gridPos.y < Values.gridRows)
        {
            ToggleNoteAtPosition(gridPos);
        }
    }
    
    void HandleTimelineDrag()
    {
        Vector2 currentMousePosition = mousePosition;
        float deltaY = (currentMousePosition.y - lastMousePosition.y) * TIMELINE_DRAG_SENSITIVITY;
        
        currentTime = Mathf.Clamp(currentTime + deltaY, 0f, maxChartTime);
        
        if (snapTo16thBeat)
        {
            SnapToBeat(ref currentTime);
        }
        
        lastMousePosition = currentMousePosition;
    }
    
    void SnapToBeat(ref float time)
    {
        float beatLength = GetBeatLengthAtTime(time);
        float sixteenthBeat = beatLength / 4f;
        time = Mathf.Round(time / sixteenthBeat) * sixteenthBeat;
    }
    
    void ToggleNoteAtPosition(Vector2Int gridPos)
    {
        // 查找当前时间和位置是否已有note
        Note existingNote = FindNoteAt(currentTime, gridPos);
        
        if (existingNote != null)
        {
            // 删除现有note
            RemoveNote(existingNote);
            Debug.Log($"删除note at grid({gridPos.x},{gridPos.y}) time={currentTime:F1}ms");
        }
        else
        {
            // 创建新note
            Note newNote = CreateNote(selectedNoteType, gridPos, currentTime);
            AddNote(newNote);
            Debug.Log($"添加{selectedNoteType} note at grid({gridPos.x},{gridPos.y}) time={currentTime:F1}ms");
        }
        
        RefreshPreview();
    }
    
    Note FindNoteAt(float time, Vector2Int gridPos)
    {
        return currentChart.notes.Find(note =>
            Mathf.Abs(note.timeStamp - time) <= TIME_THRESHOLD &&
            note.cellIndex.x == gridPos.x &&
            note.cellIndex.y == gridPos.y);
    }
    
    Note CreateNote(NoteType type, Vector2Int gridPos, float time)
    {
        Note note = type switch
        {
            NoteType.Tap => new Tap(),
            NoteType.Drag => new Drag(),
            NoteType.Block => new Block(),
            _ => new Tap()
        };
        
        note.cellIndex = gridPos;
        note.position = Values.CellCenterLocal(gridPos.x, gridPos.y);
        note.timeStamp = time;
        note.nthNote = currentChart.notes.Count + 1;
        
        return note;
    }
    
    void AddNote(Note note)
    {
        currentChart.notes.Add(note);
        currentChart.notes.Sort((a, b) => a.timeStamp.CompareTo(b.timeStamp));
        ReassignNthNotes();
    }
    
    void RemoveNote(Note note)
    {
        if (currentChart.notes.Remove(note))
        {
            note.Release();
            ReassignNthNotes();
        }
    }
    
    void ReassignNthNotes()
    {
        for (int i = 0; i < currentChart.notes.Count; i++)
        {
            currentChart.notes[i].nthNote = i + 1;
        }
    }
    
    void SelectNoteType(NoteType type)
    {
        selectedNoteType = type;
        
        // 更新按钮高亮显示
        for (int i = 0; i < noteTypeButtons.Length && i < noteTypes.Length; i++)
        {
            if (noteTypeButtons[i] != null)
            {
                Image buttonImage = noteTypeButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = noteTypes[i] == type ? Color.yellow : Color.white;
                }
            }
        }
    }
    
    void TogglePlayback()
    {
        if (isPlaying)
        {
            // 暂停播放 - 使用 AudioManager
            if (audioManager != null)
            {
                audioManager.Pause();
            }
            else
            {
                audioSource?.Pause();
            }
            isPlaying = false;
            isPaused = true;
            PlayInfo.isAutoplay = false;
            
            Debug.Log("暂停播放");
        }
        else
        {
            // 开始/恢复播放 - 使用 AudioManager
            AudioClip clip = audioManager != null ? audioManager.CurrentClip : audioSource?.clip;
            if (clip != null)
            {
                float startTime = Mathf.Max(0f, (currentTime - offset) / 1000f);
                
                // 检查是否超出音频长度
                if (startTime >= clip.length)
                {
                    Debug.LogWarning("当前时间超出音频长度");
                    return;
                }
                
                if (isPaused)
                {
                    // 恢复播放
                    if (audioManager != null)
                    {
                        audioManager.UnPause();
                    }
                    else
                    {
                        audioSource?.UnPause();
                    }
                    Debug.Log("恢复播放");
                }
                else
                {
                    // 从当前时间开始播放，参照GameManager：减去offset
                    if (audioManager != null)
                    {
                        audioManager.SetTime(startTime);
                        audioManager.Play();
                    }
                    else
                    {
                        if (audioSource != null)
                        {
                            audioSource.time = startTime;
                            audioSource.Play();
                        }
                    }
                    Debug.Log($"开始播放谱面预览，时间: {startTime:F2}秒");
                }
                
                isPlaying = true;
                isPaused = false;
                
                // 启用autoplay模式预览
                PlayInfo.isAutoplay = true;
            }
            else
            {
                Debug.LogWarning("没有音频文件，无法播放");
            }
        }
    }
    
    void UpdatePlayback()
    {
        // 使用 AudioManager 检查播放状态和获取时间
        bool isAudioPlaying = audioManager != null ? audioManager.IsPlaying : (audioSource != null && audioSource.isPlaying);
        
        if (isAudioPlaying)
        {
            // 参照GameManager的处理方式：使用 AudioManager.CurrentTime
            float audioTime = audioManager != null ? audioManager.CurrentTime : (audioSource?.time ?? 0f);
            currentTime = audioTime * 1000f + offset;
            
            // 检查是否播放到最大长度
            if (currentTime >= maxChartTime)
            {
                StopPlayback();
                return;
            }
            
            UpdatePreviewAtTime(currentTime);
        }
    }
    
    void StopPlayback()
    {
        // 使用 AudioManager 停止播放
        if (audioManager != null)
        {
            if (audioManager.IsPlaying)
            {
                audioManager.Stop();
            }
        }
        else if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        isPlaying = false;
        isPaused = false;
        PlayInfo.isAutoplay = false;
        currentTime = maxChartTime;
    }
    
    void UpdatePreviewAtTime(float time)
    {
        // 清理超出范围的音符显示
        CleanupNotesOutsideWindow(time);
        
        // 显示当前时间点附近的notes（只显示未来的音符）
        float previewWindow = Values.spawnTime; // 使用游戏中的spawnTime作为预览窗口
        
        // 生成新音符（类似游戏中的SpawnNotes逻辑）
        SpawnPreviewNotes(time, previewWindow);
        
        // 更新已显示音符的位置（卷轴效果）
        UpdateNotePositions(time);
    }
    
    void SpawnPreviewNotes(float currentTime, float previewWindow)
    {
        ValidateAndResetNoteIndex(currentTime, previewWindow);
        
        // 生成预览窗口内的音符
        while (nextNoteIndex < currentChart.notes.Count)
        {
            Note note = currentChart.notes[nextNoteIndex];
            
            if (note.timeStamp > currentTime + previewWindow)
                break;
            
            if (note.timeStamp >= currentTime - CLEANUP_MARGIN && 
                note.timeStamp <= currentTime + previewWindow &&
                !displayedNoteObjects.ContainsKey(note))
            {
                ShowNoteInPreview(note, currentTime);
            }
            
            nextNoteIndex++;
        }
    }
    
    void ValidateAndResetNoteIndex(float currentTime, float previewWindow)
    {
        // 索引超出范围或时间回退太多时重置
        if (nextNoteIndex >= currentChart.notes.Count ||
            (nextNoteIndex > 0 && nextNoteIndex < currentChart.notes.Count &&
             currentTime < currentChart.notes[nextNoteIndex].timeStamp - previewWindow - 1000f))
        {
            // 找到第一个应该在预览窗口内的音符
            nextNoteIndex = 0;
            for (int i = 0; i < currentChart.notes.Count; i++)
            {
                if (currentChart.notes[i].timeStamp >= currentTime - Values.spawnTime)
                {
                    nextNoteIndex = Mathf.Max(0, i - 1);
                    break;
                }
            }
        }
    }
    
    void ShowNoteInPreview(Note note, float currentTime)
    {
        if (noteHolder == null || displayedNoteObjects.ContainsKey(note))
            return;
        
        note.Initialize(noteHolder);
        
        if (note is Tap tapNote && tapNote.gameObject != null)
        {
            displayedNoteObjects[note] = tapNote.gameObject;
            
            // 禁用自动滚动
            if (tapNote.circle != null)
            {
                NoteScroll scroll = tapNote.circle.gameObject.GetComponent<NoteScroll>();
                if (scroll != null) scroll.isActive = false;
                
                // 设置初始Z位置
                UpdateNoteZPosition(tapNote.circle.transform, note.timeStamp - currentTime);
            }
        }
    }
    
    void UpdateNoteZPosition(Transform noteTransform, float timeUntilNote)
    {
        Vector3 pos = noteTransform.position;
        pos.z = Values.planeDistance + (timeUntilNote / 1000f) * Values.Preference.NoteSpeed;
        noteTransform.position = pos;
    }
    
    void UpdateNotePositions(float currentTime)
    {
        foreach (var kvp in displayedNoteObjects)
        {
            if (kvp.Value == null) continue;
            
            Note note = kvp.Key;
            if (note is Tap tapNote && tapNote.circle != null)
            {
                UpdateNoteZPosition(tapNote.circle.transform, note.timeStamp - currentTime);
            }
        }
    }
    
    void CleanupNotesOutsideWindow(float currentTime)
    {
        float cleanupWindow = Values.spawnTime + CLEANUP_MARGIN;
        List<Note> notesToRemove = new List<Note>();
        
        foreach (var kvp in displayedNoteObjects)
        {
            Note note = kvp.Key;
            if (note.timeStamp < currentTime - CLEANUP_MARGIN || note.timeStamp > currentTime + cleanupWindow)
            {
                notesToRemove.Add(note);
            }
        }
        
        foreach (Note note in notesToRemove)
        {
            note.Release();
            displayedNoteObjects.Remove(note);
        }
        
        if (notesToRemove.Count > 0)
        {
            ResetNoteIndex(currentTime);
        }
    }
    
    void ResetNoteIndex(float currentTime)
    {
        nextNoteIndex = 0;
        for (int i = 0; i < currentChart.notes.Count; i++)
        {
            if (currentChart.notes[i].timeStamp >= currentTime - Values.spawnTime)
            {
                nextNoteIndex = i;
                break;
            }
        }
    }
    
    void ClearPreviewNotes()
    {
        // 清理所有预览中的note显示
        foreach (var kvp in displayedNoteObjects)
        {
            Note note = kvp.Key;
            note.Release();
        }
        displayedNoteObjects.Clear();
        nextNoteIndex = 0;
    }
    
    void RefreshPreview()
    {
        UpdatePreviewAtTime(currentTime);
    }
    
    void UpdateTimelineDisplay()
    {
        if (currentTimeText != null)
        {
            currentTimeText.text = $"时间: {currentTime:F0}ms";
        }
        
        if (currentBeatText != null)
        {
            // 参照游戏中的BPM处理，从ChartEvent中获取beatLength
            float beatLength = GetBeatLengthAtTime(currentTime);
            float currentBeat = currentTime / beatLength;
            currentBeatText.text = $"拍子: {currentBeat:F2}";
        }
        
        if (timelineSlider != null && !isDraggingTimeline)
        {
            timelineSlider.value = maxChartTime > 0 ? currentTime / maxChartTime : 0;
        }
    }
    
    void OnTimelineChanged(float value)
    {
        if (!isDraggingTimeline && !isPlaying)
        {
            currentTime = value * maxChartTime;
            currentTime = Mathf.Clamp(currentTime, 0f, maxChartTime);
            if (snapTo16thBeat)
            {
                SnapToBeat(ref currentTime);
            }
            UpdatePreviewAtTime(currentTime);
        }
    }
    
    void OnBPMChanged(string value)
    {
        if (float.TryParse(value, out float newBPM))
        {
            currentBPM = Mathf.Clamp(newBPM, 60f, 300f);
            UpdateBPMInChart();
        }
    }
    
    void OnOffsetChanged(string value)
    {
        if (float.TryParse(value, out float newOffset))
        {
            offset = newOffset;
        }
    }
    
    void UpdateBPMInChart()
    {
        // 更新谱面中的BPM事件
        if (currentChart.events == null)
        {
            currentChart.events = new List<ChartEvent>();
        }
        
        // 查找并更新初始BPM事件（参照Util.GetChart的处理方式）
        ChartEvent bpmEvent = currentChart.events.Find(e => e.type == "bpm" && e.timeStamp == 0);
        if (bpmEvent == null)
        {
            bpmEvent = new ChartEvent
            {
                timeStamp = 0,
                type = "bpm",
                data = (60000f / currentBPM).ToString(CultureInfo.InvariantCulture) // beatLength in ms
            };
            currentChart.events.Add(bpmEvent);
        }
        else
        {
            bpmEvent.data = (60000f / currentBPM).ToString(CultureInfo.InvariantCulture);
        }
    }
    
    float GetBeatLengthAtTime(float time)
    {
        if (currentChart.events == null || currentChart.events.Count == 0)
            return 60000f / currentBPM;
        
        // 找到当前时间点之前最近的BPM事件
        ChartEvent latestBPMEvent = null;
        foreach (var ev in currentChart.events)
        {
            if (ev.type == "bpm" && ev.timeStamp <= time)
            {
                if (latestBPMEvent == null || ev.timeStamp > latestBPMEvent.timeStamp)
                {
                    latestBPMEvent = ev;
                }
            }
        }
        
        if (latestBPMEvent != null &&
            float.TryParse(latestBPMEvent.data, NumberStyles.Float, CultureInfo.InvariantCulture, out float beatLength))
        {
            return Mathf.Max(1f, beatLength);
        }
        
        return 60000f / currentBPM;
    }
    
    void CreateNewChart()
    {
        // 停止播放 - 使用 AudioManager
        if (audioManager != null)
        {
            audioManager.Stop();
        }
        else
        {
            audioSource?.Stop();
        }
        isPlaying = false;
        isPaused = false;
        PlayInfo.isAutoplay = false;
        
        ClearPreviewNotes();
        
        currentChart = new Chart
        {
            notes = new List<Note>(),
            events = new List<ChartEvent>()
        };
        
        currentTime = 0f;
        currentBPM = 120f;
        offset = 0f;
        
        // 如果有音频文件，使用音频长度作为最大时间
        if (loadedAudioClip != null)
        {
            maxChartTime = loadedAudioClip.length * 1000f;
        }
        else
        {
            maxChartTime = MAX_CHART_TIME;
        }
        
        UpdateBPMInChart();
        RefreshPreview();
        
        Debug.Log("创建新谱面");
    }
    
    void ImportAudioAndChart()
    {
        // 打开文件选择器选择音频文件
        string selectedFilePath = OpenFileDialog("选择音频文件", GetAudioFileExtensions());
        
        if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
        {
            Debug.LogWarning("未选择有效的音频文件");
            return;
        }
        
        StartCoroutine(LoadAudioAndChart(selectedFilePath));
    }
    
    string OpenFileDialog(string title, string extensions)
    {
        // Unity运行时文件选择器实现
        // 在不同平台使用不同的方法
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.OpenFilePanel(title, "", extensions);
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        // 运行时可以使用第三方库，这里先使用简单的路径输入
        Debug.LogWarning("运行时文件选择器需要实现，请手动输入文件路径");
        // 可以添加一个输入框让用户输入路径
        return "";
#else
        Debug.LogWarning("当前平台不支持文件选择器");
        return "";
#endif
    }
    
    string GetAudioFileExtensions()
    {
        return "mp3;wav;ogg;m4a;aac";
    }
    
    IEnumerator LoadAudioAndChart(string audioPath)
    {
        audioFilePath = audioPath;
        
        // 加载音频文件
        yield return StartCoroutine(LoadAudioClip(audioPath));
        
        if (loadedAudioClip == null)
        {
            Debug.LogError("音频文件加载失败");
            yield break;
        }
        
        // 设置音频源 - 使用 AudioManager
        if (audioManager != null)
        {
            // 使用 AudioManager 的 musicSource
            audioManager.SetClip(loadedAudioClip);
            audioSource = audioManager.musicSource;
        }
        else
        {
            // 如果 AudioManager 不存在，创建临时 AudioSource（编辑器兼容性）
            if (audioSource == null)
            {
                GameObject audioGO = new GameObject("EditorAudioSource");
                audioSource = audioGO.AddComponent<AudioSource>();
                DontDestroyOnLoad(audioGO);
            }
            audioSource.clip = loadedAudioClip;
        }
        
        maxChartTime = loadedAudioClip.length * 1000f; // 转换为毫秒
        
        // 加载对应难度的谱面文件
        int difficulty = difficultyDropdown != null ? difficultyDropdown.value : 0;
        currentDifficulty = difficulty;
        LoadChartForDifficulty(difficulty);
        
        // 重新启动自动保存（因为音频文件已加载）
        StartAutoSave();
    }
    
    IEnumerator LoadAudioClip(string path)
    {
        string fileUrl = "file://" + path;
        AudioType audioType = GetAudioTypeFromPath(path);

        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUrl, audioType);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"加载音频失败: {www.error}");
            yield break;
        }

        loadedAudioClip = DownloadHandlerAudioClip.GetContent(www);
        while (loadedAudioClip.loadState != AudioDataLoadState.Loaded)
        {
            yield return null;
        }

        Debug.Log($"音频加载成功: {loadedAudioClip.name}, 长度: {loadedAudioClip.length}秒");
    }
    
    AudioType GetAudioTypeFromPath(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        return extension switch
        {
            ".mp3" => AudioType.MPEG,
            ".wav" => AudioType.WAV,
            ".ogg" => AudioType.OGGVORBIS,
            ".m4a" => AudioType.MPEG,
            ".aac" => AudioType.MPEG,
            _ => AudioType.UNKNOWN
        };
    }
    
    void LoadChartForDifficulty(int difficulty)
    {
        if (string.IsNullOrEmpty(audioFilePath))
        {
            Debug.LogWarning("未选择音频文件");
            return;
        }
        
        // 获取音频文件所在目录
        string audioDirectory = Path.GetDirectoryName(audioFilePath);
        string chartFileName = $"chart_{difficulty}.txt";
        string chartPath = Path.Combine(audioDirectory, chartFileName);
        
        if (!File.Exists(chartPath))
        {
            Debug.LogWarning($"谱面文件不存在: {chartPath}，将创建新谱面");
            CreateNewChart();
            return;
        }
        
        try
        {
            string chartData = File.ReadAllText(chartPath);
            currentChart = Util.GetChart(chartData);
            
            // 从导入的谱面中获取初始BPM
            LoadInitialBPMFromChart();
            
            // 重置预览状态
            ClearPreviewNotes();
            currentTime = 0f;
            nextNoteIndex = 0;
            
            RefreshPreview();
            Debug.Log($"导入谱面成功: {currentChart.notes.Count} notes, {currentChart.events.Count} events, 难度: {difficulty}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"导入谱面失败: {e.Message}");
        }
    }
    
    void LoadInitialBPMFromChart()
    {
        if (currentChart.events == null || currentChart.events.Count == 0)
            return;
        
        ChartEvent initialBPM = currentChart.events.Find(e => e.type == "bpm" && e.timeStamp == 0);
        if (initialBPM != null &&
            float.TryParse(initialBPM.data, NumberStyles.Float, CultureInfo.InvariantCulture, out float beatLength))
        {
            currentBPM = 60000f / beatLength;
            if (bpmInput != null) bpmInput.text = currentBPM.ToString();
        }
    }
    
    void ExportChart()
    {
        if (string.IsNullOrEmpty(audioFilePath))
        {
            Debug.LogWarning("未选择音频文件，无法确定保存路径");
            return;
        }
        
        int difficulty = difficultyDropdown != null ? difficultyDropdown.value : 0;
        SaveChartForDifficulty(difficulty);
    }
    
    // 保存指定难度的谱面（通用保存方法）
    void SaveChartForDifficulty(int difficulty)
    {
        if (string.IsNullOrEmpty(audioFilePath))
        {
            Debug.LogWarning("未选择音频文件，无法确定保存路径");
            return;
        }
        
        string audioDirectory = Path.GetDirectoryName(audioFilePath);
        string chartFileName = $"chart_{difficulty}.txt";
        string chartPath = Path.Combine(audioDirectory, chartFileName);
        
        try
        {
            string chartData = ConvertChartToText();
            
            // 确保目录存在
            if (!Directory.Exists(audioDirectory))
            {
                Directory.CreateDirectory(audioDirectory);
            }
            
            File.WriteAllText(chartPath, chartData);
            Debug.Log($"保存谱面成功: {chartPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存谱面失败: {e.Message}");
        }
    }
    
    // 保存当前难度的谱面
    void SaveChartForCurrentDifficulty()
    {
        int difficulty = difficultyDropdown != null ? difficultyDropdown.value : currentDifficulty;
        SaveChartForDifficulty(difficulty);
    }
    
    // 启动自动保存协程
    void StartAutoSave()
    {
        StopAutoSave(); // 先停止现有的协程（如果有）
        if (!string.IsNullOrEmpty(audioFilePath))
        {
            autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
        }
    }
    
    // 停止自动保存协程
    void StopAutoSave()
    {
        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
        }
    }
    
    // 自动保存协程
    IEnumerator AutoSaveCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(AUTO_SAVE_INTERVAL);
            
            // 只在有音频文件且谱面不为空时自动保存
            if (!string.IsNullOrEmpty(audioFilePath) && currentChart != null && currentChart.notes.Count > 0)
            {
                SaveChartForCurrentDifficulty();
            }
        }
    }
    
    string ConvertChartToText()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // 添加头部信息
        sb.AppendLine("[General]");
        sb.AppendLine($"AudioFilename: track.mp3");
        sb.AppendLine($"AudioLeadIn: 0");
        sb.AppendLine("");
        
        // 添加TimingPoints
        sb.AppendLine("[TimingPoints]");
        foreach (ChartEvent ev in currentChart.events)
        {
            if (ev.type == "bpm")
            {
                sb.AppendLine($"{ev.timeStamp.ToString(CultureInfo.InvariantCulture)},{ev.data},4,1,0,100,1,0");
            }
        }
        sb.AppendLine("");
        
        // 添加HitObjects
        sb.AppendLine("[HitObjects]");
        foreach (Note note in currentChart.notes)
        {
            Vector2 topLeftPos = Values.CellCenterLocal(note.cellIndex.x, note.cellIndex.y);
            // 转换回顶左坐标系
            Vector2 cellSize = Values.CellSize();
            float x = (note.cellIndex.x + 0.5f) * cellSize.x;
            float y = (note.cellIndex.y + 0.5f) * cellSize.y;
            
            int noteType = note.type switch
            {
                NoteType.Tap => 1,
                NoteType.Drag => 2,
                NoteType.Block => 4,
                _ => 1
            };
            
            sb.AppendLine($"{x:F0},{y:F0},{note.timeStamp.ToString(CultureInfo.InvariantCulture)},0,{noteType}");
        }
        
        return sb.ToString();
    }
}