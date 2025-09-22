using UnityEngine;

public class NoteScroll : MonoBehaviour
{
    public bool isActive = true;
    // private float timeOffset;
    // private float initialZ;

    void OnEnable()
    {
        // initialZ = transform.position.z;
        // 计算从当前位置到判定线所需的时间
        // why did this exist? cursor?!
        // timeOffset = (initialZ - Values.planeDistance) / Values.Preference.NoteSpeed;
    }

    void Update()
    {
        if (GameManager.Instance.gameState.IsPlaying && isActive && transform.position.z > Values.planeDistance)
        {
            // 使用补偿后的时间来计算位置
            // float compensatedSpeed = Values.Preference.NoteSpeed * (1 + timeOffset * 0.01f);
            transform.position -= new Vector3(0, 0, Values.Preference.NoteSpeed * Time.deltaTime);
        }
    }
}
