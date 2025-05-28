using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class DigitalLevel : MonoBehaviour
{
    public GameObject circle;
    public RectTransform canvas;

    [Range(0.1f, 1f)]
    public float smoothFactor = 0.8f; // 平滑因子，值越小越平滑

    [Header("Trail Settings")]
    public Color trailStartColor = Color.white;
    public Color trailEndColor = new Color(1, 1, 1, 0);
    public float trailTime = 0.5f;
    [Range(0.1f, 2f)]
    public float trailWidthMultiplier = 1f; // 相对于circle大小的倍数
    private TrailRenderer trailRenderer;

    public static DigitalLevel Instance;

    Vector2 tilt;
    Vector2 calibration = Vector2.zero;
    Vector2 targetPosition; // 目标位置
    Vector2 currentPosition; // 当前平滑后的位置

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
        
        // 初始化当前位置
        currentPosition = circle.transform.localPosition;

        // 设置拖尾效果
        trailRenderer = circle.AddComponent<TrailRenderer>();
        trailRenderer.time = trailTime;
        
        // 获取circle的实际大小
        Transform circleTransform = circle.GetComponent<Transform>();
        float circleSize = Mathf.Min(circleTransform.lossyScale.x, circleTransform.lossyScale.y);
        
        // 设置拖尾宽度基于circle的大小
        trailRenderer.startWidth = circleSize * trailWidthMultiplier;
        trailRenderer.endWidth = 0f;
        trailRenderer.startColor = trailStartColor;
        trailRenderer.endColor = trailEndColor;
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
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
            circlePos.x = tilt.x * Mathf.Rad2Deg / Values.fullTiltAngle * Values.Preference.sensitivity * Values.canvasHalfWidth;
            circlePos.y = tilt.y * Mathf.Rad2Deg / Values.fullTiltAngle * Values.Preference.sensitivity * Values.canvasHalfWidth;

            targetPosition = circlePos;
            currentPosition = Vector2.Lerp(currentPosition, targetPosition, smoothFactor);
            circle.transform.localPosition = currentPosition;

            // rotation.x = alpha * (rotation.x + gyroRotationRate.x * Time.deltaTime) + (1 - alpha) * tiltX;
            // rotation.y = alpha * (rotation.y + gyroRotationRate.y * Time.deltaTime) + (1 - alpha) * tiltY;
        }
        else
        {
            Vector2 screenPoint = Mouse.current.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screenPoint, Camera.main, out Vector2 localPoint);
            targetPosition = localPoint;
            currentPosition = Vector2.Lerp(currentPosition, targetPosition, smoothFactor);
            circle.transform.localPosition = currentPosition;
        }
    }

    public Vector2 GetPosition()
    {
        return currentPosition; // 返回平滑后的位置
    }

    public void Calibrate(Finger _ = null)
    {
        calibration += tilt;
        Debug.LogWarning("calibrated this much:");
        Debug.LogWarning(tilt);
    }
}
