using UnityEngine;
using UnityEngine.UI;

public static class RpgUI
{
    private static Font defaultFont;
    public static Font DefaultFont => defaultFont != null ? defaultFont : defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

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

    public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var image = CreateRect(name, parent, anchor, pivot, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

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

    public static Slider CreateSlider(string name, Transform parent, Sprite sprite, Color fillColor, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var rect = CreateRect(name, parent, anchor, pivot, position, size);
        var slider = rect.gameObject.AddComponent<Slider>();
        var background = CreateImage("Background", rect, sprite, new Color(0.08f, 0.08f, 0.08f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.sizeDelta = Vector2.zero;
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

    public static Button CreateButton(string name, Transform parent, Sprite sprite, string label, Vector2 position, Vector2 size)
    {
        var image = CreateImage(name, parent, sprite, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        var button = image.gameObject.AddComponent<Button>();
        CreateText("Label", image.transform, label, 26, new Color(1f, 0.94f, 0.78f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return button;
    }
}
