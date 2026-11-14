using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 跨场景黑场过渡：淡出 → 章节标题/最短停留 → 异步加载 → 淡入。
public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;

    private CanvasGroup group;
    private Text titleText;
    private Text subtitleText;
    private bool busy;
    private bool gameplayReady = true;

    public static bool IsTransitioning { get { return instance != null && instance.busy; } }
    public static bool BlocksGameplay { get { return instance != null && instance.busy; } }

    public static void LoadScene(string sceneName, string title = null, string subtitle = null)
    {
        EnsureInstance();
        if (instance.busy)
        {
            Debug.LogWarning("SceneTransition: 已在切换中，忽略重复请求 " + sceneName);
            return;
        }
        instance.gameObject.SetActive(true);
        instance.StartCoroutine(instance.Run(sceneName, title, subtitle));
    }

    public static IEnumerator WaitForGameplayReady()
    {
        while (instance != null && !instance.gameplayReady) yield return null;
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("SceneTransition");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneTransition>();
        instance.Build();
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;
        group.interactable = false;

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

    private IEnumerator Run(string sceneName, string title, string subtitle)
    {
        busy = true;
        gameplayReady = false;
        gameObject.SetActive(true);
        group.alpha = 0f;
        titleText.text = title ?? "";
        subtitleText.text = subtitle ?? "";
        yield return Fade(0f, 1f, 0.7f);

        float holdStart = Time.unscaledTime;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
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
        yield return new WaitForSecondsRealtime(0.25f);
        yield return Fade(1f, 0f, 0.8f);

        gameObject.SetActive(false);
        busy = false;
        gameplayReady = true;
    }

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
