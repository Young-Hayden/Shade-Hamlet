using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public InventoryUI inventoryUI;
    public Sprite panelSprite;
    public Sprite buttonSprite;
    public GameObject PausePanel;
    public Button ContinueButton;
    public Button RestartButton;
    public Button MainMenuButton;
    public Button QuitButton;
    public bool IsOpen { get { return PausePanel != null && PausePanel.activeSelf; } }

    void Awake()
    {
        if (inventoryUI == null) inventoryUI = FindObjectOfType<InventoryUI>();
        Build();
    }

    void Update() { if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape(); }

    public void HandleEscape()
    {
        if (GameFlow.Instance != null && GameFlow.Instance.IsDeathPanelShown) return;
        if (inventoryUI != null && inventoryUI.InventoryPanel.activeSelf)
        {
            inventoryUI.Close();
            return;
        }
        Toggle();
    }

    public void Toggle()
    {
        if (!IsOpen && GameFlow.Instance != null && GameFlow.Instance.IsDeathPanelShown) return;
        bool show = !IsOpen;
        PausePanel.SetActive(show);
        Time.timeScale = show ? 0f : 1f;
    }

    private void Build()
    {
        var canvas = RpgUI.CreateCanvas("PauseCanvas", transform, true);
        PausePanel = canvas.gameObject;
        var dim = RpgUI.CreateImage("Dim", canvas.transform, null, new Color(0f, 0f, 0f, 0.68f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.sizeDelta = Vector2.zero;
        var panel = RpgUI.CreateImage("Panel", canvas.transform, panelSprite, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 540f));
        RpgUI.CreateText("Title", panel.transform, "游戏菜单", 40, new Color(1f, 0.92f, 0.72f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(560f, 54f));
        ContinueButton = RpgUI.CreateButton("ContinueButton", panel.transform, buttonSprite, "继续", new Vector2(0f, 125f), new Vector2(300f, 64f));
        RestartButton = RpgUI.CreateButton("RestartButton", panel.transform, buttonSprite, "重新开始", new Vector2(0f, 45f), new Vector2(300f, 64f));
        MainMenuButton = RpgUI.CreateButton("MainMenuButton", panel.transform, buttonSprite, "返回主界面", new Vector2(0f, -35f), new Vector2(300f, 64f));
        QuitButton = RpgUI.CreateButton("QuitButton", panel.transform, buttonSprite, "退出游戏", new Vector2(0f, -115f), new Vector2(300f, 64f));
        RpgUI.CreateText("Controls", panel.transform, "WASD 移动 / Shift 奔跑\n右键攻击 / 中键旋转 / 滚轮缩放\nQ 药水 / I 背包 / G 烟雾 / Esc 继续", 21, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(560f, 100f));
        ContinueButton.onClick.AddListener(Toggle);
        RestartButton.onClick.AddListener(() => { Time.timeScale = 1f; GameFlow.Instance.RestartGame(); });
        MainMenuButton.onClick.AddListener(() => { Time.timeScale = 1f; GameFlow.Instance.ReturnToMain(); });
        QuitButton.onClick.AddListener(QuitGame);
        PausePanel.SetActive(false);
    }

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
