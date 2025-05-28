using UnityEngine;
using System;

/// <summary>
/// 输入事件数据结构
/// </summary>
public struct InputEventData
{
    public int tapCount;
    public Vector2 cursorPosition;
    public float timestamp;

    public InputEventData(int tapCount, Vector2 cursorPosition)
    {
        this.tapCount = tapCount;
        this.cursorPosition = cursorPosition;
        this.timestamp = Time.time;
    }
}

/// <summary>
/// 输入事件管理器 - 用于解耦输入处理和判定逻辑
/// </summary>
public static class InputEvent
{
    /// <summary>
    /// 输入事件，当有输入时触发
    /// </summary>
    public static event Action<InputEventData> OnInput;

    /// <summary>
    /// 触发输入事件
    /// </summary>
    /// <param name="eventData">输入事件数据</param>
    public static void TriggerInput(InputEventData eventData)
    {
        OnInput?.Invoke(eventData);
    }

    /// <summary>
    /// 清除所有事件订阅（用于场景切换等情况）
    /// </summary>
    public static void ClearAllSubscribers()
    {
        OnInput = null;
    }
} 