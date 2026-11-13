# RPG_Project Full UI HUD and Menus Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 用现有 Fantasy RPG UI 资源完成动作栏、操作提示、Esc 暂停菜单、界面一致性修正和开始界面。

**Architecture:** GameHUD 扩展动作栏与提示；PauseMenu 独占 Esc 和暂停状态；GameFlow 继续负责死亡与场景流程；InventoryUI 保持实时背包并提供 Close；开始界面通过编辑器脚本直接修改并保存。

**Tech Stack:** Tuanjie 2022.3.62t5、C#、uGUI、RpgUI、Fantasy RPG UI、Codely Bridge。

## Global Constraints

- 不新增弓箭手、武器切换、药水以外道具或其他玩法。
- 动作槽只表达现有右键攻击、Q 药水、I 背包；不出现未实现技能。
- `Button_05` 用于动作槽，`Menu_button_02_Little_On` 用于文字按钮，`Button_23` 用于齿轮，`Button_04` 只用于关闭。
- 暂停菜单打开 timeScale=0；继续/重开/返回/退出恢复 1；死亡面板不与其共存。
- 每阶段先失败后实现，保存场景并更新 progress.md；工程无 Git，不 commit。
- Bridge 使用 `node C:\Users\22492\.claude\tools\rpg-bridge.mjs`；真实键鼠仍由用户验收。

---

### Task 1: 动作栏与可消退操作提示

**Files:**
- Modify: `Assets/Script/GameHUD.cs`
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\FullHudTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_full-ui\`

**Interfaces:**
- Produces: `GameHUD.actionBar`、`controlsHint`、`potionButton`、`inventoryButton`、`inventoryUI`、`actionSlotSprite`、`weaponSprite`、`inventoryIconSprite`。
- Consumes: PotionInventory.TryUse、InventoryUI.Toggle、RpgUI。

- [x] **Step 1: 备份并写失败测试**

备份 Script 与 RPG游戏场景，前缀 `before-full-ui`。测试：

```csharp
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

var hud = UnityEngine.Object.FindObjectOfType<GameHUD>();
if (hud == null) throw new Exception("FAIL: GameHUD missing");
var type = typeof(GameHUD);
var actionBarField = type.GetField("actionBar");
var potionButtonField = type.GetField("potionButton");
var inventoryButtonField = type.GetField("inventoryButton");
var hintField = type.GetField("controlsHint");
if (actionBarField == null || potionButtonField == null || inventoryButtonField == null || hintField == null)
    throw new Exception("FAIL: full HUD action bar members missing");
