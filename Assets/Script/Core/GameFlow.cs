using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 全局游戏流程管理（单例）。
/// 负责：记录击杀数、处理主角死亡、生成死亡面板、重新挑战与返回主界面。
/// 挂在 RPG游戏 场景的 GameSystems 节点下；死亡面板为运行时生成，不写入场景。
/// </summary>
public class GameFlow : MonoBehaviour
{
    /// <summary>全局唯一实例；场景内脚本通过它上报击杀或查询击杀数。</summary>
    public static GameFlow Instance { get; private set; }

    /// <summary>死亡面板的背景底图（在 Inspector 中指定，缺省时使用纯色）。</summary>
    public Sprite panelSprite;
    /// <summary>死亡面板按钮的底图。</summary>
    public Sprite buttonSprite;

    /// <summary>已生成的死亡面板根对象；非空表示主角正处于死亡结算界面。</summary>
    private GameObject deathPanel;

    /// <summary>死亡面板是否已经显示，供测试与其他系统判断。</summary>
    public bool IsDeathPanelShown { get { return deathPanel != null; } }

    /// <summary>已击杀敌人的 ID 集合；用集合去重，同一敌人重复上报只计一次。</summary>
    private readonly System.Collections.Generic.HashSet<int> killedEnemies = new System.Collections.Generic.HashSet<int>();

    /// <summary>当前击杀数量。</summary>
    public int KillCount { get { return killedEnemies.Count; } }

    /// <summary>旧搜打撤模式的目标击杀数（剧情模式下仅用于 HUD 兼容）。</summary>
    public int KillTarget = 1;

    /// <summary>敌人死亡时调用；以敌人实例 ID 去重后计入击杀数。</summary>
    public void RegisterEnemyKill(int enemyId) { killedEnemies.Add(enemyId); }

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>
    /// 主角死亡回调：先让剧情系统保存检查点，再弹出死亡面板。
    /// 面板只创建一次，重复调用直接返回，避免叠加多个面板。
    /// </summary>
    public void OnHeroDeath()
    {
        if (deathPanel != null) return;

        // 剧情进度先落到内存检查点，重新挑战时由 StoryDirector 恢复。
        if (StoryDirector.Instance != null) StoryDirector.Instance.CaptureCheckpoint();

        // 死亡界面全部运行时生成，不依赖场景预制体。
        var canvas = RpgUI.CreateCanvas("DeathCanvas", transform, true);
        deathPanel = canvas.gameObject;
        // 半透明黑色遮罩，铺满屏幕。
        var dim = RpgUI.CreateImage("Dim", canvas.transform, null, new Color(0f, 0f, 0f, 0.68f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.sizeDelta = Vector2.zero;
        // 中央面板：标题 +「重新挑战」/「返回主界面」两个按钮。
        var panel = RpgUI.CreateImage("Panel", canvas.transform, panelSprite, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 300f));
        RpgUI.CreateText("Title", panel.transform, "你已倒下", 42, new Color(1f, 0.9f, 0.72f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(500f, 70f));
        RpgUI.CreateButton("RestartButton", panel.transform, buttonSprite, "重新挑战", new Vector2(-130f, -55f), new Vector2(220f, 64f))
            .onClick.AddListener(RestartGame);
        RpgUI.CreateButton("MainMenuButton", panel.transform, buttonSprite, "返回主界面", new Vector2(130f, -55f), new Vector2(220f, 64f))
            .onClick.AddListener(ReturnToMain);
    }

    /// <summary>重新挑战：恢复时间缩放后，通过过渡重新加载当前场景（检查点保留）。</summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneTransition.LoadScene(SceneManager.GetActiveScene().name, "第一章 · 回村的人", "重新开始");
    }

    /// <summary>返回主界面：清空跨章节会话状态，再加载主菜单场景。</summary>
    public void ReturnToMain()
    {
        Time.timeScale = 1f;
        StorySession.Reset();
        SceneTransition.LoadScene("界面", "影村", "");
    }
}
