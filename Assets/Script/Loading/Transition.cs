using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class Transition : MonoBehaviour
{
    private RectTransform transitionPanel;
    private const float SLIDE_DURATION = 0.5f;
    private const float LOAD_DELAY = 1f;
    private static readonly Vector2 START_POSITION = new(0, -600);
    private static readonly Vector2 END_POSITION = Vector2.zero;

    void Start()
    {
        transitionPanel = GetComponent<RectTransform>();
        // 设置初始位置
        transitionPanel.anchoredPosition = START_POSITION;

        string sceneName = Transfer.toScene;

        // 使用DOTween滑入动画
        transitionPanel.DOAnchorPos(END_POSITION, SLIDE_DURATION).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            SceneManager.UnloadSceneAsync(Transfer.fromScene);
            StartCoroutine(LoadSceneAsync(sceneName));
        });
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        // 开始异步加载场景
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // 禁止场景立即切换，等待动画完成
        operation.allowSceneActivation = false;

        yield return new WaitForSeconds(LOAD_DELAY);

        // 等待加载完成
        yield return new WaitUntil(() => operation.progress >= 0.9f);

        // 使用DOTween滑出动画
        transitionPanel.DOAnchorPos(START_POSITION, SLIDE_DURATION)
            .SetEase(Ease.InCubic)
            .OnComplete(() => SceneManager.UnloadSceneAsync("LoadingScene"));
            
        operation.allowSceneActivation = true;
    }
}