var actionBar = (RectTransform)actionBarField.GetValue(hud);
var potionButton = (Button)potionButtonField.GetValue(hud);
var inventoryButton = (Button)inventoryButtonField.GetValue(hud);
var hint = (CanvasGroup)hintField.GetValue(hud);
var hero = UnityEngine.Object.FindObjectOfType<HeroController>();
var inventory = hero.GetComponent<PotionInventory>();
var inventoryUI = UnityEngine.Object.FindObjectOfType<InventoryUI>();
Func<RectTransform, Rect> screenRect = rect =>
{
    var corners = new Vector3[4];
    rect.GetWorldCorners(corners);
    return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
};
double stageStart = EditorApplication.timeSinceStartup;
int stage = 0;
EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    try
    {
        if (!EditorApplication.isPlaying) throw new Exception("FAIL: play stopped");
        double elapsed = EditorApplication.timeSinceStartup - stageStart;
        if (stage == 0 && elapsed > 0.5)
        {
            if (actionBar == null || actionBar.childCount != 3 || potionButton == null || inventoryButton == null || hint == null)
                throw new Exception("FAIL: action bar construction");
            var menuButton = GameObject.Find("Order").GetComponentInChildren<Button>();
            if (screenRect(actionBar).Overlaps(screenRect(hud.playerPanel))
                || screenRect(actionBar).Overlaps(screenRect(menuButton.GetComponent<RectTransform>())))
                throw new Exception("FAIL: action bar overlaps player HUD or menu button");
            inventory.Add(1);
            hero.Hurt(50f, null);
            potionButton.onClick.Invoke();
            if (hero.CurrentHP != 80f || inventory.Count != 0) throw new Exception("FAIL: potion action button");
            inventoryButton.onClick.Invoke();
            if (!inventoryUI.InventoryPanel.activeSelf) throw new Exception("FAIL: inventory action open");
            inventoryButton.onClick.Invoke();
            if (inventoryUI.InventoryPanel.activeSelf) throw new Exception("FAIL: inventory action close");
            type.GetField("hintAge", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, 8.1f);
            stage = 1; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 1 && elapsed > 0.2)
        {
            if (hint.gameObject.activeSelf) throw new Exception("FAIL: controls hint did not expire");
            ScreenCapture.CaptureScreenshot("C:/Users/22492/.claude/tools/full-hud.png");
            EditorApplication.update -= tick;
            Debug.Log("FULL HUD TESTS PASS");
        }
    }
    catch (Exception ex) { EditorApplication.update -= tick; Debug.LogException(ex); }
};
EditorApplication.update += tick;
```

Expected before implementation: FAIL `full HUD action bar members missing`.

- [x] **Step 2: 扩展 GameHUD 字段与接线**

添加字段：

```csharp
public InventoryUI inventoryUI;
public Sprite actionSlotSprite;
public Sprite weaponSprite;
public Sprite inventoryIconSprite;
public RectTransform actionBar;
public CanvasGroup controlsHint;
public Button potionButton;
public Button inventoryButton;
private float hintAge;
```

Awake 在 Build 前添加：

```csharp
if (inventoryUI == null) inventoryUI = FindObjectOfType<InventoryUI>();
```

- [x] **Step 3: 构建动作栏与提示**

在 GameHUD.Build 末尾加入：

```csharp
actionBar = RpgUI.CreateRect("ActionBar", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(284f, 84f));
string[] slotNames = { "AttackSlot", "PotionSlot", "InventorySlot" };
Sprite[] icons = { weaponSprite, potionSprite, inventoryIconSprite };
string[] keys = { "右键", "Q", "I" };
for (int i = 0; i < 3; i++)
{
    float x = (i - 1) * 100f;
    var slot = RpgUI.CreateImage(slotNames[i], actionBar, actionSlotSprite, Color.white,
        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(84f, 84f));
    RpgUI.CreateImage("Icon", slot.transform, icons[i], Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(52f, 52f));
    RpgUI.CreateText("Key", slot.transform, keys[i], 18, new Color(1f, 0.94f, 0.78f), TextAnchor.MiddleCenter,
        new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(76f, 22f));
    if (i == 1) potionButton = slot.gameObject.AddComponent<Button>();
    if (i == 2) inventoryButton = slot.gameObject.AddComponent<Button>();
}
potionButton.onClick.AddListener(() => { if (potionInventory != null) potionInventory.TryUse(); });
inventoryButton.onClick.AddListener(() => { if (inventoryUI != null) inventoryUI.Toggle(); });

var hintImage = RpgUI.CreateImage("ControlsHint", canvas.transform, panelSprite, new Color(1f, 1f, 1f, 0.88f),
    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 126f), new Vector2(650f, 42f));
controlsHint = hintImage.gameObject.AddComponent<CanvasGroup>();
controlsHint.blocksRaycasts = false;
RpgUI.CreateText("Text", hintImage.transform, "左键移动　右键攻击　滚轮缩放　中键旋转　Q 药水　I 背包　Esc 菜单", 20,
    Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 34f));
```

GameHUD.LateUpdate 开头加入：

```csharp
hintAge += Time.unscaledDeltaTime;
if (controlsHint != null)
{
    controlsHint.alpha = 1f - Mathf.Clamp01((hintAge - 6f) / 2f);
    if (hintAge >= 8f) controlsHint.gameObject.SetActive(false);
}
```

- [x] **Step 4: 接线保存并测试**

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scene = SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/RPG游戏.unity") throw new System.Exception("Wrong active scene");
var hud = Object.FindObjectOfType<GameHUD>();
var systems = GameObject.Find("GameSystems");
if (hud == null || systems == null) throw new System.Exception("HUD/GameSystems missing");
hud.inventoryUI = systems.GetComponent<InventoryUI>();
hud.actionSlotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Parts/Button_05.png");
hud.weaponSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Parts/InventoryButton_weapon.png");
hud.inventoryIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Parts/Button_11.png");
if (hud.inventoryUI == null || hud.actionSlotSprite == null || hud.weaponSprite == null || hud.inventoryIconSprite == null)
    throw new System.Exception("Full HUD binding failed");
EditorUtility.SetDirty(hud);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("FULL HUD SCENE SAVED");
```

