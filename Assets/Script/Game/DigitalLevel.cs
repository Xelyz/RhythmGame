using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TMPro;
using UnityEngine.Tilemaps;

public class DigitalLevel : MonoBehaviour
{
    public GameObject circle;
    public RectTransform canvas;

    [Range(0.1f, 1f)]
    public float smoothFactor = 0.8f; // 平滑因子，值越小越平滑

    [Header("Trail Settings")]
    public Color trailStartColor = Color.white;
    public Color trailEndColor = new(1, 1, 1, 0);
    public float trailTime = 0.5f;
    [Range(0.1f, 2f)]
    public float trailWidthMultiplier = 1f; // 相对于circle大小的倍数
    private TrailRenderer trailRenderer;

    public GravitySensor gravitySensor;

    public static DigitalLevel Instance;

    /// <remarks>
    /// X: pitch (俯仰角)
    /// Y: yaw (偏航角) 
    /// Z: roll (横滚角)
    /// </remarks>
    Vector3 tilt;
    Vector3 calibration = Vector3.zero;
    Vector2 targetPosition; // 目标位置
    Vector2 currentPosition; // 当前平滑后的位置

    [SerializeField]
    private bool isTesting = false;
    [SerializeField]
    private TextMeshProUGUI testText;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        if (Values.accAvail)
        {
            InputSystem.EnableDevice(GravitySensor.current);
            gravitySensor = GravitySensor.current;
        }
    }

    void OnDisable()
    {
        if (Values.accAvail)
        {
            InputSystem.DisableDevice(AttitudeSensor.current);
        }
    }

    void Start()
    {
        Transform circleTransform = circle.transform;
        // 初始化当前位置
        currentPosition = circleTransform.localPosition;

        // 设置拖尾效果
        trailRenderer = circle.AddComponent<TrailRenderer>();
        trailRenderer.time = trailTime;
        
        // 获取circle的实际大小
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

            if (isTesting)
            {
                testText.text = "tilt: " + tilt + "\n" +
                                "circlePos: " + circlePos + "\n" +
                                "targetPosition: " + targetPosition + "\n" +
                                "currentPosition: " + currentPosition;
            }
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

    private float Normalize(float angle)
    {
        while (angle > 180)
            angle -= 360;
        while (angle <= -180)
            angle += 360;
        return angle;
    }
}
