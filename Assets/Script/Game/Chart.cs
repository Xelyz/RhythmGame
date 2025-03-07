using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;

public class Note
{
    public int timeStamp;
    public Vector2 position;
    public NoteType noteType;
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

    public bool isFading = false;

    protected virtual void GetObjects()
    {
        gameObject = NotePool.Instance.Get(noteType);
        head = gameObject.transform;
        circle = head.Find("Circle").GetComponent<SpriteRenderer>();
        outerCircle = head.Find("OuterCircle").gameObject;
    }

    public override void FadeOut()
    {
        if (gameObject != null && !isFading)
        {
            isFading = true;
            circle.DOFade(0, 0.2f).OnComplete(
                () =>
                {
                    Release();
                }
            );
        }
    }

    public override void PopOut()
    {
        if (gameObject != null)
        {
            circle.DOKill();
            outerCircle.transform.DOKill();
            circle.DOFade(0f, 0.1f).SetEase(Ease.OutQuad);
            circle.transform.DOScale(1.5f, 0.1f).SetEase(Ease.OutQuad);
        }
    }

    public override void Release()
    {
        if (gameObject != null)
        {
            circle.DOKill();
            outerCircle.transform.DOKill();
            NotePool.Instance.Release(noteType, gameObject);
        }
    }

    public override void Initialize(Transform noteHolder)
    {
        GetObjects();
        float animationDuration = (timeStamp - GameManager.Instance.currentTime) / 1000f;

        gameObject.transform.SetParent(noteHolder);
        head.localPosition = position;
        head.localScale = new Vector3(Values.noteRadius, Values.noteRadius, 1f);

        circle.color = Util.GetColor();
        circle.DOFade(1f, 0.2f).From(0f).SetEase(Ease.Linear);
        circle.sortingOrder = -nthNote;

        outerCircle.SetActive(true);
        SpriteRenderer outerSR = outerCircle.GetComponentInChildren<SpriteRenderer>();
        outerSR.color = Util.GetColor();
        outerSR.DOFade(1f, 0.2f).From(0f).SetEase(Ease.Linear);

        outerCircle.transform.DOScale(1f, animationDuration).From(animationDuration * 4f).SetEase(Ease.Linear).OnKill(() => outerCircle.SetActive(false));

        isFading = false;
    }
}

public class Slide : Tap
{
    public float beatInterval;
    public float duration;
    public float fixedLength;
    public List<SlideSegment> slideSegments = new();
    public List<float> segmentLengthRatio = new();

    public int tick = 1;

    public HoldCurve curveDrawer;
    public Material curveMaterial;

    protected override void GetObjects()
    {
        gameObject = NotePool.Instance.Get(noteType);
        head = gameObject.transform.Find("Head");
        circle = head.Find("Circle").GetComponent<SpriteRenderer>();
        outerCircle = head.Find("OuterCircle").gameObject;
        curveDrawer = gameObject.transform.Find("PathRenderer").GetComponent<HoldCurve>();
        curveMaterial = curveDrawer.GetComponent<MeshRenderer>().material;
    }

    public override void FadeOut()
    {
        if (gameObject != null && !isFading)
        {
            isFading = true;
            circle.DOFade(0, 0.2f).OnComplete(
                () =>
                {
                    Release();
                }
            );

            curveMaterial.DOFade(0, 0.2f);

            outerCircle.SetActive(false);
        }
    }

    public override void PopOut()
    {
        if (gameObject != null)
        {
            circle.DOKill();
            outerCircle.transform.DOKill();
            curveMaterial.DOKill();
            circle.DOFade(0f, 0.1f).SetEase(Ease.OutQuad);
            circle.transform.DOScale(1.5f, 0.1f).SetEase(Ease.OutQuad);
            curveMaterial.DOFade(0f, 0.1f).SetEase(Ease.OutQuad);
            DOTween.To(() => curveDrawer.width, x => {
                curveDrawer.width = x;
                curveDrawer.RenderPath(slideSegments);
            }, curveDrawer.width * 1.5f, 0.1f).SetEase(Ease.OutQuad);
        }
    }

    public override void Release()
    {
        if (gameObject != null)
        {
            circle.DOKill();
            outerCircle.transform.DOKill();
            curveMaterial.DOKill();
            NotePool.Instance.Release(noteType, gameObject);
        }
    }

    public override void Initialize(Transform noteHolder)
    {
        base.Initialize(noteHolder);
        gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        curveDrawer.width = 2 * Values.noteRadius;
        curveDrawer.RenderPath(slideSegments);

        curveMaterial.color = Util.GetColor();
        curveMaterial.DOFade(1f, 0.2f).From(0f).SetEase(Ease.Linear);
    }

    public void PreProcessSegments()
    {
        float length = 0;
        foreach (var segment in slideSegments)
        {
            length += segment.GetLength();
            segmentLengthRatio.Add(length / fixedLength);
            if (length > fixedLength)
            {
                segment.maxT = (fixedLength - (length - segment.GetLength())) / segment.GetLength();
            }
        }
    }

    public void UpdatePosition(int time, out Vector2 position)
    {
        position = PointAt(time);
        head.localPosition = position;
    }

    public Vector2 PointAt(int time)
    {
        float t = (time - timeStamp) / duration;
        t = Mathf.Clamp(t, 0f, 1f);

        float pre = 0f;
        for (int i = 0; i < segmentLengthRatio.Count; i++)
        {
            float ratio = segmentLengthRatio[i];
            if (t <= ratio)
            {
                SlideSegment currSeg = slideSegments[i];
                Vector2 position = currSeg.PointAt((t - pre) / (ratio - pre));
                return position;
            }
            pre = ratio;
        }
        return slideSegments[^1].PointAt(slideSegments[^1].maxT);
    }
}

public class SlideSegment
{
    public Vector3[] points;
    public SlideShape shape;
    private float length;
    public float maxT = 1;

    public Vector3 PointAt(float t)
    {
        return Util.GetCurveFunc(shape)(t, points);
    }

    public float GetLength()
    {
        if (length == 0)
        {
            for (int i = 0; i < 50; i++) // segment count = 50
            {
                length += Vector2.Distance(PointAt(i / 50f), PointAt((i + 1) / 50f));
            }
        }
        return length;
    }
}

public enum SlideShape
{
    Bezier,
    Linear,
    Circle
}

public enum NoteType
{
    Tap,
    Slide,
}

public class Chart
{
    public List<Note> notes;
}
