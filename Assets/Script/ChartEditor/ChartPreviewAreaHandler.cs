using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 处理 chartPreviewArea 的所有交互操作
/// 包括：点击添加/删除note，滚轮/双指滑动调整时间轴
/// </summary>
public class ChartPreviewAreaHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
{
    [Header("Settings")]
    [SerializeField] private float scrollSensitivity = 10f; // 滚轮灵敏度
    [SerializeField] private float touchScrollSensitivity = 0.5f; // 触摸滑动灵敏度
    
    private RectTransform rectTransform;
    private Camera uiCamera;
    private bool isDragging = false;
    private Vector2 lastTouchPosition;
    private int touchCount = 0;
    
    // 事件定义
    public System.Action<Vector2Int> OnAreaClicked; // 点击事件，参数是网格坐标
    public System.Action<float> OnTimeScroll; // 时间轴滚动事件，参数是滚动量（毫秒）
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // 尝试获取 UI Camera（通常是 Canvas 的 Render Camera）
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }
        else
        {
            uiCamera = Camera.main;
        }
    }
    
    void Update()
    {
        // 处理触摸输入（双指滑动）
        HandleTouchInput();
    }
    
    /// <summary>
    /// 处理触摸输入（双指滑动调整时间轴）
    /// </summary>
    void HandleTouchInput()
    {
        // 检查是否在区域内
        if (!IsPointerOverArea())
        {
            return;
        }
        
        // 检测触摸输入
        if (Touchscreen.current != null)
        {
            int currentTouchCount = Touchscreen.current.touches.Count;
            
            // 双指滑动
            if (currentTouchCount == 2)
            {
                var touches = Touchscreen.current.touches;
                Vector2 touch1Pos = touches[0].position.ReadValue();
                Vector2 touch2Pos = touches[1].position.ReadValue();
                Vector2 currentCenter = (touch1Pos + touch2Pos) / 2f;
                
                if (touchCount == 2)
                {
                    // 计算滑动距离
                    float deltaY = (currentCenter.y - lastTouchPosition.y) * touchScrollSensitivity;
                    if (Mathf.Abs(deltaY) > 0.1f)
                    {
                        OnTimeScroll?.Invoke(deltaY);
                    }
                }
                
                lastTouchPosition = currentCenter;
                touchCount = 2;
            }
            else
            {
                touchCount = currentTouchCount;
            }
        }
    }
    
    /// <summary>
    /// 检查指针是否在区域内
    /// </summary>
    bool IsPointerOverArea()
    {
        Vector2 screenPoint = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        
        // 如果没有鼠标，尝试获取触摸位置
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            screenPoint = Touchscreen.current.touches[0].position.ReadValue();
        }
        
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, uiCamera);
    }
    
    /// <summary>
    /// 将屏幕坐标转换为网格坐标
    /// </summary>
    Vector2Int ScreenToGridPosition(Vector2 screenPosition)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, screenPosition, uiCamera, out Vector2 localPoint))
        {
            return Values.LocalToCellIndex(localPoint);
        }
        return new Vector2Int(-1, -1);
    }
    
    /// <summary>
    /// 左键点击处理
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 只处理左键点击
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }
        
        // 检查是否点击了其他 UI 元素（如按钮）
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // 检查点击的是否是本组件
            if (eventData.pointerCurrentRaycast.gameObject != gameObject)
            {
                return;
            }
        }
        
        // 转换为网格坐标
        Vector2Int gridPos = ScreenToGridPosition(eventData.position);
        
        // 检查是否在有效网格范围内
        if (gridPos.x >= 0 && gridPos.x < Values.gridColumns && 
            gridPos.y >= 0 && gridPos.y < Values.gridRows)
        {
            OnAreaClicked?.Invoke(gridPos);
        }
    }
    
    /// <summary>
    /// 指针按下事件（用于检测拖拽开始）
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            isDragging = true;
            lastTouchPosition = eventData.position;
        }
    }
    
    /// <summary>
    /// 指针抬起事件（用于检测拖拽结束）
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            isDragging = false;
        }
    }
    
    /// <summary>
    /// 拖拽处理（右键拖拽调整时间轴）
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && isDragging)
        {
            float deltaY = (eventData.position.y - lastTouchPosition.y) * touchScrollSensitivity;
            
            if (Mathf.Abs(deltaY) > 0.1f)
            {
                OnTimeScroll?.Invoke(deltaY);
            }
            
            lastTouchPosition = eventData.position;
        }
    }
    
    /// <summary>
    /// 滚轮滚动处理（调整时间轴）
    /// </summary>
    public void OnScroll(PointerEventData eventData)
    {
        // 检查是否在区域内
        if (!IsPointerOverArea())
        {
            return;
        }
        
        // 计算滚动量（转换为时间偏移，毫秒）
        float scrollDelta = eventData.scrollDelta.y * scrollSensitivity;
        OnTimeScroll?.Invoke(scrollDelta);
    }
}

