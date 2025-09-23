using System;
using System.Collections;
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
        public int X, Y, TimeStamp, Type;
        public NoteData(string[] data)
        {
            X = int.Parse(data[0]);
            Y = int.Parse(data[1]);
            TimeStamp = int.Parse(data[2]);
            Type = int.Parse(data[4]);
        }
    }

    public static Chart GetChart(string chartData)
    {
        Chart chart = new()
        {
            notes = new(),
            beatIntervalMs = 0f
        };
        string[] lines = chartData.Split('\n');
        string reading = "";
        int n = 1;
        bool timingPointParsed = false;
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
            if (reading == "TimingPoints" && !timingPointParsed)
            {
                // 只读取第一行，逗号分隔的第二个元素为每拍毫秒
                try
                {
                    string[] data = line.Split(',');
                    if (data.Length >= 2)
                    {
                        chart.beatIntervalMs = float.Parse(data[1], CultureInfo.InvariantCulture);
                        timingPointParsed = true;
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

    public static Vector2 PivotMiddle(Vector2 pivotTopLeft)
    {
        return new Vector2(pivotTopLeft.x - 0.5f * Values.noteHolderWidth, -pivotTopLeft.y + 0.5f * Values.noteHolderHeight);
    }
}