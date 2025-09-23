using UnityEngine;

public class BarlineManager : MonoBehaviour
{
    private Transform noteHolder;
    private int nextBarlineTimeMs;
    private int measureMs;
    private bool initialized = false;

    public void Init(Transform holder, float beatIntervalMs)
    {
        noteHolder = holder;
        measureMs = Mathf.RoundToInt(beatIntervalMs * 4f);
        nextBarlineTimeMs = 0;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || GameManager.Instance == null) return;
        if (!GameManager.Instance.gameState.IsPlaying) return;

        int current = GameManager.Instance.gameState.CurrentTime;
        while (current >= nextBarlineTimeMs - Values.spawnTime)
        {
            SpawnBarline(nextBarlineTimeMs);
            nextBarlineTimeMs += measureMs;
        }
    }

    private void SpawnBarline(int targetTimeMs)
    {
        GameObject go = new("BarLine");
        go.transform.SetParent(noteHolder);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // 绘制矩形边框
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.alignment = LineAlignment.TransformZ;
        lr.loop = false;
        lr.positionCount = 5;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        Color c = new(1f, 1f, 1f, 0.4f);
        lr.startColor = c;
        lr.endColor = c;
        lr.startWidth = 3f;
        lr.endWidth = 3f;
        lr.sortingOrder = -100; // 保持在音符下方

        float halfW = Values.noteHolderWidth * 0.5f;
        float halfH = Values.noteHolderHeight * 0.5f;
        Vector3[] pts = new Vector3[5]
        {
            new(-halfW,  halfH, 0f),
            new( halfW,  halfH, 0f),
            new( halfW, -halfH, 0f),
            new(-halfW, -halfH, 0f),
            new(-halfW,  halfH, 0f),
        };
        lr.SetPositions(pts);

        // 滚动设置
        NoteScroll scroll = go.AddComponent<NoteScroll>();
        scroll.isActive = true;

        float animationDuration = (targetTimeMs - GameManager.Instance.gameState.CurrentTime) / 1000f;
        Vector3 worldPos = go.transform.position;
        worldPos.z = Values.planeDistance + animationDuration * Values.Preference.NoteSpeed;
        go.transform.position = worldPos;
    }
}


