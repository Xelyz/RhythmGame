using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;

public class DigitalLevel : MonoBehaviour
{
    public GameObject circle;
    public RectTransform canvas;

    [Range(0.1f, 1f)]
    public float smoothFactor = 0.8f; // 平滑因子，值越小越平滑

	[Header("Autoplay Movement")]
	public bool showAutoplayDebugInfo = false; // 是否显示autoplay调试信息

    [Header("Trail Settings")]
    public Color trailStartColor = Color.white;
    public Color trailEndColor = new(1, 1, 1, 0);
    public float trailTime = 0.5f;
    [Range(0.1f, 2f)]
    public float trailWidthMultiplier = 1f; // 相对于circle大小的倍数
    private TrailRenderer trailRenderer;

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
    Vector2 targetPosition; // 目标位置
    Vector2 currentPosition; // 当前平滑后的位置

    private bool isCircleUnlocked = false;
    private bool isAutoplayControlled = false;
    private Vector2 autoplayTargetPosition;
    private float autoplayTargetTimeMs; // 目标到达时间（游戏时间ms）
    private float autoplayStartTimeMs; // 开始移动的时间（游戏时间ms）
    private Vector2 autoplayStartPosition; // 开始移动时的位置
    private float calculatedSpeed; // 根据时间和距离计算的移动速度

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
            InputSystem.EnableDevice(AttitudeSensor.current);
            attitudeSensor = AttitudeSensor.current;
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

        circle.GetComponent<SpriteRenderer>().DOFade(0f, 0f);

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
		// Autoplay: 基于游戏时间的精确移动
		if (PlayInfo.isAutoplay && isAutoplayControlled)
        {
            float currentTimeMs = GameManager.Instance.gameState.CurrentTime;
            float totalMoveTimeMs = autoplayTargetTimeMs - autoplayStartTimeMs;
            float elapsedTimeMs = currentTimeMs - autoplayStartTimeMs;
            
            if (totalMoveTimeMs > 0 && elapsedTimeMs >= 0)
            {
                float progress = Mathf.Clamp01(elapsedTimeMs / totalMoveTimeMs);
                
                // 使用平滑插值确保在目标时间点准确到达
                currentPosition = Vector2.Lerp(autoplayStartPosition, autoplayTargetPosition, progress);
                
                // 如果已经到达或超过目标时间，确保精确定位
                if (progress >= 1.0f || currentTimeMs >= autoplayTargetTimeMs)
                {
                    currentPosition = autoplayTargetPosition;
                }
            }
            else
            {
                // 如果时间配置有问题，立即移动到目标位置
                currentPosition = autoplayTargetPosition;
            }
            
            circle.transform.localPosition = currentPosition;

            if (isTesting && showAutoplayDebugInfo)
            {
                float distance = Vector2.Distance(autoplayStartPosition, autoplayTargetPosition);
                float progress = totalMoveTimeMs > 0 ? Mathf.Clamp01(elapsedTimeMs / totalMoveTimeMs) : 1f;
                testText.text = "[AUTO] target=" + autoplayTargetPosition + "\n" +
                                "current=" + currentPosition + "\n" +
                                "progress=" + (progress * 100f).ToString("F1") + "%" + "\n" +
                                "distance=" + distance.ToString("F1") + "\n" +
                                "timeLeftMs=" + Mathf.Max(0, autoplayTargetTimeMs - currentTimeMs).ToString("F1") + "ms" + "\n" +
                                "calcSpeed=" + calculatedSpeed.ToString("F1");
            }
            return;
        }

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

            if (!isCircleUnlocked)
            {
                return;
            }

            Vector2 circlePos;
            circlePos.x = tilt.x / Values.fullTiltAngle * Values.Preference.sensitivity * Values.canvasHalfWidth;
            circlePos.y = tilt.y / Values.fullTiltAngle * Values.Preference.sensitivity * Values.canvasHalfWidth;

            targetPosition = circlePos;
            currentPosition = Vector2.Lerp(currentPosition, targetPosition, smoothFactor);
            circle.transform.localPosition = currentPosition;

            if (isTesting)
            {
                testText.text = "tilt: " + tilt + "\n" +
                                "localGravity: " + localGravity + "\n" +
                                "circlePos: " + circlePos + "\n" +
                                "targetPosition: " + targetPosition + "\n";
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

    public void EnableAutoplayControl(bool enabled)
    {
        isAutoplayControlled = enabled;
        if (enabled)
        {
            // 启用时立即将外部目标设置为当前位置，避免跳变
            autoplayTargetPosition = currentPosition;
            autoplayTargetTimeMs = GameManager.Instance.gameState.CurrentTime;
            autoplayStartTimeMs = GameManager.Instance.gameState.CurrentTime;
            autoplayStartPosition = currentPosition;
            if (Values.gridDebugLog)
            {
                Debug.Log("[AUTO] Autoplay control enabled for cursor.");
            }
        }
    }

    public void SetAutoplayTarget(Vector2 target, float targetTimeMs)
    {
        autoplayTargetPosition = target;
        autoplayTargetTimeMs = targetTimeMs;
        autoplayStartTimeMs = GameManager.Instance.gameState.CurrentTime;
        autoplayStartPosition = currentPosition;
        
        float distance = Vector2.Distance(autoplayStartPosition, autoplayTargetPosition);
        float timeToMoveMs = autoplayTargetTimeMs - autoplayStartTimeMs;
        calculatedSpeed = timeToMoveMs > 0 ? distance / (timeToMoveMs / 1000f) : float.MaxValue; // 转换为units/second
        
        if (Values.gridDebugLog)
        {
            Debug.Log($"[AUTO] Set cursor target to {target} at gameTime {targetTimeMs:F1}ms, " +
                     $"distance={distance:F1}, moveTimeMs={timeToMoveMs:F1}ms, speed={calculatedSpeed:F1}");
        }
    }

    // 保持向后兼容的重载方法
    public void SetAutoplayTarget(Vector2 target)
    {
        // 如果没有指定时间，立即移动
        SetAutoplayTarget(target, GameManager.Instance.gameState.CurrentTime + 100f);
    }

    public bool HasAutoplayTarget()
    {
        if (!isAutoplayControlled) return false;
        // 基于游戏时间判断是否还在移动过程中
        float currentTimeMs = GameManager.Instance.gameState.CurrentTime;
        return currentTimeMs < autoplayTargetTimeMs;
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

    public void FadeInCircle()
    {
        circle.GetComponent<SpriteRenderer>().DOFade(1f, 1f).From(0f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            isCircleUnlocked = true;
            Calibrate();
        });
    }
}
