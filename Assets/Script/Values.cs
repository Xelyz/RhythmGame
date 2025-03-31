using System.IO;
using UnityEngine;

public static class Values
{
    public static bool accAvail = false;
    public static string savePath = Path.Combine(Application.persistentDataPath, "playResults.json");
    public static PlayerData playerData = new();
    public static Preference Preference => playerData.preference;

    public static float planeDistance = 5f;
    public static float TapRadius = 40f;
    public static float DragRadius = 30f;
    public static float judgeLeniency = 15f;
    public static float TapJudgeRadius => TapRadius + judgeLeniency;
    public static float HoldingRadius => TapRadius * 2;

    public static float fullTiltAngle = 60;

    public static int waitTime = 2000;
    public static int spawnTime = 1500;

    public static int prefectWindow = 60;
    public static int goodWindow = 90;
    public static int badWindow = 120;

    public static float canvasHalfHeight = 320;
    public static float canvasHalfWidth = 240;

    public static int noteHolderWidth = 512;
    public static int noteHolderHeight = 384;
}
