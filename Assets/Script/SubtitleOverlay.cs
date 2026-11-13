using System;
using UnityEngine;
using UnityEngine.UI;

public class SubtitleOverlay : MonoBehaviour
{
    public bool IsShowing { get; private set; }
    private Text label;
    private string[] lines;
    private int index;
    private Action onComplete;

    public static SubtitleOverlay Create(Transform parent)
    {
        var canvas = RpgUI.CreateCanvas("SubtitleCanvas", parent, false);
        var overlay = canvas.gameObject.AddComponent<SubtitleOverlay>();
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

    void Update() { if (IsShowing && Input.anyKeyDown) Advance(); }
}