Play 运行 FullHudTests。Expected: `FULL HUD TESTS PASS`；截图确认三个槽、文本与提示布局。

---

### Task 2: Esc 暂停菜单与界面一致性

**Files:**
- Create: `Assets/Script/PauseMenu.cs`
- Modify: `Assets/Script/HeroCamera.cs`（删除 Esc 旧逻辑）
- Modify: `Assets/Script/GameFlow.cs`（公开流程方法、IsDeathPanelShown）
- Modify: `Assets/Script/InventoryUI.cs`（Close 与关闭按钮）
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\PauseMenuTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_full-ui\RPG游戏.before-pause.unity`

**Interfaces:**
- Produces: `PauseMenu.Toggle()`、`HandleEscape()`、`IsOpen`、`PausePanel`、`ContinueButton`、`RestartButton`、`MainMenuButton`、`QuitButton`；`InventoryUI.Close()`。
- Consumes: GameFlow.RestartGame/ReturnToMain/IsDeathPanelShown、RpgUI。

- [x] **Step 1: 写失败测试**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

Type type = Type.GetType("PauseMenu, Assembly-CSharp");
if (type == null) throw new Exception("FAIL: PauseMenu type missing");
var pause = (Component)UnityEngine.Object.FindObjectOfType(type);
var toggle = type.GetMethod("Toggle");
var escape = type.GetMethod("HandleEscape");
var isOpen = type.GetProperty("IsOpen");
var panel = (GameObject)type.GetField("PausePanel").GetValue(pause);
var inventoryUI = UnityEngine.Object.FindObjectOfType<InventoryUI>();
double stageStart = EditorApplication.timeSinceStartup;
int stage = 0;
EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    try
    {
        if (!EditorApplication.isPlaying) throw new Exception("FAIL: play stopped");
        double elapsed = EditorApplication.timeSinceStartup - stageStart;
        if (stage == 0 && elapsed > 0.3)
        {
            toggle.Invoke(pause, null);
            if (!(bool)isOpen.GetValue(pause) || Time.timeScale != 0f) throw new Exception("FAIL: pause open");
            ScreenCapture.CaptureScreenshot("C:/Users/22492/.claude/tools/pause-open.png");
            stage = 1; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 1 && elapsed > 0.3)
        {
            ((Button)type.GetField("ContinueButton").GetValue(pause)).onClick.Invoke();
            if ((bool)isOpen.GetValue(pause) || Time.timeScale != 1f) throw new Exception("FAIL: continue");
            inventoryUI.Toggle();
            escape.Invoke(pause, null);
            if (inventoryUI.InventoryPanel.activeSelf || (bool)isOpen.GetValue(pause)) throw new Exception("FAIL: Esc did not close inventory first");
            escape.Invoke(pause, null);
            if (!(bool)isOpen.GetValue(pause) || Time.timeScale != 0f) throw new Exception("FAIL: Esc did not open pause");
            ((Button)type.GetField("ContinueButton").GetValue(pause)).onClick.Invoke();
            var hero = UnityEngine.Object.FindObjectOfType<HeroController>();
            hero.Hurt(1000f, null);
            escape.Invoke(pause, null);
            if ((bool)isOpen.GetValue(pause) || Time.timeScale != 1f) throw new Exception("FAIL: pause opened over death panel");
            EditorApplication.update -= tick;
            Debug.Log("PAUSE MENU TESTS PASS");
        }
    }
    catch (Exception ex) { EditorApplication.update -= tick; Debug.LogException(ex); }
};
EditorApplication.update += tick;
```

Expected before implementation: FAIL `PauseMenu type missing`.

- [x] **Step 2: 实现 PauseMenu**

```csharp
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
        RpgUI.CreateText("Controls", panel.transform, "左键移动 / 右键攻击\n滚轮缩放 / 中键旋转\nQ 药水 / I 背包 / Esc 继续", 21, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(560f, 100f));
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
```

- [x] **Step 3: 修改既有 Esc/死亡/背包行为**

