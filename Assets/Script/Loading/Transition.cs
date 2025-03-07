using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class Transition : MonoBehaviour
{
    RectTransform transitionPanel; // 过场画面的RectTransform
    readonly float slideDuration = 0.5f; // 滑动持续时间
    Vector2 startPosition = new(0, -600); // 起始位置
    Vector2 endPosition = new(0, 0); // 终止位置

    void Start()
    {
        transitionPanel = GetComponent<RectTransform>();
        // 设置初始位置
        transitionPanel.anchoredPosition = startPosition;

        // 使用DOTween滑入动画
        transitionPanel.DOAnchorPos(endPosition, slideDuration).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            SceneManager.UnloadSceneAsync(Transfer.fromScene);
        });

        string sceneName = Transfer.toScene;
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        // 开始异步加载场景
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // 禁止场景立即切换，等待动画完成
        operation.allowSceneActivation = false;

        yield return new WaitForSeconds(1f);

        while (true)
        {
            if (operation.progress >= 0.9f)
            {
                // 使用DOTween滑出动画
                transitionPanel.DOAnchorPos(startPosition, 0.5f).SetEase(Ease.InCubic).OnComplete(
                    () =>
                    {
                        SceneManager.UnloadSceneAsync("LoadingScene");
                    }
                );
                operation.allowSceneActivation = true; // 激活场景
                break;
            }

            yield return null;
        }
    }
}