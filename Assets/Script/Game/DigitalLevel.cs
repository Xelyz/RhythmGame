using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class DigitalLevel : MonoBehaviour
{
    public GameObject circle;
    public RectTransform canvas;

    public static DigitalLevel Instance;

    Vector2 tilt;
    Vector2 calibration = Vector2.zero;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (Values.accAvail)
        {
            InputSystem.EnableDevice(GravitySensor.current);
        }
    }

    void FixedUpdate()
    {
        if (Values.accAvail)
        {
            // 获取加速度记数据
            Vector3 acceleration = InputSystem.GetDevice<GravitySensor>().gravity.value;

            // 计算倾斜角度
            tilt.y = Mathf.Atan2(acceleration.y, -acceleration.z);
            tilt.x = Mathf.Atan2(acceleration.x, -acceleration.z);

            tilt -= calibration;

            Vector2 circlePos;
            circlePos.x = tilt.x * Mathf.Rad2Deg / Values.fullTiltAngle * Preference.sensitivity * Values.canvasHalfWidth;
            circlePos.y = tilt.y * Mathf.Rad2Deg / Values.fullTiltAngle * Preference.sensitivity * Values.canvasHalfWidth;

            circle.transform.localPosition = circlePos;

            // rotation.x = alpha * (rotation.x + gyroRotationRate.x * Time.deltaTime) + (1 - alpha) * tiltX;
            // rotation.y = alpha * (rotation.y + gyroRotationRate.y * Time.deltaTime) + (1 - alpha) * tiltY;
        }
        else
        {
            Vector2 screenPoint = Mouse.current.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screenPoint, Camera.main, out Vector2 localPoint);
            circle.transform.localPosition = localPoint;
        }
    }

    public Vector2 GetPosition()
    {
        return circle.transform.localPosition;
    }

    public void Calibrate(Finger _ = null)
    {
        calibration += tilt;
        Debug.LogWarning("calibrated this much:");
        Debug.LogWarning(tilt);
    }
}
