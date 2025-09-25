using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchInput : MonoBehaviour
{
    private int tapCount = 0;
    private Vector2 cursorPos;
    
    public static TouchInput Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        InitializeInputData();
        UpdateCursorPosition();
        
        if (!PlayInfo.isAutoplay)
        {
            if (Values.accAvail)
            {
                ProcessTouch();
            }
            else
            {
                ProcessMouse();
            }
        }

        // 使用事件系统传递输入信息
        if (GameManager.Instance.gameState.IsPlaying && tapCount > 0)
        {
            InputEvent.TriggerInput(new InputEventData(tapCount, cursorPos));
        }
    }

    void OnDestroy()
    {
        // 清理事件订阅
        InputEvent.ClearAllSubscribers();
    }
    
    private void InitializeInputData()
    {
        tapCount = 0;
    }
    
    private void UpdateCursorPosition()
    {
        cursorPos = DigitalLevel.Instance.GetPosition();
    }

    private void ProcessTouch()
    {
        foreach (Touch finger in Touch.activeTouches)
        {
            if (finger.began)
            {
                tapCount++;
            }
        }
    }

    private void ProcessMouse()
    {
        if (Keyboard.current.zKey.wasPressedThisFrame || Keyboard.current.xKey.wasPressedThisFrame)
        {
            tapCount++;
        }
    }
    
    /// <summary>
    /// 获取当前帧的点击次数（供外部调用）
    /// </summary>
    public int GetTapCount()
    {
        return tapCount;
    }
    
    /// <summary>
    /// 获取当前光标位置（供外部调用）
    /// </summary>
    public Vector2 GetCursorPosition()
    {
        return cursorPos;
    }
}


