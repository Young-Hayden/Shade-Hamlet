using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 剧情结局面板：显示章节标题，并提供「重新挑战」与「返回主界面」。
/// 按钮直接复用 GameFlow 的流程方法。
/// </summary>
public class EndingPanel : MonoBehaviour
{
    /// <summary>面板是否正在显示。</summary>
    public bool IsShowing { get; private set; }
    /// <summary>标题文本。</summary>
    private Text titleText;

    /// <summary>运行时创建结局面板并挂载本组件。</summary>
    public static EndingPanel Create(Transform parent)
    {
        var canvas = RpgUI.CreateCanvas("StoryEndingCanvas", parent, true);
        var panel = canvas.gameObject.AddComponent<EndingPanel>();
        var bg = RpgUI.CreateImage("Panel", canvas.transform, null, new Color(0.06f, 0.05f, 0.05f, 0.95f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 300f));
        panel.titleText = RpgUI.CreateText("Title", bg.transform, "第一章 完", 42, new Color(1f, 0.9f, 0.72f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(500f, 70f));
        var restartButton = RpgUI.CreateButton("RestartButton", bg.transform, null, "重新挑战", new Vector2(-130f, -55f), new Vector2(220f, 64f));
        restartButton.image.color = new Color(0.16f, 0.11f, 0.08f, 0.98f);
        restartButton.onClick.AddListener(() => { if (GameFlow.Instance != null) GameFlow.Instance.RestartGame(); });
        var mainMenuButton = RpgUI.CreateButton("MainMenuButton", bg.transform, null, "返回主界面", new Vector2(130f, -55f), new Vector2(220f, 64f));
        mainMenuButton.image.color = new Color(0.16f, 0.11f, 0.08f, 0.98f);
        mainMenuButton.onClick.AddListener(() => { if (GameFlow.Instance != null) GameFlow.Instance.ReturnToMain(); });
        canvas.gameObject.SetActive(false);
        return panel;
    }

    /// <summary>显示结局面板（恢复 timeScale，保证按钮可交互），并设置标题。</summary>
    public void Show(string title)
    {
        if (titleText != null) titleText.text = title;
        gameObject.SetActive(true);
        IsShowing = true;
        Time.timeScale = 1f;
    }
}
