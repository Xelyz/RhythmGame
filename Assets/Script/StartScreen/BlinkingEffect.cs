using TMPro;
using UnityEngine;

public class BlickingEffect : MonoBehaviour
{
    public float blinkSpeed = 1f; // 闪烁速度
    private TextMeshProUGUI textComponent;
    private Color color;

    void Start()
    {
        // 获取 Text 组件
        textComponent = GetComponent<TextMeshProUGUI>();
        color = textComponent.color;
    }

    void Update()
    {
        // 使用 Mathf.PingPong 计算 Alpha 值在 0 到 1 之间来回变化
        float alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        
        // 获取当前颜色并修改 Alpha 值
        color.a = alpha;
        textComponent.color = color;
    }
}
