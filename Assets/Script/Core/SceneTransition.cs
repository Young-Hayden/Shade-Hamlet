using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 跨场景黑场过渡（单例，DontDestroyOnLoad）。
/// 流程：黑场淡出 → 章节标题展示（含最短停留）→ 异步加载场景 → 淡入。
/// 过渡期间 BlocksGameplay 为 true，主角移动与剧情开场应等待 WaitForGameplayReady。
/// </summary>
public class SceneTransition : MonoBehaviour
{
    /// <summary>全局唯一实例；首次请求时自动创建。</summary>
    private static SceneTransition instance;

    /// <summary>整个过渡遮罩的透明度控制。</summary>
    private CanvasGroup group;
    /// <summary>章节标题文本（例如「第一章 · 回村的人」）。</summary>
    private Text titleText;
    /// <summary>标题下方的副标题/操作提示文本。</summary>
    private Text subtitleText;
    /// <summary>是否正在执行一次过渡；用该标记拦截重复切换请求。</summary>
    private bool busy;
    /// <summary>新场景是否已经可以开始玩法逻辑（淡入结束后置 true）。</summary>
    private bool gameplayReady = true;

    /// <summary>当前是否处于场景过渡中。</summary>
    public static bool IsTransitioning { get { return instance != null && instance.busy; } }
    /// <summary>过渡期间是否应阻断玩家操作。</summary>
    public static bool BlocksGameplay { get { return instance != null && instance.busy; } }

    /// <summary>
    /// 发起一次带黑场的场景切换。
    /// </summary>
    /// <param name="sceneName">目标场景名（Build Settings 中的名称）。</param>
    /// <param name="title">可选章节标题；为空则不显示。</param>
    /// <param name="subtitle">可选副标题；为空则不显示。</param>
    public static void LoadScene(string sceneName, string title = null, string subtitle = null)
    {
        EnsureInstance();
        // 过渡中再次请求会被忽略，避免连续点击导致场景加载竞态。
        if (instance.busy)
        {
            Debug.LogWarning("SceneTransition: 已在切换中，忽略重复请求 " + sceneName);
            return;
        }
        instance.gameObject.SetActive(true);
        instance.StartCoroutine(instance.Run(sceneName, title, subtitle));
    }

    /// <summary>等待过渡结束、新场景可以开始玩法逻辑；未在过渡中时立即返回。</summary>
    public static IEnumerator WaitForGameplayReady()
    {
        while (instance != null && !instance.gameplayReady) yield return null;
    }

    /// <summary>确保单例存在；首次调用时创建对象并构建遮罩 UI。</summary>
    private static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("SceneTransition");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneTransition>();
        instance.Build();
    }

    /// <summary>构建全屏遮罩 Canvas 与标题文本；构建后默认隐藏，等待过渡时激活。</summary>
    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 排序值拉满，确保遮罩盖住所有游戏内 Canvas。
        canvas.sortingOrder = 9999;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        // 过渡期间拦截点击，防止玩家点到场内 UI。
        group.blocksRaycasts = true;
        group.interactable = false;

        // 纯黑底图，运行时铺满屏幕。
        var image = new GameObject("Black", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(transform, false);
        var rt = image.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = image.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = true;

        titleText = CreateText("Title", new Vector2(0f, 40f), 84, new Color(1f, 0.88f, 0.62f));
        subtitleText = CreateText("Subtitle", new Vector2(0f, -40f), 30, new Color(0.92f, 0.88f, 0.78f));
        gameObject.SetActive(false);
    }

    /// <summary>创建一个带阴影、居中显示的文本控件。</summary>
    private Text CreateText(string name, Vector2 position, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(1200f, 120f);
        var text = go.GetComponent<Text>();
        text.font = RpgUI.DefaultFont;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(3f, -3f);
        return text;
    }

    /// <summary>过渡主协程：淡出、加载、保证标题停留时间、淡入并恢复玩法。</summary>
    private IEnumerator Run(string sceneName, string title, string subtitle)
    {
        busy = true;
        gameplayReady = false;
        gameObject.SetActive(true);
        group.alpha = 0f;
        titleText.text = title ?? "";
        subtitleText.text = subtitle ?? "";
        yield return Fade(0f, 1f, 0.7f);

        // 记录开始时间，用于保证标题至少显示 1 秒（加载过快时补足）。
        float holdStart = Time.unscaledTime;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            // 场景名无效时给出可见错误，并安全退出过渡，避免卡死黑屏。
            Debug.LogError("SceneTransition: 无法加载场景 " + sceneName);
            titleText.text = "场景加载失败";
            subtitleText.text = sceneName;
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Fade(1f, 0f, 0.5f);
            gameObject.SetActive(false);
            busy = false;
            gameplayReady = true;
            yield break;
        }
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        float remain = 1.0f - (Time.unscaledTime - holdStart);
        if (remain > 0f) yield return new WaitForSecondsRealtime(remain);
        // 额外停留 0.25 秒，避免黑场一闪而过。
        yield return new WaitForSecondsRealtime(0.25f);
        yield return Fade(1f, 0f, 0.8f);

        gameObject.SetActive(false);
        busy = false;
        gameplayReady = true;
    }

    /// <summary>使用不受 timeScale 影响的实时时间做透明度渐变。</summary>
    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
