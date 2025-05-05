using UnityEngine;
using TMPro;
using UnityEngine.Pool;
using DG.Tweening;

public class JudgeCenter : MonoBehaviour
{
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI scoreText;

    public RectTransform canvas;
    public GameObject judgeEffect;
    ObjectPool<GameObject> judgeEffectPool;

    int combo = 0;
    int referenceScore = 0;
    int score = 0;

    void Start()
    {
        GameStats.Clear();

        judgeEffectPool = new(() =>
        {
            return Instantiate(judgeEffect);
        }, effect =>
        {
            effect.SetActive(true);
        }, effect =>
        {
            effect.SetActive(false);
        }, effect =>
        {
            Destroy(effect);
        }, false, 6, 10);
    }

    public Judgment Judge(int timeDifference)
    {
        int time = System.Math.Abs(timeDifference);
        if (time <= Values.perfectWindow)
        {
            return Judgment.Perfect;
        }
        else if (time <= Values.goodWindow)
        {
            return Judgment.Good;
        }
        else
        {
            return Judgment.Bad;
        }
    }

    public void UpdateStat(Judgment judgment)
    {
        switch (judgment)
        {
            case Judgment.Perfect:
                combo++;
                score += 5;
                GameStats.perfectCount++;
                break;
            case Judgment.Good:
                combo++;
                score += 3;
                GameStats.goodCount++;
                break;
            case Judgment.Bad:
                combo = 0;
                score += 1;
                GameStats.badCount++;
                break;
            case Judgment.Miss:
                combo = 0;
                GameStats.missCount++;
                break;
        }
        referenceScore += 5;

        comboText.text = combo > 0 ? combo.ToString() : "";
        GameStats.acc = score / (float)referenceScore * 100;
        scoreText.text = GameStats.acc.ToString("F2") + '%';
    }

    readonly float moveDistance = 10;

    public void Show(Judgment judgment)
    {
        Vector2 position = new(0, 190);

        GameObject effect = judgeEffectPool.Get();
        RectTransform rectTransform = effect.GetComponent<RectTransform>();
        rectTransform.SetParent(canvas, false);
        rectTransform.anchoredPosition = position;

        TextMeshProUGUI judgeText = effect.GetComponent<TextMeshProUGUI>();
        judgeText.text = judgment.ToString();

        switch (judgment)
        {
            case Judgment.Perfect:
                judgeText.color = Color.yellow;
                break;
            case Judgment.Good:
                judgeText.color = Color.green;
                break;
            case Judgment.Bad:
                judgeText.color = Color.red;
                break;
            case Judgment.Miss:
                judgeText.color = Color.grey;
                break;
        }

        rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + moveDistance, 0.5f).OnComplete(() =>
        {
            judgeEffectPool.Release(effect);
        });
    }
}

public enum Judgment
{
    Perfect,
    Good,
    Bad,
    Miss
}