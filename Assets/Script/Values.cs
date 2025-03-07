using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Values
{
    public static bool accAvail = false;

    public static float noteRadius = 45f;
    public static float judgeLeniency = 15f;
    public static float JudgeRadius => noteRadius + judgeLeniency;
    public static float HoldingRadius => noteRadius * 2;

    public static float fullTiltAngle = 60;

    public static int waitTime = 1500;
    public static int spawnTime = 1000;
    public static float baseSlideSpeed = 1.1f;

    public static int prefectWindow = 60;
    public static int goodWindow = 90;
    public static int badWindow = 120;

    public static float canvasHalfHeight = 320;
    public static float canvasHalfWidth = 240;

    public static int noteHolderWidth = 512;
    public static int noteHolderHeight = 384;
}
