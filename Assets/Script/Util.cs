using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.IO;
using System.Globalization;

public static class Util
{
    public static void Transition(string toScene)
    {
        Transfer.fromScene = SceneManager.GetActiveScene().name;
        Transfer.toScene = toScene;
        Transfer.sceneReady = false;
        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);

        AudioManager.Instance.ClearMusic();
    }

    public static IEnumerator DelayAction(Action action, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        action?.Invoke();
    }

    public static int Sign(float value)
    {
        return value > 0 ? 1 : value < 0 ? -1 : 0;
    }

    public static void SaveData()
    {
        try
        {
            // 使用 Newtonsoft.Json 序列化
            string json = JsonConvert.SerializeObject(Values.playerData, Formatting.Indented);
            File.WriteAllText(Values.SavePath, json);
            Debug.Log("Results saved to: " + Values.SavePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save results: " + e.Message);
        }
    }

    private static readonly int block = 1 << 2;
    private static readonly int drag = 1 << 1;

    struct NoteData
    {
        public int X, Y, Type;
        public float TimeStamp;
        public NoteData(string[] data)
        {
            X = int.Parse(data[0]);
            Y = int.Parse(data[1]);
            TimeStamp = float.Parse(data[2], CultureInfo.InvariantCulture);
            Type = int.Parse(data[4]);
        }
    }

    public static Chart GetChart(string chartData)
    {
        Chart chart = new()
        {
            notes = new(),
            events = new()
        };
        string[] lines = chartData.Split('\n');
        string reading = "";
        int n = 1;
        // parse all timing points as events (bpm only)
        foreach (string line in lines)
        {
            if (line == "") continue;
            if (line.StartsWith("[TimingPoints]"))
            {
                reading = "TimingPoints";
                continue;
            }
            if (line.StartsWith("[HitObjects]"))
            {
                reading = "HitObjects";
                continue;
            }
            if (line.StartsWith("["))
            {
                reading = "";
                continue;
            }
            if (reading == "TimingPoints")
            {
                // 读取每一行TimingPoints为事件。仅保留uninherited==1的BPM事件
                try
                {
                    string[] data = line.Split(',');
                    if (data.Length >= 7)
                    {
                        // data[6] == "1" 表示非继承节拍(BPM)
                        if (data[6].Trim() == "1")
                        {
                            float time = float.Parse(data[0], CultureInfo.InvariantCulture);
                            string beatLen = float.Parse(data[1], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                            ChartEvent ev = new()
                            {
                                timeStamp = time,
                                type = "bpm",
                                data = beatLen
                            };
                            chart.events.Add(ev);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse TimingPoints line: '{line}'. Error: {e.Message}");
                }
                continue;
            }
            if (reading == "HitObjects")
            {
                string[] data = line.Split(',');

                try
                {
                    NoteData noteData = new(data);

                    Note note;

                    if ((noteData.Type & block) == block)
                    {
                        note = new Block();
                    }
                    else if ((noteData.Type & drag) == drag)
                    {
                        note = new Drag();
                    }
                    else
                    {
                        note = new Tap();
                    }

                    // 将谱面顶左坐标量化到网格
                    Vector2Int cell = Values.TopLeftToCellIndex(new Vector2(noteData.X, noteData.Y));
                    note.cellIndex = cell;
                    note.position = Values.CellCenterLocal(cell.x, cell.y);
                    note.timeStamp = noteData.TimeStamp;
                    note.nthNote = n++;

                    chart.notes.Add(note);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse line: '{line}'. Error: {e.Message}");
                    continue; // 跳过错误行，继续处理下一行
                }
            }
        }
        return chart;
    }

    public static List<float> BuildBarlineTimestamps(List<ChartEvent> events, List<Note> notes)
    {
        List<float> result = new();
        if (events == null || events.Count == 0) return result;

        // 仅使用bpm事件，按时间排序
        List<(float t, float beatMs)> bpmEvents = new();
        foreach (var ev in events)
        {
            if (ev != null && ev.type == "bpm")
            {
                if (float.TryParse(ev.data, NumberStyles.Float, CultureInfo.InvariantCulture, out float beatMs))
                {
                    bpmEvents.Add((ev.timeStamp, beatMs));
                }
            }
        }
        if (bpmEvents.Count == 0) return result;
        bpmEvents.Sort((a, b) => a.t.CompareTo(b.t));

        // 计算结束时间（使用谱面中最后一个音符时间）
        float endTime = 0f;
        if (notes != null && notes.Count > 0)
        {
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i] != null && notes[i].timeStamp > endTime)
                {
                    endTime = notes[i].timeStamp;
                }
            }
        }

        endTime += 1000f;

        for (int i = 0; i < bpmEvents.Count; i++)
        {
            float segmentStart = bpmEvents[i].t;
            float beatMs = Mathf.Max(1f, bpmEvents[i].beatMs);
            float measureMs = beatMs * 4f;
            float segmentEnd = (i < bpmEvents.Count - 1) ? bpmEvents[i + 1].t : endTime;

            if (segmentEnd < segmentStart)
            {
                segmentEnd = segmentStart; // 防御
            }

            // 在收到第一个/新的BPM事件时立即生成一个barline
            if (result.Count == 0 || result[^1] != segmentStart)
            {
                result.Add(segmentStart);
            }

            // 然后按小节间隔继续生成直到下一事件或结束时间
            float t = segmentStart + measureMs;
            while (t <= segmentEnd)
            {
                result.Add(t);
                t += measureMs;
            }
        }

        return result;
    }

    public static Vector2 PivotMiddle(Vector2 pivotTopLeft)
    {
        return new Vector2(pivotTopLeft.x - 0.5f * Values.noteHolderWidth, -pivotTopLeft.y + 0.5f * Values.noteHolderHeight);
    }
}