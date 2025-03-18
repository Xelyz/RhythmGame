using System.Collections.Generic;

[System.Serializable]
public class PlayResult
{
    public float accuracy = 0f;
    public string rating = "NEW!";
    public string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // 新增时间戳
}

[System.Serializable]
public class PlayerData
{
    // songID:
    // {
    //     difficulty as in [0,1,2,3]:
    //     {
    //         --PlayResult
    //     }
    // }

    public Dictionary<string, Dictionary<int, PlayResult>> songResults = new();
}
