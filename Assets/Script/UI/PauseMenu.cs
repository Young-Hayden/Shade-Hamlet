using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单：Esc 开关，提供继续、重新开始、音量设置、返回主界面、退出游戏。
/// Esc 的优先级：死亡面板 > 音量面板 > 背包 > 暂停菜单。
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>背包 UI，用于 Esc 优先关闭背包。</summary>
    public InventoryUI inventoryUI;
    /// <summary>面板底图。</summary>
    public Sprite panelSprite;
    /// <summary>按钮底图。</summary>
    public Sprite buttonSprite;
    /// <summary>暂停面板根对象。</summary>
    public GameObject PausePanel;
    /// <summary>继续按钮。</summary>
    public Button ContinueButton;
    /// <summary>重新开始按钮。</summary>
    public Button RestartButton;
    /// <summary>音量设置按钮。</summary>
    public Button VolumeButton;
    /// <summary>返回主界面按钮。</summary>
    public Button MainMenuButton;
    /// <summary>退出游戏按钮。</summary>
    public Button QuitButton;
    /// <summary>音量面板实例。</summary>
    private VolumeSettingsPanel volumePanel;
    /// <summary>全局唯一实例，供字幕/对话等系统判断暂停状态。</summary>
    public static PauseMenu Instance { get; private set; }
    /// <summary>暂停菜单是否打开。</summary>
    public bool IsOpen { get { return PausePanel != null && PausePanel.activeSelf; } }

    void Awake()
    {
        Instance = this;
        if (inventoryUI == null) inventoryUI = FindObjectOfType<InventoryUI>();
        Build();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update() { if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape(); }

    /// <summary>
    /// Esc 处理顺序：死亡时忽略；音量面板打开则关闭它；
    /// 背包打开则关闭背包；否则开关暂停菜单。
    /// </summary>
    public void HandleEscape()
    {
        if (GameFlow.Instance != null && GameFlow.Instance.IsDeathPanelShown) return;
        if (volumePanel != null && volumePanel.IsOpen)
        {
            volumePanel.Close();
            return;
        }
        if (inventoryUI != null && inventoryUI.InventoryPanel.activeSelf)
        {
            inventoryUI.Close();
            return;
        }
        Toggle();
    }

    /// <summary>开关暂停菜单，并同步暂停 timeScale；剧情暂停时关闭菜单仍保持暂停。</summary>
    public void Toggle()
    {
        // 场景过渡期间不允许打开暂停菜单。
        if (SceneTransition.IsTransitioning) return;
        if (!IsOpen && GameFlow.Instance != null && GameFlow.Instance.IsDeathPanelShown) return;
        bool show = !IsOpen;
        PausePanel.SetActive(show);
        if (show)
        {
            Time.timeScale = 0f;
        }
        else
        {
            // 关闭暂停时如果剧情仍在暂停，保持 timeScale=0，由 StoryDirector 负责最终恢复。
            bool storyPaused = StoryDirector.Instance != null && StoryDirector.Instance.IsGameplayPaused;
            Time.timeScale = storyPaused ? 0f : 1f;
        }
    }

    /// <summary>构建暂停菜单控件与按钮事件。</summary>
    private void Build()
    {
        var canvas = RpgUI.CreateCanvas("PauseCanvas", transform, true);
        PausePanel = canvas.gameObject;
        var dim = RpgUI.CreateImage("Dim", canvas.transform, null, new Color(0f, 0f, 0f, 0.68f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.sizeDelta = Vector2.zero;
        var panel = RpgUI.CreateImage("Panel", canvas.transform, panelSprite, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 650f));
        RpgUI.CreateText("Title", panel.transform, "游戏菜单", 40, new Color(1f, 0.92f, 0.72f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(560f, 54f));
        ContinueButton = RpgUI.CreateButton("ContinueButton", panel.transform, buttonSprite, "继续", new Vector2(0f, 170f), new Vector2(300f, 60f));
        RestartButton = RpgUI.CreateButton("RestartButton", panel.transform, buttonSprite, "重新开始", new Vector2(0f, 90f), new Vector2(300f, 60f));
        VolumeButton = RpgUI.CreateButton("VolumeButton", panel.transform, buttonSprite, "音量设置", new Vector2(0f, 10f), new Vector2(300f, 60f));
        MainMenuButton = RpgUI.CreateButton("MainMenuButton", panel.transform, buttonSprite, "返回主界面", new Vector2(0f, -70f), new Vector2(300f, 60f));
        QuitButton = RpgUI.CreateButton("QuitButton", panel.transform, buttonSprite, "退出游戏", new Vector2(0f, -150f), new Vector2(300f, 60f));
        RpgUI.CreateText("Controls", panel.transform, "WASD 移动 / Shift 奔跑\n右键攻击 / 鼠标视角 / 滚轮缩放\nQ 药水 / I 背包 / G 烟雾 / Esc 继续", 21, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(560f, 90f));
        ContinueButton.onClick.AddListener(Toggle);
        RestartButton.onClick.AddListener(() => { Time.timeScale = 1f; GameFlow.Instance.RestartGame(); });
        // 音量面板作为暂停画布的子层，层级更靠前。
        volumePanel = VolumeSettingsPanel.Create(canvas.transform, panelSprite, buttonSprite);
        VolumeButton.onClick.AddListener(volumePanel.Open);
        MainMenuButton.onClick.AddListener(() => { Time.timeScale = 1f; GameFlow.Instance.ReturnToMain(); });
        QuitButton.onClick.AddListener(QuitGame);
        PausePanel.SetActive(false);
    }

    /// <summary>退出游戏：编辑器内停止 Play，打包后退出应用。</summary>
    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
