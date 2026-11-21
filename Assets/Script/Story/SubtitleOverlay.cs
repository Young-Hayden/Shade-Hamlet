using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开场/结局字幕层：全屏黑底 + 居中字幕，按任意键逐句推进，结束后触发回调。
/// 在 timeScale=0 下仍可工作（输入不受时间缩放影响）。
/// </summary>
public class SubtitleOverlay : MonoBehaviour
{
    /// <summary>字幕是否正在显示。</summary>
    public bool IsShowing { get; private set; }
    /// <summary>当前字幕文本。</summary>
    private Text label;
    /// <summary>待播放的字幕行。</summary>
    private string[] lines;
    /// <summary>当前行索引。</summary>
    private int index;
    /// <summary>全部播放完后的回调。</summary>
    private Action onComplete;

    /// <summary>运行时创建全屏字幕层并挂载本组件。</summary>
    public static SubtitleOverlay Create(Transform parent)
    {
        var canvas = RpgUI.CreateCanvas("SubtitleCanvas", parent, false);
        var overlay = canvas.gameObject.AddComponent<SubtitleOverlay>();
        // 全黑底图，遮住场景以突出叙事文本。
        var dim = RpgUI.CreateImage("Dim", canvas.transform, null, new Color(0f, 0f, 0f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.sizeDelta = Vector2.zero;
        overlay.label = RpgUI.CreateText("Line", canvas.transform, "", 34, new Color(0.94f, 0.9f, 0.82f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 400f));
        RpgUI.CreateText("Hint", canvas.transform, "按任意键继续", 20, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(600f, 40f));
        canvas.gameObject.SetActive(false);
        return overlay;
    }

    /// <summary>开始播放字幕；空数组时立即回调。</summary>
    public void Show(string[] lines, Action onComplete)
    {
        this.lines = lines ?? new string[0];
        this.onComplete = onComplete;
        index = 0;
        if (this.lines.Length == 0) { onComplete?.Invoke(); return; }
        gameObject.SetActive(true);
        IsShowing = true;
        label.text = this.lines[0];
    }

    /// <summary>推进到下一行；最后一行结束后关闭并回调。</summary>
    public void Advance()
    {
        if (!IsShowing) return;
        index++;
        if (index >= lines.Length)
        {
            IsShowing = false;
            gameObject.SetActive(false);
            var done = onComplete;
            onComplete = null;
            done?.Invoke();
            return;
        }
        label.text = lines[index];
    }

    void Update()
    {
        if (!IsShowing) return;
        // 暂停菜单打开时，用于打开暂停的按键不应同时推进字幕。
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) return;
        if (Input.anyKeyDown) Advance();
    }
}
