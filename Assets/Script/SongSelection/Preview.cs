using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Preview : MonoBehaviour
{
    public TextMeshProUGUI title;
    public Image jacket;
    public Sprite placeholderSprite;
    public GameObject[] diffElement;
    public GameObject selectionBox;
    public AudioSource conductor;

    private Dictionary<string, AudioClip> audioCache = new();
    private Dictionary<string, Sprite> jacketCache = new();

    void Start()
    {
        for (int diff = 0; diff < diffElement.Length; diff++)
        {
            int currentDiff = diff;
            diffElement[diff].GetComponent<Button>().onClick.AddListener(() => Select(currentDiff));
        }
    }

    public void PreloadResources(string id)
    {
        // 预加载音频
        var audioClip = Resources.Load<AudioClip>($"Songs/{id}/track");
        if (audioClip != null)
        {
            audioCache[id] = audioClip;
            audioClip.LoadAudioData();
        }
        else
        {
            Debug.LogError($"未能加载音频文件: {id}");
        }

        // 预加载封面图片
        var jacketSprite = Resources.Load<Sprite>($"Songs/{id}/jacket");
        if (jacketSprite != null)
        {
            jacketCache[id] = jacketSprite;
        }
        else
        {
            jacketCache[id] = placeholderSprite;
        }
    }

    public void UpdatePreview(Meta meta)
    {
        title.text = meta.title;
        PlayStats.meta = meta;

        // 设置封面
        jacket.sprite = jacketCache.TryGetValue(meta.id, out var sprite) ? sprite : placeholderSprite;

        // 播放音乐
        if (audioCache.TryGetValue(meta.id, out AudioClip clip))
        {
            conductor.clip = clip;
            conductor.Stop();
            conductor.time = meta.previewStart / 1000f;
            conductor.Play();
        }
        else
        {
            Debug.LogError($"未能找到歌曲音频: {meta.id}");
        }

        // 设置难度
        for (int i = 0; i < meta.level.Length; i++)
        {
            string diff = meta.level[i];
            if (diff != "")
            {
                TextMeshProUGUI diff_text = diffElement[i].transform.Find("Difficulty").GetComponent<TextMeshProUGUI>();
                diffElement[i].SetActive(true);
                diff_text.text = diff;
            }
            else
            {
                diffElement[i].SetActive(false);
            }
        }

        Select(PlayStats.diff);
    }

    Tween currentTween;
    public void Select(int diff)
    {
        if (!diffElement[diff].activeSelf)
        {
            // Don't select if difficulty element is not active. Instead select the previous diff
            int prevDiff = diff switch
            {
                0 => 3,
                3 => 2,
                2 => 1,
                1 => 0,
                _ => 0
            };
            Select(prevDiff);
            return;
        }

        PlayStats.diff = diff;

        // Get the target position from the difficulty element
        Vector3 targetPosition = diffElement[diff].transform.position;

        currentTween?.Kill();

        // Animate the selection box to the new position
        currentTween = selectionBox.transform.DOMove(targetPosition, 0.2f)
            .SetEase(Ease.OutQuad);
    }
}
