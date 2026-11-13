using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }
    public Sprite panelSprite;
    public Sprite buttonSprite;
    private GameObject deathPanel;
    public bool IsDeathPanelShown { get { return deathPanel != null; } }
    private readonly System.Collections.Generic.HashSet<int> killedEnemies = new System.Collections.Generic.HashSet<int>();
    public int KillCount { get { return killedEnemies.Count; } }
    public int KillTarget = 1;
    public void RegisterEnemyKill(int enemyId) { killedEnemies.Add(enemyId); }

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void OnHeroDeath()
    {
        if (deathPanel != null) return;
        if (StoryDirector.Instance != null) StoryDirector.Instance.CaptureCheckpoint();
        var canvas = RpgUI.CreateCanvas("DeathCanvas", transform, true);
        deathPanel = canvas.gameObject;
        var dim = RpgUI.CreateImage("Dim", canvas.transform, null, new Color(0f, 0f, 0f, 0.68f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.sizeDelta = Vector2.zero;
        var panel = RpgUI.CreateImage("Panel", canvas.transform, panelSprite, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 300f));
        RpgUI.CreateText("Title", panel.transform, "你已倒下", 42, new Color(1f, 0.9f, 0.72f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(500f, 70f));
        RpgUI.CreateButton("RestartButton", panel.transform, buttonSprite, "重新挑战", new Vector2(-130f, -55f), new Vector2(220f, 64f))
            .onClick.AddListener(RestartGame);
        RpgUI.CreateButton("MainMenuButton", panel.transform, buttonSprite, "返回主界面", new Vector2(130f, -55f), new Vector2(220f, 64f))
            .onClick.AddListener(ReturnToMain);
    }

    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void ReturnToMain() { Time.timeScale = 1f; StorySession.Reset(); SceneManager.LoadScene("界面"); }
}
