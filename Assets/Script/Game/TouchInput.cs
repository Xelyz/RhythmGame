using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchInput : MonoBehaviour
{
    List<Note> notes;
    public JudgeCenter judgeCenter;

    int tap = 0;
    int touch = 0;
    Vector2 cursorPos = new();

    List<int> judgmentQueue = new();
    internal List<int> holdJudgmentQueue = new();

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
        JudgeHolding();
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
            if (notes[pointer].noteType == NoteType.Slide)
            {
                holdJudgmentQueue.Add(pointer);
            }

            pointer++;
        }
    }

    private void JudgeNotes()
    {
        int time = GameManager.Instance.currentTime;
        float judgeRadiusSquared = Values.TapJudgeRadius * Values.TapJudgeRadius;

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

            judgeCenter.UpdateStat(Judgment.Miss);
            judgeCenter.Show(Judgment.Miss);
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

            bool isJudged = false;
            Judgment judgment = Judgment.Miss;

            if (timeDifference < 0)
            {
                note.FadeOut();
            }

            if ((note.noteType == NoteType.Tap && tap <= 0) ||
            (note.noteType == NoteType.Drag && touch <= 0))
            {

            }
            else if ((note.position - curPos).sqrMagnitude < judgeRadiusSquared)
            {
                if (note.noteType == NoteType.Drag)
                {
                    if (judgeCenter.Judge(timeDifference) != Judgment.Bad)
                    {
                        isJudged = true;

                        judgment = Judgment.Perfect;
                        note.PopOut();
                    }
                }
                if (note.noteType == NoteType.Tap)
                {
                    isJudged = true;
                    judgment = judgeCenter.Judge(timeDifference);
                    tap--;

                    note.PopOut();
                }
            }

            if (isJudged)
            {
                removeList.Add(i);
                judgeCenter.UpdateStat(judgment);
                judgeCenter.Show(judgment);
            }
        }

        for (int i = removeList.Count - 1; i >= 0; i--)
        {
            judgmentQueue.RemoveAt(removeList[i]);
        }
    }

    private void JudgeHolding()
    {
        int time = GameManager.Instance.currentTime;
        float holdingRadiusSquared = Values.HoldingRadius * Values.HoldingRadius;
        Vector2 curPos = cursorPos; // 缓存光标位置
        bool hasTouch = touch > 0;  // 缓存触摸状态
        int count = holdJudgmentQueue.Count;

        for (int i = 0; i < count;)
        {
            int n = holdJudgmentQueue[i];
            Slide note = notes[n] as Slide;

            if (note.timeStamp > time)
            {
                i++;
                continue;
            }

            note.UpdatePosition(time, out Vector2 position);
            float stop = note.timeStamp + note.duration;
            bool isHolding = false;

            // 只在有触摸时计算距离
            if (hasTouch)
            {
                float sqrDist = (position - curPos).sqrMagnitude;
                isHolding = sqrDist < holdingRadiusSquared;
            }

            if (time >= stop)
            {
                Judgment judgment = isHolding ? Judgment.Perfect : Judgment.Miss;
                judgeCenter.UpdateStat(judgment);
                judgeCenter.Show(judgment);
                note.PopOut();
                holdJudgmentQueue.RemoveAt(i);
                count--;
                continue;
            }

            // Update judgment periodically based on the note's beat interval
            if ((time - note.timeStamp) > note.beatInterval * note.tick)
            {
                Judgment judgment = isHolding ? Judgment.Perfect : Judgment.Miss;
                judgeCenter.UpdateStat(judgment);
                judgeCenter.Show(judgment);
                note.tick++;
            }

            i++;
        }
    }

    private void ProcessTouch()
    {
        foreach (Touch finger in Touch.activeTouches)
        {
            touch += 1;
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
        if (Keyboard.current.zKey.isPressed || Keyboard.current.xKey.isPressed)
        {
            touch += 1;
        }
    }

    private void InitializeInputData()
    {
        tap = 0;
        touch = 0;
    }
}