删除 HeroCamera.LateUpdate 末尾整个 `Input.GetKeyDown(KeyCode.Escape)` 块。

GameFlow：
```csharp
public bool IsDeathPanelShown { get { return deathPanel != null; } }
public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
public void ReturnToMain() { Time.timeScale = 1f; SceneManager.LoadScene("界面"); }
```

InventoryUI 添加字段 `public Sprite closeSprite;`，方法：

```csharp
public void Close() { InventoryPanel.SetActive(false); }
```

Build 在隐藏面板前创建关闭按钮：

```csharp
var closeImage = RpgUI.CreateImage("CloseButton", panel.transform, closeSprite, Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(48f, 48f));
closeImage.gameObject.AddComponent<Button>().onClick.AddListener(Close);
```

- [x] **Step 4: 场景接线与保存**

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scene = SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/RPG游戏.unity") throw new System.Exception("Wrong active scene");
var systems = GameObject.Find("GameSystems");
var pause = systems.GetComponent<PauseMenu>();
if (pause == null) pause = systems.AddComponent<PauseMenu>();
pause.inventoryUI = systems.GetComponent<InventoryUI>();
pause.panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Parts/Background_01.png");
pause.buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Update 1.2/Menu_button_02_Little_On.png");
pause.inventoryUI.closeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Parts/Button_04.png");
var flow = systems.GetComponent<GameFlow>();
flow.buttonSprite = pause.buttonSprite;
var menuButton = GameObject.Find("Order").GetComponentInChildren<Button>();
menuButton.image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Parts/Button_23.png");
var menuText = menuButton.GetComponentInChildren<Text>();
if (menuText != null) menuText.text = "";
var rect = menuButton.GetComponent<RectTransform>();
rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
rect.anchoredPosition = new Vector2(-20f, -20f);
rect.sizeDelta = new Vector2(60f, 60f);
rect.localScale = Vector3.one;
while (menuButton.onClick.GetPersistentEventCount() > 0)
    UnityEventTools.RemovePersistentListener(menuButton.onClick, 0);
UnityEventTools.AddPersistentListener(menuButton.onClick, pause.Toggle);
EditorUtility.SetDirty(pause);
EditorUtility.SetDirty(pause.inventoryUI);
EditorUtility.SetDirty(flow);
EditorUtility.SetDirty(menuButton);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("PAUSE MENU SCENE SAVED");
```

运行 PauseMenuTests。Expected: `PAUSE MENU TESTS PASS`；暂停截图在关闭前单独生成。

---

### Task 3: 开始界面

**Files:**
- Modify: `Assets/Script/CLick.cs`（增加 ExitGame）
- Modify: `Assets/Scenes/界面.scene`
- Test: `C:\Users\22492\.claude\tools\StartMenuTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_full-ui\界面.before.unity`

- [x] **Step 1: 写失败测试**

在编辑器打开界面.scene 后进入 Play：

```csharp
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;

