using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch; // 引入 EnhancedTouch 命名空间

public class ClickToStart : MonoBehaviour
{
    void OnEnable()
    {
        // 订阅触摸事件
        Touch.onFingerDown += OnFingerDown;
    }

    void OnDisable()
    {
        // 取消订阅触摸事件
        Touch.onFingerDown -= OnFingerDown;
    }

    private void OnFingerDown(Finger finger)
    {
        // 当手指按下时，跳转场景
        Util.Transition("SongSelectScene");
        enabled = false;
    }
}
