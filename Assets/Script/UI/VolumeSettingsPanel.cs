using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音量设置面板：主音量滑条 + 百分比显示 + 关闭按钮。
/// 打开时读取 AudioSettings 的当前值；拖动实时应用；关闭时写盘持久化。
/// </summary>
public class VolumeSettingsPanel : MonoBehaviour
{
    /// <summary>音量滑条。</summary>
    private Slider slider;
    /// <summary>百分比文本。</summary>
    private Text valueText;

    /// <summary>面板是否打开。</summary>
    public bool IsOpen { get { return gameObject.activeSelf; } }

    /// <summary>运行时创建音量面板（默认隐藏）。</summary>
    public static VolumeSettingsPanel Create(Transform parent, Sprite panelSprite, Sprite buttonSprite)
    {
        var go = new GameObject("VolumeSettingsPanel", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var panel = go.AddComponent<VolumeSettingsPanel>();
        panel.Build(panelSprite, buttonSprite);
        return panel;
    }

    /// <summary>构建全屏遮罩、面板、标题、标签、百分比、滑条与关闭按钮。</summary>
    private void Build(Sprite panelSprite, Sprite buttonSprite)
    {
        // 根节点铺满父 Canvas，自带半透明遮罩。
        var root = (RectTransform)transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        var dim = gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.78f);
        dim.raycastTarget = true;

        var box = RpgUI.CreateImage("Panel", transform, panelSprite, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 380f));
        RpgUI.CreateText("Title", box.transform, "设置", 38, new Color(1f, 0.92f, 0.7f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(500f, 56f));
        RpgUI.CreateText("Label", box.transform, "主音量", 26, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 42f), new Vector2(200f, 40f));
        valueText = RpgUI.CreateText("Value", box.transform, "100%", 26, new Color(1f, 0.88f, 0.62f), TextAnchor.MiddleRight,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, 42f), new Vector2(120f, 40f));

        // 滑条：RpgUI 默认不可交互，这里显式开启并补充拖拽手柄。
        slider = RpgUI.CreateSlider("VolumeSlider", box.transform, buttonSprite, new Color(0.95f, 0.76f, 0.4f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(420f, 30f));
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = true;
        var handle = RpgUI.CreateImage("Handle", slider.transform, buttonSprite, new Color(1f, 0.91f, 0.68f),
            new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
        handle.raycastTarget = false;
        slider.handleRect = handle.rectTransform;
        slider.onValueChanged.AddListener(OnValueChanged);

        var close = RpgUI.CreateButton("VolumeClose", box.transform, buttonSprite, "关闭",
            new Vector2(0f, -115f), new Vector2(220f, 56f));
        close.GetComponentInChildren<Text>().fontSize = 26;
        close.onClick.AddListener(Close);

        gameObject.SetActive(false);
    }

    /// <summary>打开面板：置顶、同步滑条与百分比到当前音量。</summary>
    public void Open()
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        // 静默设置，避免打开时触发一次不必要的写盘。
        slider.SetValueWithoutNotify(AudioSettings.MasterVolume);
        UpdateValue(AudioSettings.MasterVolume);
    }

    /// <summary>关闭面板：保存 PlayerPrefs。</summary>
    public void Close()
    {
        AudioSettings.Save();
        gameObject.SetActive(false);
    }

    /// <summary>滑条变化：实时应用主音量并刷新百分比。</summary>
    private void OnValueChanged(float value)
    {
        AudioSettings.SetMasterVolume(value);
        UpdateValue(value);
    }

    /// <summary>把 0～1 音量显示为整数百分比。</summary>
    private void UpdateValue(float value)
    {
        if (valueText != null) valueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
