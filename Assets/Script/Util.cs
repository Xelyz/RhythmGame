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
            File.WriteAllText(Values.savePath, json);
            Debug.Log("Results saved to: " + Values.savePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save results: " + e.Message);
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

                int noteType = int.Parse(data[4]);

                Note note;

                if ((noteType & 0b10000) == 0b10000)
                {
                    note = new Drag()
                    {
                        position = PivotMiddle(new(int.Parse(data[0]), int.Parse(data[1]))),
                        timeStamp = int.Parse(data[2]),
                        nthNote = n++
                    };
                }
                else
                {
                    note = new Tap()
                    {
                        position = PivotMiddle(new(int.Parse(data[0]), int.Parse(data[1]))),
                        timeStamp = int.Parse(data[2]),
                        nthNote = n++
                    };
                }

                chart.notes.Add(note);
            }
        }
        return chart;
    }

    public static Vector2 PivotMiddle(Vector2 pivotTopLeft)
    {
        return new Vector2(pivotTopLeft.x - 0.5f * Values.noteHolderWidth, -pivotTopLeft.y + 0.5f * Values.noteHolderHeight);
    }
}