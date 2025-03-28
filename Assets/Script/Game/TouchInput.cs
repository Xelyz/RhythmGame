using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchInput : MonoBehaviour
{
    List<Note> notes;
    public JudgeCenter judgeCenter;

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

        UpdateJudgmentQueue();
        JudgeNotes();
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
        int time = GameManager.Instance.currentTime;
        float judgeRadiusSquared = Values.TapJudgeRadius * Values.TapJudgeRadius;
        float blockRadiusSquared = Values.TapRadius * Values.TapRadius;

        // 使用数组而不是List来存储判定队列,减少内存分配
        int queueCount = judgmentQueue.Count;
        if (queueCount == 0) return;

        // 批量处理Miss判定
        int missCount = 0;
        int firstValidIndex = 0;
        for (; firstValidIndex < queueCount; firstValidIndex++)
        {
            Note note = notes[judgmentQueue[firstValidIndex]];
            if (note.timeStamp - time >= -Values.badWindow) break;
            missCount++;

            JudgeFeedback(Judgment.Miss, null);
        }

        if (missCount > 0)
        {
            judgmentQueue.RemoveRange(0, missCount);
            queueCount -= missCount;
            if (queueCount == 0) return;
        }

        // 缓存常用变量
        Vector2 curPos = cursorPos;

        List<int> removeList = new();

        for (int i = 0; i < queueCount; i++)
        {
            int n = judgmentQueue[i];
            Note note = notes[n];
            int timeDifference = note.timeStamp - time;

            Judgment judgment = Judgment.Miss;

            if (timeDifference < 0)
            {
                note.FadeOut();

                if (note.type == NoteType.Block)
                {
                    if ((note.position - curPos).sqrMagnitude < blockRadiusSquared)
                        judgment = Judgment.Miss;
                        // some effects here
                    else
                        judgment = Judgment.Perfect;

                    removeList.Add(i);

                    JudgeFeedback(judgment, null);
                }
            }

            if (note.type == NoteType.Tap && tap <= 0)
            {
                continue;
            }

            if ((note.position - curPos).sqrMagnitude < judgeRadiusSquared)
            {
                if (note.type == NoteType.Drag)
                {
                    if (judgeCenter.Judge(timeDifference) != Judgment.Bad)
                    {
                        judgment = Judgment.Perfect;
                        removeList.Add(i);

                        if (timeDifference > 0)
                            StartCoroutine(Util.DelayAction(() => JudgeFeedback(judgment, note), timeDifference / 1000f));
                        else
                            JudgeFeedback(judgment, note);
                    }
                }
                if (note.type == NoteType.Tap)
                {
                    judgment = judgeCenter.Judge(timeDifference);
                    tap--;
                    removeList.Add(i);

                    JudgeFeedback(judgment, note);
                }
            }

        }

        for (int i = removeList.Count - 1; i >= 0; i--)
        {
            judgmentQueue.RemoveAt(removeList[i]);
        }
    }

    void JudgeFeedback(Judgment judgment, Note note)
    {
        judgeCenter.UpdateStat(judgment);
        judgeCenter.Show(judgment);

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
