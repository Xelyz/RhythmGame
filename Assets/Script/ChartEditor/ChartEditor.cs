using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Globalization;
using UnityEngine.InputSystem;

public class ChartEditor : MonoBehaviour
{
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
    
    [Header("Preview Settings")]
    public Transform noteHolder;
    
    [Header("Timeline Control")]
    public bool snapTo16thBeat = true;
    public float currentTime = 0f; // 当前时间（毫秒）
    public float currentBPM = 120f;
    public float offset = 0f;
    
    // Chart data
    private Chart currentChart;
    private List<Note> editorNotes = new List<Note>();
    private NoteType selectedNoteType = NoteType.Tap;
    private bool isPlaying = false;
    private bool isPaused = false;
    
    // Audio
    private AudioSource audioSource;
    private AudioManager audioManager;
    
    // Grid and input
    private bool isDraggingTimeline = false;
    private Vector2 lastMousePosition;
    
    // Input System
    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActionAsset;
    private InputActionMap chartEditorMap;
    private InputAction leftClickAction;
    private InputAction rightClickAction;
    private InputAction rightHoldAction;
    private InputAction mousePositionAction;
    private InputAction togglePlaybackAction;
    private Vector2 mousePosition;
    
    public static ChartEditor Instance { get; private set; }
    
    void Awake()
    {
        Instance = this;
        InitializeEditor();
        InitializeInputSystem();
    }
    
