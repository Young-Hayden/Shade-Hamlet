using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>旧搜打撤任务阶段。</summary>
public enum MissionStage
{
    /// <summary>收集情报/卷轴。</summary>
    CollectIntel,
    /// <summary>前往撤离点。</summary>
    Escape,
    /// <summary>已完成。</summary>
    Completed
}

/// <summary>
/// 旧「取卷轴 → 撤离」任务控制器（单例）。
/// 剧情模式第一章已用 StoryDirector 取代本任务；场景中该组件处于停用状态，保留以兼容旧流程与 HUD。
/// </summary>
public class MissionController : MonoBehaviour
{
    /// <summary>全局唯一实例。</summary>
    public static MissionController Instance { get; private set; }
    /// <summary>当前任务阶段。</summary>
    public MissionStage Stage { get; private set; }
    /// <summary>是否已取得情报（阶段已越过收集）。</summary>
    public bool HasIntel { get { return Stage != MissionStage.CollectIntel; } }
    /// <summary>胜利面板根对象。</summary>
    private GameObject victoryPanel;

    void Awake()
    {
        Instance = this;
        Stage = MissionStage.CollectIntel;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>登记已取得卷轴，进入撤离阶段。</summary>
    public void RegisterIntel()
    {
        if (Stage != MissionStage.CollectIntel) return;
        Stage = MissionStage.Escape;
    }

    /// <summary>尝试撤离；仅在撤离阶段有效，成功后显示胜利面板。</summary>
    public bool TryExtract()
    {
        if (Stage != MissionStage.Escape) return false;
        Stage = MissionStage.Completed;
        ShowVictory();
        return true;
    }

    /// <summary>返回当前阶段的任务目标文本（HUD 兼容）。</summary>
    public string GetObjectiveText()
    {
        switch (Stage)
        {
            case MissionStage.CollectIntel: return "目标：取得卷轴 0 / 1";
            case MissionStage.Escape: return "目标：前往撤离点";
            default: return "任务完成：已安全撤离";
        }
    }

    /// <summary>生成胜利面板：标题 + 重新挑战/返回主界面；面板只创建一次。</summary>
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
        // GameFlow 缺失时退回直接加载场景，保证按钮仍可用。
        RpgUI.CreateButton("RestartButton", panel.transform, flow != null ? flow.buttonSprite : null, "重新挑战", new Vector2(-130f, -55f), new Vector2(220f, 64f))
            .onClick.AddListener(() => { if (flow != null) flow.RestartGame(); else SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
        RpgUI.CreateButton("MainMenuButton", panel.transform, flow != null ? flow.buttonSprite : null, "返回主界面", new Vector2(130f, -55f), new Vector2(220f, 64f))
            .onClick.AddListener(() => { if (flow != null) flow.ReturnToMain(); else SceneManager.LoadScene("界面"); });
    }
}
