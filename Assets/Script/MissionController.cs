using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MissionStage
{
    CollectIntel,
    Escape,
    Completed
}

public class MissionController : MonoBehaviour
{
    public static MissionController Instance { get; private set; }
    public MissionStage Stage { get; private set; }
    public bool HasIntel { get { return Stage != MissionStage.CollectIntel; } }
    private GameObject victoryPanel;

    void Awake()
    {
        Instance = this;
        Stage = MissionStage.CollectIntel;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void RegisterIntel()
    {
        if (Stage != MissionStage.CollectIntel) return;
        Stage = MissionStage.Escape;
    }

    public bool TryExtract()
    {
        if (Stage != MissionStage.Escape) return false;
        Stage = MissionStage.Completed;
        ShowVictory();
        return true;
    }

    public string GetObjectiveText()
    {
        switch (Stage)
        {
            case MissionStage.CollectIntel: return "目标：取得卷轴 0 / 1";
            case MissionStage.Escape: return "目标：前往撤离点";
            default: return "任务完成：已安全撤离";
        }
    }

    private void ShowVictory()
    {
        if (victoryPanel != null) return;
        var flow = GameFlow.Instance;
        var canvas = RpgUI.CreateCanvas("MissionVictoryCanvas", transform, true);
        victoryPanel = canvas.gameObject;
        var dim = RpgUI.CreateImage("Dim", canvas.transform, null, new Color(0f, 0f, 0f, 0.68f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.sizeDelta = Vector2.zero;
        var panel = RpgUI.CreateImage("Panel", canvas.transform, flow != null ? flow.panelSprite : null, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 300f));
        RpgUI.CreateText("Title", panel.transform, "任务完成", 42, new Color(1f, 0.9f, 0.72f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(500f, 70f));
        RpgUI.CreateButton("RestartButton", panel.transform, flow != null ? flow.buttonSprite : null, "重新挑战", new Vector2(-130f, -55f), new Vector2(220f, 64f))
            .onClick.AddListener(() => { if (flow != null) flow.RestartGame(); else SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
        RpgUI.CreateButton("MainMenuButton", panel.transform, flow != null ? flow.buttonSprite : null, "返回主界面", new Vector2(130f, -55f), new Vector2(220f, 64f))
            .onClick.AddListener(() => { if (flow != null) flow.ReturnToMain(); else SceneManager.LoadScene("界面"); });
    }
}
