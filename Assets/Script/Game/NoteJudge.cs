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
    private readonly float judgeRadiusSquared = Values.TapJudgeRadius * Values.TapJudgeRadius;
    private readonly float blockRadiusSquared = Values.tapRadius * Values.tapRadius;
    
    public static NoteJudge Instance { get; private set; }
    
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
        int time = GameManager.Instance.gameState.CurrentTime;

        while (pointer < notes.Count)
        {
            int timeDifference = notes[pointer].timeStamp - time;

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
        
        int time = GameManager.Instance.gameState.CurrentTime;
        Vector2 cursorPosition = DigitalLevel.Instance.GetPosition();
        
        ProcessMissedNotes(time);
        
        if (judgmentQueue.Count == 0) return;
        
        ProcessActiveNotes(time, cursorPosition);
    }

    private void ProcessMissedNotes(int time)
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

    private void ProcessActiveNotes(int time, Vector2 cursorPosition)
    {
        removeIndices.Clear();

        for (int i = 0; i < judgmentQueue.Count; i++)
        {
            Note note = notes[judgmentQueue[i]];
            float distanceSquared = (note.position - cursorPosition).sqrMagnitude;
            
            bool shouldRemove = false;
            switch (note.type)
            {
                case NoteType.Block:
                    shouldRemove = ProcessBlockNote(note, time, distanceSquared);
                    break;
                    
                case NoteType.Drag:
                    shouldRemove = ProcessDragNote(note, time, distanceSquared);
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
        int time = GameManager.Instance.gameState.CurrentTime;
        int remainingTaps = tapCount;
        
        removeIndices.Clear();

        for (int i = 0; i < judgmentQueue.Count && remainingTaps > 0; i++)
        {
            Note note = notes[judgmentQueue[i]];
            
            if (note.type != NoteType.Tap) continue;
            
            float distanceSquared = (note.position - cursorPosition).sqrMagnitude;
            
            if (ProcessTapNote(note, time, distanceSquared))
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

    private bool ProcessTapNote(Note note, int time, float distanceSquared)
    {
        if (note.timeStamp - time <= 0) note.FadeOut();

        if (distanceSquared >= judgeRadiusSquared) return false;
        
        Judgment judgment = judgeCenter.Judge(note.timeStamp - time);
        JudgeFeedback(judgment, note);

        return true;
    }

    private bool ProcessBlockNote(Note note, int time, float distanceSquared)
    {
        if (note.timeStamp - time >= 0) return false;
        
        note.FadeOut();
        if (distanceSquared < blockRadiusSquared)
        {
            JudgeFeedback(Judgment.Miss, null);
            shakeEffects.ForEach(x => x.TriggerShake());
        }
        else
        {
            JudgeFeedback(Judgment.Perfect, null);
        }
        
        return true;
    }

    private bool ProcessDragNote(Note note, int time, float distanceSquared)
    {
        if (note.timeStamp - time <= 0) note.FadeOut();

        if (distanceSquared >= judgeRadiusSquared) return false;
        
        int timeDifference = note.timeStamp - time;
        if (judgeCenter.Judge(timeDifference) == Judgment.Bad) return false;

        if (timeDifference > 0)
        {
            StartCoroutine(Util.DelayAction(
                () => JudgeFeedback(Judgment.Perfect, note), 
                timeDifference / 1000f));
        }
        else
        {
            JudgeFeedback(Judgment.Perfect, note);
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