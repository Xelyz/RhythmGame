using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NoteJudge : MonoBehaviour
{
    [Header("Judgements related")]
    public JudgeCenter judgeCenter;
    
    private List<Note> notes;
    private List<ShakeEffect> shakeEffects;
    private List<int> judgmentQueue = new();
    
    private int pointer = 0;
    
    // Cache variables
    private readonly List<int> removeIndices = new();
    // 基于网格判定，不再使用半径
    
    public static NoteJudge Instance { get; private set; }
    private readonly System.Collections.Generic.HashSet<int> scheduledAutoTap = new();
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        notes = GameManager.Instance.notes;
        shakeEffects = FindObjectsByType<ShakeEffect>(FindObjectsSortMode.None).ToList();
        
        // 订阅输入事件
        InputEvent.OnInput += HandleInput;
    }
    
    void OnDestroy()
    {
        // 取消订阅输入事件
        InputEvent.OnInput -= HandleInput;
    }
    
    void Update()
    {
        if (GameManager.Instance.gameState.IsPlaying)
        {
            UpdateJudgmentQueue();
            JudgeNotes();
            if (PlayInfo.isAutoplay)
            {
                AutoplayTick();
            }
        }
    }
    
    /// <summary>
    /// 处理输入事件
    /// </summary>
    /// <param name="inputData">输入数据</param>
    private void HandleInput(InputEventData inputData)
    {
        if (inputData.tapCount > 0 && judgmentQueue.Count > 0)
        {
            ProcessTapInput(inputData.tapCount, inputData.cursorPosition);
        }
    }
    
    private void UpdateJudgmentQueue()
    {
        float time = GameManager.Instance.gameState.CurrentTime;

        while (pointer < notes.Count)
        {
            float timeDifference = notes[pointer].timeStamp - time;

            if (timeDifference > Values.badWindow)
            {
                break;
            }

            judgmentQueue.Add(pointer);
            pointer++;
        }
    }

    private void JudgeNotes()
    {
        if (judgmentQueue.Count == 0) return;
        
        float time = GameManager.Instance.gameState.CurrentTime;
        Vector2 cursorPosition = DigitalLevel.Instance.GetPosition();
        
        ProcessMissedNotes(time);
        
        if (judgmentQueue.Count == 0) return;
        
        ProcessActiveNotes(time, cursorPosition);
    }

    private void AutoplayTick()
    {
        if (judgmentQueue.Count == 0) return;
        float time = GameManager.Instance.gameState.CurrentTime;

        // 确保有目标位置 - 如果还没有设置目标，进行初始移动
        if (!DigitalLevel.Instance.HasAutoplayTarget())
        {
            AutoplayMoveToNext();
        }

        // 选择下一个可操作音符进行点击
        int idx = -1;
        for (int i = 0; i < judgmentQueue.Count; i++)
        {
            Note cand = notes[judgmentQueue[i]];
            if (cand.type != NoteType.Block) { idx = judgmentQueue[i]; break; }
        }
        if (idx == -1 && judgmentQueue.Count > 0) idx = judgmentQueue[0];
        if (idx == -1) return;
        
        Note next = notes[idx];

        // 模拟点击：严格在音符时间点触发（若延误则立刻补点），避免重复
        if (next.type == NoteType.Tap && !scheduledAutoTap.Contains(idx))
        {
            float dt = next.timeStamp - time; // ms
            if (dt > 0f)
            {
                scheduledAutoTap.Add(idx);
                StartCoroutine(Util.DelayAction(() =>
                {
                    Vector2 pos = DigitalLevel.Instance.GetPosition();
                    if (Values.gridDebugLog)
                    {
                        Debug.Log($"[AUTO] Timed tap at {next.timeStamp:F1}ms noteIndex={idx}");
                    }
                    InputEvent.TriggerInput(new InputEventData(1, pos));
                }, dt / 1000f));
            }
            else
            {
                // 已经过时，立即补点一次
                scheduledAutoTap.Add(idx);
                Vector2 pos = DigitalLevel.Instance.GetPosition();
                if (Values.gridDebugLog)
                {
                    Debug.Log($"[AUTO] Late tap immediately dt={dt:F1}ms noteIndex={idx}");
                }
                InputEvent.TriggerInput(new InputEventData(1, pos));
            }
        }
    }

    /// <summary>
    /// 立即移动到下一个目标音符，基于时间和距离计算移动速度
    /// </summary>
    private void AutoplayMoveToNext()
    {
        if (!PlayInfo.isAutoplay) return;
        
        float currentTime = GameManager.Instance.gameState.CurrentTime;
        Vector2 cursor = DigitalLevel.Instance.GetPosition();
        Vector2Int cursorCell = Values.LocalToCellIndex(cursor);
        
        // 查找下一个目标音符
        Note targetNote = FindNextTargetNote(currentTime);
        if (targetNote == null) return;

        Vector2Int targetCell = DetermineTargetCell(targetNote, cursorCell, currentTime);
        Vector2 targetPosition = Values.CellCenterLocal(targetCell.x, targetCell.y);
        
        // 直接使用游戏时间（毫秒）作为目标时间
        float targetTimeMs = targetNote.timeStamp;
        
        // 提前一点点时间到达，确保不会迟到
        const float arrivalMarginMs = 50f; // 提前50ms到达
        targetTimeMs -= arrivalMarginMs;
        
        DigitalLevel.Instance.EnableAutoplayControl(true);
        DigitalLevel.Instance.SetAutoplayTarget(targetPosition, targetTimeMs);
        
        if (Values.gridDebugLog)
        {
            float distance = Vector2.Distance(cursor, targetPosition);
            float moveTimeMs = targetTimeMs - currentTime;
            Debug.Log($"[AUTO] Move to {targetCell} for note at {targetNote.timeStamp:F1}ms, " +
                     $"distance={distance:F1}, moveTimeMs={moveTimeMs:F1}ms");
        }
    }

    /// <summary>
    /// 查找下一个目标音符
    /// </summary>
    private Note FindNextTargetNote(float currentTime)
    {
        // 优先从judgment queue中找
        foreach (int idx in judgmentQueue)
        {
            Note note = notes[idx];
            if (note.type != NoteType.Block)
            {
                return note;
            }
        }
        
        // 如果judgment queue中没有合适的，查看更远的音符
        const float lookAheadWindow = 1000f; // 提前1000ms查看
        for (int i = pointer; i < notes.Count; i++)
        {
            Note note = notes[i];
            if (note.timeStamp > currentTime + lookAheadWindow) break;
            if (note.type != NoteType.Block)
            {
                return note;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 确定目标格子，考虑block回避
    /// </summary>
    private Vector2Int DetermineTargetCell(Note targetNote, Vector2Int cursorCell, float currentTime)
    {
        Vector2Int desiredCell = targetNote.cellIndex;
        Vector2Int targetCell = desiredCell;
        
        // 检查是否需要回避block
        const float blockAvoidWindow = 90f; // ms
        bool willBeHitByBlock = false;
        
        // 检查当前光标位置是否会被block击中
        for (int i = 0; i < judgmentQueue.Count; i++)
        {
            Note n = notes[judgmentQueue[i]];
            if (n.type != NoteType.Block) continue;
            if (Mathf.Abs(n.timeStamp - currentTime) <= blockAvoidWindow)
            {
                if (n.cellIndex == cursorCell)
                {
                    willBeHitByBlock = true;
                    break;
                }
            }
        }

        if (willBeHitByBlock)
        {
            // 检查目标位置是否也不安全
            bool targetUnsafe = false;
            for (int i = 0; i < judgmentQueue.Count; i++)
            {
                Note n = notes[judgmentQueue[i]];
                if (n.type == NoteType.Block && Mathf.Abs(n.timeStamp - currentTime) <= blockAvoidWindow && n.cellIndex == targetCell)
                {
                    targetUnsafe = true;
                    break;
                }
            }

            if (targetUnsafe)
            {
                // 四邻探索：右、左、下、上
                Vector2Int[] neighbors = new Vector2Int[] {
                    new Vector2Int(Mathf.Min(cursorCell.x + 1, Values.gridColumns - 1), cursorCell.y),
                    new Vector2Int(Mathf.Max(cursorCell.x - 1, 0), cursorCell.y),
                    new Vector2Int(cursorCell.x, Mathf.Min(cursorCell.y + 1, Values.gridRows - 1)),
                    new Vector2Int(cursorCell.x, Mathf.Max(cursorCell.y - 1, 0)),
                };
                foreach (var cand in neighbors)
                {
                    bool safe = true;
                    for (int i = 0; i < judgmentQueue.Count; i++)
                    {
                        Note n = notes[judgmentQueue[i]];
                        if (n.type == NoteType.Block && Mathf.Abs(n.timeStamp - currentTime) <= blockAvoidWindow && n.cellIndex == cand)
                        {
                            safe = false; break;
                        }
                    }
                    if (safe) { targetCell = cand; break; }
                }
            }
        }

        return targetCell;
    }



    private void ProcessMissedNotes(float time)
    {
        int missCount = 0;
        foreach (int index in judgmentQueue)
        {
            if (notes[index].timeStamp - time >= -Values.badWindow) break;
            missCount++;
            JudgeFeedback(Judgment.Miss, null);
        }
        
        if (missCount > 0)
        {
            judgmentQueue.RemoveRange(0, missCount);
        }
    }

    private void ProcessActiveNotes(float time, Vector2 cursorPosition)
    {
        removeIndices.Clear();
        Vector2Int cursorCell = Values.LocalToCellIndex(cursorPosition);

        for (int i = 0; i < judgmentQueue.Count; i++)
        {
            Note note = notes[judgmentQueue[i]];
            bool sameCell = note.cellIndex.x == cursorCell.x && note.cellIndex.y == cursorCell.y;
            
            // 统一处理FadeOut
            if (note.timeStamp - time <= 0f)
            {
                note.FadeOut();
            }
            
            bool shouldRemove = false;
            switch (note.type)
            {
                case NoteType.Block:
                    shouldRemove = ProcessBlockNote(note, time, sameCell);
                    break;
                    
                case NoteType.Drag:
                    shouldRemove = ProcessDragNote(note, time, sameCell);
                    break;
            }
            
            if (shouldRemove)
            {
                removeIndices.Add(i);
            }
        }

        // 从后向前移除，避免索引变化
        for (int i = removeIndices.Count - 1; i >= 0; i--)
        {
            judgmentQueue.RemoveAt(removeIndices[i]);
        }
    }
    
    private void ProcessTapInput(int tapCount, Vector2 cursorPosition)
    {
        float time = GameManager.Instance.gameState.CurrentTime;
        int remainingTaps = tapCount;
        
        removeIndices.Clear();
        Vector2Int cursorCell = Values.LocalToCellIndex(cursorPosition);

        for (int i = 0; i < judgmentQueue.Count && remainingTaps > 0; i++)
        {
            Note note = notes[judgmentQueue[i]];
            
            if (note.type != NoteType.Tap) continue;
            
            bool sameCell = note.cellIndex.x == cursorCell.x && note.cellIndex.y == cursorCell.y;
            
            if (ProcessTapNote(note, time, sameCell))
            {
                remainingTaps--;
                removeIndices.Add(i);
            }
        }

        // 从后向前移除，避免索引变化
        for (int i = removeIndices.Count - 1; i >= 0; i--)
        {
            judgmentQueue.RemoveAt(removeIndices[i]);
        }
    }

    private bool ProcessTapNote(Note note, float time, bool sameCell)
    {
        if (!sameCell) return false;
        
        float timeDifference = note.timeStamp - time;
        Judgment judgment = judgeCenter.Judge(timeDifference);
        JudgeFeedback(judgment, note);

        // Autoplay: 在tap的正确判定时间点才开始移动到下一个目标，和drag保持一致
        if (PlayInfo.isAutoplay)
        {
            if (timeDifference > 0)
            {
                // 如果tap还没到时间，等到正确的时间点才移动
                StartCoroutine(Util.DelayAction(() => {
                    AutoplayMoveToNext();
                }, timeDifference / 1000f));
            }
            else
            {
                // 已经到时间或过时，立即移动
                AutoplayMoveToNext();
            }
        }

        return true;
    }

    private bool ProcessBlockNote(Note note, float time, bool sameCell)
    {
        if (note.timeStamp - time >= 0f) return false;
        
        if (sameCell)
        {
            JudgeFeedback(Judgment.Miss, null);
            shakeEffects.ForEach(x => x.TriggerShake());
        }
        else
        {
            JudgeFeedback(Judgment.Perfect, null);
        }
        
        // Autoplay: 处理完block后立即移动到下一个目标
        if (PlayInfo.isAutoplay)
        {
            AutoplayMoveToNext();
        }
        
        return true;
    }

    private bool ProcessDragNote(Note note, float time, bool sameCell)
    {
        if (!sameCell) return false;
        
        float timeDifference = note.timeStamp - time;
        if (judgeCenter.Judge(timeDifference) == Judgment.Bad) return false;

        if (timeDifference > 0)
        {
            StartCoroutine(Util.DelayAction(() => {
                JudgeFeedback(Judgment.Perfect, note);
                // Autoplay: 在drag判定完成后立即移动到下一个目标
                if (PlayInfo.isAutoplay)
                {
                    AutoplayMoveToNext();
                }
            }, timeDifference / 1000f));
        }
        else
        {
            JudgeFeedback(Judgment.Perfect, note);
            // Autoplay: 处理完drag后立即移动到下一个目标
            if (PlayInfo.isAutoplay)
            {
                AutoplayMoveToNext();
            }
        }
        
        return true;
    }

    private void JudgeFeedback(Judgment judgment, Note note)
    {
        judgeCenter.UpdateStat(judgment);
        judgeCenter.Show(judgment);

        if (judgment != Judgment.Miss && note != null)
            AudioManager.Instance.PlayEffect("Hit");

        note?.PopOut();
    }

    public bool IsJudging()
    {
        return judgmentQueue.Count > 0;
    }
} 