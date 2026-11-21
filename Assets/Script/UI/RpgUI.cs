using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 运行时 UI 工厂：统一创建 Canvas、图片、文本、滑条与按钮。
/// 所有动态 UI（HUD、对话框、死亡面板等）都通过这里构建，避免依赖场景预制体。
/// </summary>
public static class RpgUI
{
    /// <summary>默认字体缓存；使用 Unity 内置 LegacyRuntime.ttf。</summary>
    private static Font defaultFont;
    /// <summary>默认字体（首次访问时加载并缓存）。</summary>
    public static Font DefaultFont => defaultFont != null ? defaultFont : defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    /// <summary>创建一个带 RectTransform 的空节点，统一设置锚点、轴心、位置与尺寸。</summary>
    public static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        // layer 5 为 UI 层，保证被 UI 相机/射线正确处理。
        go.layer = 5;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    /// <summary>创建全屏 Canvas：ScaleWithScreenSize，参考分辨率 1920×1080。</summary>
    /// <param name="interactive">true 时添加 GraphicRaycaster，使子控件可点击。</param>
    public static Canvas CreateCanvas(string name, Transform parent, bool interactive)
    {
        var rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var canvas = rect.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = rect.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        if (interactive) rect.gameObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>创建图片控件。</summary>
    public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var image = CreateRect(name, parent, anchor, pivot, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    /// <summary>创建文本控件；默认不拦截射线，避免挡住按钮点击。</summary>
    public static Text CreateText(string name, Transform parent, string value, int fontSize, Color color, TextAnchor alignment, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var text = CreateRect(name, parent, anchor, pivot, position, size).gameObject.AddComponent<Text>();
        text.font = DefaultFont;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>
    /// 创建滑条（背景 + 填充）。默认不可交互，血条直接复用；
    /// 音量面板会显式开启交互并补充拖拽手柄。
    /// </summary>
    public static Slider CreateSlider(string name, Transform parent, Sprite sprite, Color fillColor, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var rect = CreateRect(name, parent, anchor, pivot, position, size);
        var slider = rect.gameObject.AddComponent<Slider>();
        // 背景铺满滑条区域。
        var background = CreateImage("Background", rect, sprite, new Color(0.08f, 0.08f, 0.08f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.sizeDelta = Vector2.zero;
        // 填充同样铺满，由 Slider 控制宽度。
        var fill = CreateImage("Fill", rect, sprite, fillColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.sizeDelta = Vector2.zero;
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = background;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        return slider;
    }

    /// <summary>创建按钮：底图 + 居中文字，返回 Button 以便绑定点击事件。</summary>
    public static Button CreateButton(string name, Transform parent, Sprite sprite, string label, Vector2 position, Vector2 size)
    {
        var image = CreateImage(name, parent, sprite, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        var button = image.gameObject.AddComponent<Button>();
        CreateText("Label", image.transform, label, 26, new Color(1f, 0.94f, 0.78f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return button;
    }
}
