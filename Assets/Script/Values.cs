using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

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

    // 游戏参数
    public static float planeDistance = 5f;
    public static float fullTiltAngle = 45f;
    
    // 时间窗口 (ms)
    public static float waitTime = 3000f;
    public static float spawnTime = 1500f;
    public static float perfectWindow = 60f;
    public static float goodWindow = 90f;
    public static float badWindow = 120f;

    // 画布尺寸
    public static float canvasHalfHeight = 240f;
    public static float canvasHalfWidth = 320f;
    public static int noteHolderWidth = 512;
    public static int noteHolderHeight = 384;

    // 网格配置（可配置）
    public static int gridColumns = 4;
    public static int gridRows = 3;

    // 调试
    public static bool gridDebugLog = false;

    public static Vector2 CellSize()
    {
        return new Vector2((float)noteHolderWidth / gridColumns, (float)noteHolderHeight / gridRows);
    }

    // 输入：以左上角为原点(0,0)的像素坐标（如谱面原始坐标），输出：列、行索引
    public static Vector2Int TopLeftToCellIndex(Vector2 topLeft)
    {
        Vector2 cell = CellSize();
        int col = Mathf.Clamp((int)(topLeft.x / cell.x), 0, gridColumns - 1);
        int row = Mathf.Clamp((int)(topLeft.y / cell.y), 0, gridRows - 1);
        return new Vector2Int(col, row);
    }

    // 输入：以画布中心为原点的本地坐标，输出：列、行索引
    public static Vector2Int LocalToCellIndex(Vector2 local)
    {
        Vector2 cell = CellSize();
        float halfW = noteHolderWidth * 0.5f;
        float halfH = noteHolderHeight * 0.5f;
        float xFromLeft = local.x + halfW;
        float yFromTop = halfH - local.y;
        int col = Mathf.Clamp((int)(xFromLeft / cell.x), 0, gridColumns - 1);
        int row = Mathf.Clamp((int)(yFromTop / cell.y), 0, gridRows - 1);
        return new Vector2Int(col, row);
    }

    // 获取指定格子的中心点（返回以画布中心为原点的坐标）
    public static Vector2 CellCenterLocal(int col, int row)
    {
        Vector2 cell = CellSize();
        float x = (col + 0.5f) * cell.x - noteHolderWidth * 0.5f;
        float y = noteHolderHeight * 0.5f - (row + 0.5f) * cell.y;
        return new Vector2(x, y);
    }
}
