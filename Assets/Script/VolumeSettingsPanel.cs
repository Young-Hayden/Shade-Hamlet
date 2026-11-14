using UnityEngine;
using UnityEngine.UI;

public class VolumeSettingsPanel : MonoBehaviour
{
    private Slider slider;
    private Text valueText;

    public bool IsOpen { get { return gameObject.activeSelf; } }

    public static VolumeSettingsPanel Create(Transform parent, Sprite panelSprite, Sprite buttonSprite)
    {
        var go = new GameObject("VolumeSettingsPanel", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var panel = go.AddComponent<VolumeSettingsPanel>();
        panel.Build(panelSprite, buttonSprite);
        return panel;
    }

    private void Build(Sprite panelSprite, Sprite buttonSprite)
    {
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

    public void Open()
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        slider.SetValueWithoutNotify(AudioSettings.MasterVolume);
        UpdateValue(AudioSettings.MasterVolume);
    }

    public void Close()
    {
        AudioSettings.Save();
        gameObject.SetActive(false);
    }

    private void OnValueChanged(float value)
    {
        AudioSettings.SetMasterVolume(value);
        UpdateValue(value);
    }

    private void UpdateValue(float value)
    {
        if (valueText != null) valueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