var title = GameObject.Find("StartTitle");
var exit = GameObject.Find("StartExitButton");
if (title == null || exit == null) throw new Exception("FAIL: start menu title/exit missing");
if (title.GetComponent<Text>().text != "RPG 冒险") throw new Exception("FAIL: start title text");
ScreenCapture.CaptureScreenshot("C:/Users/22492/.claude/tools/start-menu.png");
double start = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (EditorApplication.timeSinceStartup - start < 0.3) return;
    EditorApplication.update -= tick;
    var buttons = UnityEngine.Object.FindObjectsOfType<Button>();
    Button startButton = null;
    foreach (var button in buttons) if (button.GetComponentInChildren<Text>() != null && button.GetComponentInChildren<Text>().text == "开始游戏") startButton = button;
    if (startButton == null) throw new Exception("FAIL: start button missing");
    startButton.onClick.Invoke();
    EditorApplication.CallbackFunction verify = null;
    double loaded = EditorApplication.timeSinceStartup;
    verify = () =>
    {
        if (EditorApplication.timeSinceStartup - loaded < 1.5) return;
        EditorApplication.update -= verify;
        if (SceneManager.GetActiveScene().name != "RPG游戏") throw new Exception("FAIL: start scene load");
        Debug.Log("START MENU TESTS PASS");
    };
    EditorApplication.update += verify;
};
EditorApplication.update += tick;
```

Expected before implementation: FAIL `start menu title/exit missing`.

- [x] **Step 2: 增加 CLick.ExitGame**

```csharp
public void ExitGame()
{
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
}
```

- [x] **Step 3: 修改并保存开始场景**

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

var scene = EditorSceneManager.OpenScene("Assets/Scenes/界面.scene", OpenSceneMode.Single);
var canvas = Object.FindObjectOfType<Canvas>();
var click = Object.FindObjectOfType<CLick>();
if (canvas == null || click == null) throw new System.Exception("Start Canvas/CLick missing");
var scaler = canvas.GetComponent<CanvasScaler>();
scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new Vector2(1920f, 1080f);
scaler.matchWidthOrHeight = 0.5f;
var wideButton = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Update 1.2/Menu_button_02_Little_On.png");
var startButton = Object.FindObjectOfType<Button>();
startButton.image.sprite = wideButton;
var startRect = startButton.GetComponent<RectTransform>();
startRect.anchorMin = startRect.anchorMax = startRect.pivot = new Vector2(0.5f, 0.5f);
startRect.anchoredPosition = new Vector2(0f, -80f);
startRect.sizeDelta = new Vector2(320f, 88f);
startRect.localScale = Vector3.one;
var startText = startButton.GetComponentInChildren<Text>();
startText.text = "开始游戏";
startText.fontSize = 30;
startText.color = new Color(1f, 0.94f, 0.78f);
startText.alignment = TextAnchor.MiddleCenter;
RpgUI.CreateText("StartTitle", canvas.transform, "RPG 冒险", 64, new Color(1f, 0.9f, 0.65f), TextAnchor.MiddleCenter,
    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(800f, 100f));
RpgUI.CreateText("StartSubtitle", canvas.transform, "探索村庄，击败巡逻敌人", 26, Color.white, TextAnchor.MiddleCenter,
    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(800f, 44f));
var exitButton = RpgUI.CreateButton("StartExitButton", canvas.transform, wideButton, "退出游戏", new Vector2(0f, -190f), new Vector2(320f, 88f));
UnityEventTools.AddPersistentListener(exitButton.onClick, click.ExitGame);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("START MENU SCENE SAVED");
```

预期 StartTitle/StartExitButton 创建成功，开始按钮原 CLick.ButtonClick 监听保持不变。

- [x] **Step 4: 测试与截图**

Play 运行 StartMenuTests。Expected: `START MENU TESTS PASS`，开始按钮进入 RPG游戏，截图标题和按钮无重叠。退出按钮的监听器须指向 CLick.ExitGame；最后单独调用退出并确认编辑器停止 Play。

---

### Task 4: 双分辨率与全量回归

**Files:**
- Update: `docs/progress.md`
- Create: `docs/superpowers/reports/2026-11-09-full-ui-verification.md`
- Create: `docs/verification/2026-11-09-full-ui/`

- [x] **Step 1: 双分辨率验证**

默认 Game View 下运行 FullHudTests、PauseMenuTests、InventoryUITests、ObjectiveHudTests。再执行：

`manage_gameview {"action":"set_resolution","width":1920,"height":1080}`

重复 FullHudTests 和 InventoryUITests，并对 actionBar/playerPanel/objectivePanel/菜单按钮、InventoryPanel 的屏幕 Rect 做不相交检查。截图分别保存。若断言失败，修布局而非删除断言。

- [x] **Step 2: 全量功能回归**

干净 Play 依次验证 PotionRuntimeTests、FeedbackRuntimeTests、HudRuntimeTests、PlayProbe5、PlayProbe6、DeathFlowTests、SceneSwitchProbe；编辑态 CameraControlTests。重开/返回/继续后 Time.timeScale 必须为 1，HeroCamera 源码不得再含 Escape 直接退出。

- [x] **Step 3: 持久化与归档**

停止 Play、从磁盘重载两个场景，核对 GameHUD/PauseMenu/InventoryUI/GameFlow 资源引用、Order 菜单按钮和开始界面元素。归档 full-hud.png、pause-open.png、start-menu.png 与 1920 截图；写验收报告、更新 progress.md、记录真实键鼠限制与既有控制台告警。无 Git，不提交。
