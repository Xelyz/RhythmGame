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

        if (GameManager.Instance.isGamePlaying)
        {
            UpdateJudgmentQueue();
            JudgeNotes();
        }
    }

    private void UpdateJudgmentQueue()
    {
        int time = GameManager.Instance.currentTime;

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
        
        int time = GameManager.Instance.currentTime;
        ProcessMissedNotes(time);
        
        if (judgmentQueue.Count == 0) return;
        
        ProcessActiveNotes(time);
    }

    private void ProcessMissedNotes(int time)
    {
        int missCount = judgmentQueue.TakeWhile(index => 
            notes[index].timeStamp - time < -Values.badWindow).Count();
            
        if (missCount > 0)
        {
            for (int i = 0; i < missCount; i++)
            {
                JudgeFeedback(Judgment.Miss, null);
            }
            judgmentQueue.RemoveRange(0, missCount);
        }
    }

    private void ProcessActiveNotes(int time)
    {
        var removeIndices = new List<int>();
        Vector2 curPos = cursorPos;
        float judgeRadiusSquared = Values.TapJudgeRadius * Values.TapJudgeRadius;

        for (int i = 0; i < judgmentQueue.Count; i++)
        {
            Note note = notes[judgmentQueue[i]];
            float distanceSquared = (note.position - curPos).sqrMagnitude;
            
            switch (note.type)
            {
                case NoteType.Block:
                    if (ProcessBlockNote(note, time, distanceSquared))
                        removeIndices.Add(i);
                    break;
                    
                case NoteType.Tap:
                    if (tap > 0 && ProcessTapNote(note, time, distanceSquared, judgeRadiusSquared))
                    {
                        removeIndices.Add(i);
                        tap--;
                    }
                    break;
                    
                case NoteType.Drag:
                    if (ProcessDragNote(note, time, distanceSquared, judgeRadiusSquared))
                        removeIndices.Add(i);
                    break;
            }
        }

        // Remove processed notes from back to front
        for (int i = removeIndices.Count - 1; i >= 0; i--)
        {
            judgmentQueue.RemoveAt(removeIndices[i]);
        }
    }

    private bool ProcessBlockNote(Note note, int time, float distanceSquared)
    {
        if (note.timeStamp - time >= 0) return false;
        
        note.FadeOut();
        float blockRadiusSquared = Values.TapRadius * Values.TapRadius;
        
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

    private bool ProcessTapNote(Note note, int time, float distanceSquared, float judgeRadiusSquared)
    {
        if (distanceSquared >= judgeRadiusSquared) return false;
        
        Judgment judgment = judgeCenter.Judge(note.timeStamp - time);
        JudgeFeedback(judgment, note);
        return true;
    }

    private bool ProcessDragNote(Note note, int time, float distanceSquared, float judgeRadiusSquared)
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
