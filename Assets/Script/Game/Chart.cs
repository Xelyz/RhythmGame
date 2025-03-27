using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class Note
{
    public int timeStamp;
    public Vector2 position;
    public NoteType type;
    public int nthNote;

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
    public float radius;

    public bool isFading = false;

    public Tap()
    {
        type = NoteType.Tap;
        radius = Values.TapRadius;
    }

    protected virtual void GetObjects()
    {
        gameObject = NotePool.Instance.Get(type);
        head = gameObject.transform;
        circle = head.Find("Circle").GetComponent<SpriteRenderer>();
        outerCircle = head.Find("OuterCircle").gameObject;
    }

    public override void FadeOut()
    {
        if (gameObject != null && !isFading)
        {
            isFading = true;
            circle.DOFade(0, 0.15f).OnComplete(Release);
        }
    }

    public override void PopOut()
    {
        if (gameObject != null)
        {
            circle.DOKill();
            outerCircle.transform.DOKill();
            circle.DOFade(0f, 0.1f).SetEase(Ease.OutQuad);
            circle.transform.DOScale(1.5f, 0.1f).SetEase(Ease.OutQuad).OnComplete(Release);
        }
    }

    public override void Release()
    {
        if (gameObject != null)
        {
            circle.DOKill();
            outerCircle.transform.DOKill();
            NotePool.Instance.Release(type, gameObject);
        }
    }

    public override void Initialize(Transform noteHolder)
    {
        GetObjects();
        float animationDuration = (timeStamp - GameManager.Instance.currentTime) / 1000f;

        gameObject.transform.SetParent(noteHolder);
        gameObject.transform.localPosition = Vector3.zero;

        head.localPosition = new(position.x, position.y);
        head.localScale = new Vector3(radius, radius, 1f);

        circle.DOFade(1f, 0.2f).From(0f).SetEase(Ease.Linear);

        Vector3 newPosition = circle.transform.position;
        newPosition.z = Values.planeDistance + animationDuration * Values.Preference.noteSpeed;
        circle.transform.position = newPosition;

        circle.transform.localScale = new(1, 1, 1);
        circle.sortingOrder = -nthNote;

        outerCircle.SetActive(true);
        SpriteRenderer outerSR = outerCircle.GetComponentInChildren<SpriteRenderer>();
        outerSR.DOFade(0.8f, 0.7f).From(0f).SetDelay(animationDuration - 0.7f).SetEase(Ease.Linear);

        outerCircle.transform.DOScale(1f, animationDuration).From(1.8f).SetEase(Ease.Linear).OnKill(() => outerCircle.SetActive(false));

        isFading = false;
    }
}

public class Drag : Tap
{
    public Drag()
    {
        type = NoteType.Drag;
        radius = Values.DragRadius;
    }
}

public class Block : Tap
{
    public Block()
    {
        type = NoteType.Block;
        radius = Values.TapRadius;
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
}
