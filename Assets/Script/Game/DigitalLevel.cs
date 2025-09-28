using UnityEngine;
using UnityEngine.InputSystem;

public class DigitalLevel : MonoBehaviour
{
    public AttitudeSensor attitudeSensor;
    public GravitySensor gravitySensor;

    public static DigitalLevel Instance;

    /// <remarks>
    /// X: roll (横滚角)
    /// Y: pitch (俯仰角)
    /// Z: yaw (偏航角)
    /// </remarks>
    Vector3 tilt;
    Vector3 calibration = Vector3.zero;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        if (Values.accAvail && !PlayInfo.isAutoplay)
        {
            InputSystem.EnableDevice(AttitudeSensor.current);
            attitudeSensor = AttitudeSensor.current;
            InputSystem.EnableDevice(GravitySensor.current);
            gravitySensor = GravitySensor.current;
        }
    }

    void OnDisable()
    {
        if (Values.accAvail && !PlayInfo.isAutoplay)
        {
            InputSystem.DisableDevice(AttitudeSensor.current);
        }
    }


    void FixedUpdate()
    {
        if (Values.accAvail)
        {
            // 获取加速度记数据
            Quaternion attitude = attitudeSensor.attitude.value;
            //Vector3 localGravity = gravitySensor.gravity.value;

            Vector3 localGravity = Quaternion.Inverse(attitude) * new Vector3(0, 0, -1f);

            // 3. 使用 Atan2 计算 Pitch 和 Roll
            // Atan2(y, x) 返回 y/x 的反正切值，但能正确处理全象限的角度
            tilt.x = Mathf.Atan2(localGravity.x, -localGravity.z) * Mathf.Rad2Deg;
            tilt.y = Mathf.Atan2(localGravity.y, -localGravity.z) * Mathf.Rad2Deg;
            tilt.z = attitude.eulerAngles.z;
            
            tilt -= calibration;

            tilt = Normalize(tilt);
        }
    }

    /// <summary>
    /// 获取处理后的倾斜角度数据
    /// </summary>
    /// <returns>倾斜角度（经过校准和归一化）</returns>
    public Vector3 GetTilt()
    {
        return tilt;
    }


    public void Calibrate()
    {
        if (!Values.accAvail)
        {
            return;
        }

        calibration += tilt;
        Debug.LogWarning("calibrated this much:");
        Debug.LogWarning(tilt);
    }

    private Vector3 Normalize(Vector3 angle)
    {
        while (angle.x > 180)
            angle.x -= 360;
        while (angle.x <= -180)
            angle.x += 360;
        while (angle.y > 180)
            angle.y -= 360;
        while (angle.y <= -180)
            angle.y += 360;
        while (angle.z > 180)
            angle.z -= 360;
        while (angle.z <= -180)
            angle.z += 360;
        return angle;
    }

}
