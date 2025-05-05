using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchInput : MonoBehaviour
{
    List<Note> notes;
    public JudgeCenter judgeCenter;
    List<ShakeEffect> shakeEffects;

    int tap = 0;
    Vector2 cursorPos = new();

    public List<int> judgmentQueue = new();

    int pointer = 0;

    internal static TouchInput Instance;

    // 添加缓存变量
    private readonly List<int> removeIndices = new();
    private readonly float judgeRadiusSquared = Values.TapJudgeRadius * Values.TapJudgeRadius;
    private readonly float blockRadiusSquared = Values.tapRadius * Values.tapRadius;

    void Awake()
    {
        Instance = this;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        notes = GameManager.Instance.notes;
        shakeEffects = FindObjectsByType<ShakeEffect>(FindObjectsSortMode.None).ToList();
    }

    // Update is called once per frame
    void Update()
    {
        InitializeInputData();

        cursorPos = DigitalLevel.Instance.GetPosition();
        if (Values.accAvail)
        {
            ProcessTouch();
        }
        else
        {
            ProcessMouse();
        }

        if (GameManager.Instance.gameState.IsPlaying)
        {
            UpdateJudgmentQueue();
            JudgeNotes();
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
        ProcessMissedNotes(time);
        
        if (judgmentQueue.Count == 0) return;
        
        ProcessActiveNotes(time);
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

    private void ProcessActiveNotes(int time)
    {
        removeIndices.Clear();
        Vector2 curPos = cursorPos;

        for (int i = 0; i < judgmentQueue.Count; i++)
        {
            Note note = notes[judgmentQueue[i]];
            float distanceSquared = (note.position - curPos).sqrMagnitude;
            
            bool shouldRemove = false;
            switch (note.type)
            {
                case NoteType.Block:
                    shouldRemove = ProcessBlockNote(note, time, distanceSquared);
                    break;
                    
                case NoteType.Tap:
                    if (tap > 0)
                    {
                        shouldRemove = ProcessTapNote(note, time, distanceSquared);
                        if (shouldRemove) tap--;
                    }
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

    private bool ProcessTapNote(Note note, int time, float distanceSquared)
    {
        if (distanceSquared >= judgeRadiusSquared) return false;
        
        Judgment judgment = judgeCenter.Judge(note.timeStamp - time);
        JudgeFeedback(judgment, note);
        return true;
    }

    private bool ProcessDragNote(Note note, int time, float distanceSquared)
    {
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

    private void ProcessTouch()
    {
        foreach (Touch finger in Touch.activeTouches)
        {
            if (finger.began)
            {
                tap += 1;
            }
        }
    }

    private void ProcessMouse()
    {
        if (Keyboard.current.zKey.wasPressedThisFrame || Keyboard.current.xKey.wasPressedThisFrame)
        {
            tap += 1;
        }
    }

    private void InitializeInputData()
    {
        tap = 0;
    }
}
