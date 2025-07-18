using System.IO;
using UnityEngine;

public static class Values
{
    // 存档相关
    public static bool accAvail = false;
    private static string _savePath;
    public static string SavePath
    {
        get
        {
            if (string.IsNullOrEmpty(_savePath))
            {
                _savePath = Path.Combine(Application.persistentDataPath, "playResults.json");
            }
            return _savePath;
        }
    }
    public static PlayerData playerData = new();
    public static Preference Preference => playerData.preference;

    // 判定相关
    public static float tapRadius = 35f;
    public static float dragRadius = 20f;
    public static float judgeLeniency = 20f;
    public static float TapJudgeRadius => tapRadius + judgeLeniency;
    public static float HoldingRadius => tapRadius * 2;

    // 游戏参数
    public static float planeDistance = 5f;
    public static float fullTiltAngle = 45f;
    
    // 时间窗口
    public static int waitTime = 3000;
    public static int spawnTime = 1500;
    public static int perfectWindow = 60;
    public static int goodWindow = 90;
    public static int badWindow = 120;

    // 画布尺寸
    public static float canvasHalfHeight = 320f;
    public static float canvasHalfWidth = 240f;
    public static int noteHolderWidth = 512;
    public static int noteHolderHeight = 384;
}
