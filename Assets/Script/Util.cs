using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.IO;

public static class Util
{
    public static void Transition(string toScene)
    {
        Transfer.fromScene = SceneManager.GetActiveScene().name;
        Transfer.toScene = toScene;
        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);
    }

    public static IEnumerator DelayAction(Action action, float delaySeconds)
    {
        Debug.Log("1");
        yield return new WaitForSeconds(delaySeconds);
        Debug.Log("2");
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
            File.WriteAllText(Values.savePath, json);
            Debug.Log("Results saved to: " + Values.savePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save results: " + e.Message);
        }
    }

    private static readonly int block = 1 << 3;
    private static readonly int drag = 1 << 2;

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
            notes = new()
        };
        string[] lines = chartData.Split('\n');
        string reading = "";
        int n = 1;
        foreach (string line in lines)
        {
            if (line == "") continue;
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

                    note.position = PivotMiddle(new Vector2(noteData.X, noteData.Y));
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