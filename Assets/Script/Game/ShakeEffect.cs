using System.Collections;
using UnityEngine;

public class ShakeEffect : MonoBehaviour
{
    public float shakeDuration = 0.2f;    // 震动持续时间
    public float shakeMagnitude = 4f;    // 震动幅度（像素单位）

    private Vector3 initialPosition;      // 初始位置

    // 触发震动的公共方法
    public void TriggerShake()
    {
        StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        initialPosition = transform.localPosition; // 记录初始位置

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            transform.localPosition = new Vector3(initialPosition.x + x, initialPosition.y + y, initialPosition.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = initialPosition; // 恢复到初始位置
    }
}
