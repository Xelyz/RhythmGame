using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class Note
{
    public float timeStamp;
    public Vector2 position;
    public NoteType type;
    public int nthNote;
    public Vector2Int cellIndex;

    public virtual void Initialize(Transform noteHolder) { }

    public virtual void Release() { }

    public virtual void FadeOut() { }

    public virtual void PopOut() { }
}

public class Tap : Note
{
    public GameObject gameObject;
    public Transform head;
    public SpriteRenderer circle;
    public GameObject outerCircle;
    NoteScroll scroll;
    public float radius;

    public bool isFading = false;

    public Tap()
    {
        type = NoteType.Tap;
    }

    protected virtual void GetObjects()
    {
        gameObject = NotePool.Instance.Get(type);
        head = gameObject.transform;
        circle = head.Find("Circle").GetComponent<SpriteRenderer>();
        scroll = circle.gameObject.GetComponent<NoteScroll>();
        outerCircle = head.Find("OuterCircle")?.gameObject;
    }

    public override void FadeOut()
    {
        if (gameObject != null && !isFading)
        {
            isFading = true;
            circle.DOFade(0, 0.1f).OnComplete(Release);
        }
    }

    public override void PopOut()
    {
        if (gameObject != null)
        {
            circle.DOKill();
            if (outerCircle != null) outerCircle.transform.DOKill();
            scroll.isActive = false;
            circle.DOFade(0f, 0.1f).SetEase(Ease.OutQuad);
            circle.transform.DOScale(1.5f, 0.1f).SetEase(Ease.OutQuad).OnComplete(Release);
        }
    }

    public override void Release()
    {
        if (gameObject != null)
        {
            circle.DOKill();
            if (outerCircle != null) outerCircle.transform.DOKill();
            NotePool.Instance.Release(type, gameObject);
            gameObject = null;
        }
    }

    public override void Initialize(Transform noteHolder)
    {
        GetObjects();
        float animationDuration = (timeStamp - GameManager.Instance.gameState.CurrentTime) / 1000f;

        gameObject.transform.SetParent(noteHolder);
        gameObject.transform.localPosition = Vector3.zero;

        head.localPosition = new(position.x, position.y);

        // 按格子尺寸填充
        Vector2 cellSize = Values.CellSize();
        head.localScale = new Vector3(cellSize.x, cellSize.y, 1f);

        Vector3 newPosition = circle.transform.position;
        newPosition.z = Values.planeDistance + animationDuration * Values.Preference.NoteSpeed;
        circle.transform.position = newPosition;

		// debug log for z-index alignment check
		Debug.Log($"SPAWN [NOTE] t={timeStamp:F3}ms current={GameManager.Instance.gameState.CurrentTime:F3}ms anim={animationDuration:F3}s z={newPosition.z:F3} plane={Values.planeDistance} speed={Values.Preference.NoteSpeed} spawnTime={Values.spawnTime}");

        Color color = circle.color;
        color.a = 0.6f;
        circle.color = color;

        circle.transform.localScale = new(1, 1, 1);
        circle.sortingOrder = -nthNote;

        scroll.isActive = true;

        if (outerCircle != null && outerCircle.activeSelf)
        {
            outerCircle.SetActive(true);
            SpriteRenderer outerSR = outerCircle.GetComponentInChildren<SpriteRenderer>();
            outerSR.DOFade(0.8f, 0.7f).From(0f).SetDelay(animationDuration - 0.7f).SetEase(Ease.Linear);

            outerCircle.transform.DOScale(1f, animationDuration).From(1.8f).SetEase(Ease.Linear).OnKill(() => outerCircle.SetActive(false));
        }

        isFading = false;

        if (Values.gridDebugLog)
        {
            Bounds b = circle.bounds;
            Debug.Log($"Note init cell=({cellIndex.x},{cellIndex.y}) center={position} cellSize={cellSize} spriteWorldSize=({b.size.x},{b.size.y})");
        }
    }
}

public class Drag : Tap
{
    public Drag()
    {
        type = NoteType.Drag;
    }
}

public class Block : Tap
{
    public Block()
    {
        type = NoteType.Block;
    }
}

public enum NoteType
{
    Tap,
    Drag,
    Block,
}

public class Chart
{
    public List<Note> notes;
    public List<ChartEvent> events;
}