    void OnDestroy()
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
        SetupUI();
        CreateNewChart();
        UpdateTimelineDisplay();
    }
    
    void Update()
    {
        // 更新鼠标位置
        if (mousePositionAction != null)
        {
            mousePosition = mousePositionAction.ReadValue<Vector2>();
        }
        
        // 处理拖拽时间轴
        if (isDraggingTimeline)
        {
            HandleTimelineDrag();
        }
        
        if (isPlaying && !isPaused)
        {
            UpdatePlayback();
        }
        UpdateTimelineDisplay();
    }
    
    void InitializeEditor()
    {
        currentChart = new Chart
        {
            notes = new List<Note>(),
            events = new List<ChartEvent>()
        };
        
        // 确保AudioManager存在
        audioManager = FindAnyObjectByType<AudioManager>();
        if (audioManager == null)
        {
            GameObject audioGO = GameObject.Find("AudioManager");
            if (audioGO != null)
            {
                audioSource = audioGO.GetComponent<AudioSource>();
            }
        }
        else
        {
            audioSource = audioManager.musicSource;
        }
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
        importButton?.onClick.AddListener(ImportChart);
        exportButton?.onClick.AddListener(ExportChart);
        newChartButton?.onClick.AddListener(CreateNewChart);
        
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
    
    void SetButtonText(Button button, string text)
    {
        if (button != null)
        {
            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = text;
                buttonText.fontSize = 14;
                buttonText.color = Color.black;
                buttonText.alignment = TextAnchor.MiddleCenter;
            }
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
        float deltaY = (currentMousePosition.y - lastMousePosition.y) * 0.5f; // 调整灵敏度
        
        currentTime += deltaY;
        
        // 16分音符对齐
        if (snapTo16thBeat)
        {
            float beatLength = 60000f / currentBPM; // 一拍的毫秒数
            float sixteenthBeat = beatLength / 4f; // 16分音符长度
            currentTime = Mathf.Round(currentTime / sixteenthBeat) * sixteenthBeat;
        }
        
        currentTime = Mathf.Max(0, currentTime);
        lastMousePosition = currentMousePosition;
        
        UpdatePreviewAtTime(currentTime);
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
        const float timeThreshold = 50f; // 50ms的时间容差
        
        foreach (Note note in currentChart.notes)
        {
            if (Mathf.Abs(note.timeStamp - time) <= timeThreshold && 
                note.cellIndex.x == gridPos.x && note.cellIndex.y == gridPos.y)
            {
                return note;
            }
        }
        return null;
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
        // 按时间排序
        currentChart.notes.Sort((a, b) => a.timeStamp.CompareTo(b.timeStamp));
        
        // 重新分配nthNote
        for (int i = 0; i < currentChart.notes.Count; i++)
        {
            currentChart.notes[i].nthNote = i + 1;
        }
    }
    
    void RemoveNote(Note note)
    {
        if (currentChart.notes.Contains(note))
        {
            note.Release(); // 清理GameObject
            currentChart.notes.Remove(note);
            
            // 重新分配nthNote
            for (int i = 0; i < currentChart.notes.Count; i++)
            {
                currentChart.notes[i].nthNote = i + 1;
            }
        }
    }
    
    void SelectNoteType(NoteType type)
    {
        selectedNoteType = type;
        
        // 更新按钮高亮显示
        if (tapNoteButton != null) tapNoteButton.GetComponent<Image>().color = type == NoteType.Tap ? Color.yellow : Color.white;
        if (dragNoteButton != null) dragNoteButton.GetComponent<Image>().color = type == NoteType.Drag ? Color.yellow : Color.white;
        if (blockNoteButton != null) blockNoteButton.GetComponent<Image>().color = type == NoteType.Block ? Color.yellow : Color.white;
        
        Debug.Log($"选择note类型: {type}");
    }
    
    void TogglePlayback()
    {
        if (isPlaying)
        {
            // 暂停播放
            audioSource?.Pause();
            isPlaying = false;
            isPaused = true;
            PlayInfo.isAutoplay = false;
            
            Debug.Log("暂停播放");
        }
        else
        {
            // 开始/恢复播放
            if (audioSource != null && audioSource.clip != null)
            {
                if (isPaused)
                {
                    // 恢复播放
                    audioSource.UnPause();
                    Debug.Log("恢复播放");
                }
                else
                {
                    // 从当前时间开始播放
                    audioSource.time = (currentTime + offset) / 1000f; // 转换为秒
                    audioSource.Play();
                    Debug.Log("开始播放谱面预览");
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
        if (audioSource != null && audioSource.isPlaying)
        {
            currentTime = audioSource.time * 1000f - offset; // 转换为毫秒并应用offset
            UpdatePreviewAtTime(currentTime);
        }
    }
    
    void UpdatePreviewAtTime(float time)
    {
        // 清理所有现有的note显示
        ClearPreviewNotes();
        
        // 显示当前时间点附近的notes
        const float previewWindow = 2000f; // 显示前后2秒的notes
        
        foreach (Note note in currentChart.notes)
        {
            if (note.timeStamp >= time && note.timeStamp <= time + previewWindow)
            {
                // 在编辑器中显示note（静态显示，不需要动画）
                ShowNoteInPreview(note, time);
            }
        }
    }
    
    void ShowNoteInPreview(Note note, float currentTime)
    {
        // 这里需要实现note的可视化显示
        // 可以创建简单的UI图标或使用游戏中的note prefab
        // 暂时用Debug可视化替代
        if (Mathf.Abs(note.timeStamp - currentTime) < 100f) // 当前时间点的notes高亮显示
        {
            Debug.Log($"当前时间点note: {note.type} at ({note.cellIndex.x},{note.cellIndex.y})");
        }
    }
    
    void ClearPreviewNotes()
    {
        // 清理所有预览中的note显示
        foreach (Note note in editorNotes)
        {
            note.Release();
        }
        editorNotes.Clear();
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
            float beatLength = 60000f / currentBPM;
            float currentBeat = currentTime / beatLength;
            currentBeatText.text = $"拍子: {currentBeat:F2}";
        }
        
        if (timelineSlider != null && !isDraggingTimeline)
        {
            // 假设最大时间为10分钟
            timelineSlider.value = currentTime / 600000f;
        }
    }
    
    void OnTimelineChanged(float value)
    {
        if (!isDraggingTimeline && !isPlaying)
        {
            currentTime = value * 600000f; // 最大10分钟
            if (snapTo16thBeat)
            {
                float beatLength = 60000f / currentBPM;
                float sixteenthBeat = beatLength / 4f;
                currentTime = Mathf.Round(currentTime / sixteenthBeat) * sixteenthBeat;
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
        
        // 查找并更新初始BPM事件
        ChartEvent bpmEvent = currentChart.events.Find(e => e.type == "bpm" && e.timeStamp == 0);
        if (bpmEvent == null)
        {
            bpmEvent = new ChartEvent
            {
                timeStamp = 0,
                type = "bpm",
                data = (60000f / currentBPM).ToString(CultureInfo.InvariantCulture)
            };
            currentChart.events.Add(bpmEvent);
        }
        else
        {
            bpmEvent.data = (60000f / currentBPM).ToString(CultureInfo.InvariantCulture);
        }
    }
    
    void CreateNewChart()
    {
        // 停止播放
        audioSource?.Stop();
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
        
        UpdateBPMInChart();
        RefreshPreview();
        
        Debug.Log("创建新谱面");
    }
    
    void ImportChart()
    {
        // 这里实现从txt文件导入谱面的逻辑
        string filePath = Application.streamingAssetsPath + "/charts/import.txt";
        
        if (File.Exists(filePath))
        {
            try
            {
                string chartData = File.ReadAllText(filePath);
                currentChart = Util.GetChart(chartData);
                currentTime = 0f;
                RefreshPreview();
                Debug.Log($"导入谱面成功: {currentChart.notes.Count} notes, {currentChart.events.Count} events");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"导入谱面失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"导入文件不存在: {filePath}");
        }
    }
    
    void ExportChart()
    {
        // 实现导出谱面到txt文件的逻辑
        string filePath = Application.streamingAssetsPath + "/charts/export.txt";
        
        try
        {
            string chartData = ConvertChartToText();
            
            // 确保目录存在
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, chartData);
            Debug.Log($"导出谱面成功: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"导出谱面失败: {e.Message}");
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