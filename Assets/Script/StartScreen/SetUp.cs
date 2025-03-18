using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem;
using System.IO;
using Newtonsoft.Json;

public class SetUp : MonoBehaviour
{
    void Awake()
    {
        EnhancedTouchSupport.Enable();
        if (GravitySensor.current != null)
        {
            Values.accAvail = true;
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        LoadPlayData();
    }

    void LoadPlayData()
    {
        if (File.Exists(Values.savePath))
        {
            try
            {
                string json = File.ReadAllText(Values.savePath);
                var loadedData = JsonConvert.DeserializeObject<PlayerData>(json);
                if (loadedData != null)
                {
                    Values.playerData = loadedData;
                }
                else
                {
                    Values.playerData = new();
                }
                Debug.Log("PlayData loaded from: " + Values.savePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to load PlayData: " + e.Message);
                Values.playerData = new(); // 加载失败时初始化
            }
        }
        else
        {
            Values.playerData = new(); // 文件不存在时初始化
            Debug.Log("No PlayData file found, initialized empty data.");
        }
    }
}
