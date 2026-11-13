using UnityEngine;
using UnityEngine.UI;

public class EndingPanel : MonoBehaviour
{
    public bool IsShowing { get; private set; }
    private Text titleText;

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

    public void Show(string title)
    {
        if (titleText != null) titleText.text = title;
        gameObject.SetActive(true);
        IsShowing = true;
        Time.timeScale = 1f;
    }
}
