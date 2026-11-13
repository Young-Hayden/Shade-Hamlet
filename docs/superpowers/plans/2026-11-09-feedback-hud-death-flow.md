# RPG_Project Combat Feedback HUD and Death Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 用现有美术资源补齐伤害反馈、基础 HUD 与主角死亡后的重新挑战/返回主界面闭环。

**Architecture:** `DamagePopup` 只负责弹字生命周期，`CombatFeedback` 统一响应伤害和粒子，`GameHUD` 只显示战斗状态，`GameFlow` 只负责死亡结算。`RpgUI` 提供共享 uGUI 构建方法；场景中的 GameSystems 对象只持有服务组件和显式资源引用。

**Tech Stack:** Tuanjie 2022.3.62t5、C#、uGUI、SimpleFX、Fantasy RPG UI、Codely Bridge。

## Global Constraints

- 不改 ATK、HP 上限、攻击距离、攻击间隔、寻路、动画事件或仇恨规则。
- 不实现药水、背包、弓箭手、武器切换、完整 Esc 菜单或对象池。
- 新 HUD 为 Screen Space Overlay，Scale With Screen Size，1920×1080；头顶血条保留。
- 死亡面板不修改 Time.timeScale；重复死亡只创建一个 DeathCanvas。
- 不新增 Input System；按钮通过 `Button.onClick.Invoke()` 验证，真人点击另行验收。
- 工程无 Git：不 commit。每阶段备份并保存场景，更新 `docs/progress.md`。
- Bridge：`node C:\Users\22492\.claude\tools\rpg-bridge.mjs <command> '<json>'`。

---

### Task 1: 伤害数字与命中/击杀粒子

**Files:**
- Create: `Assets/Script/RpgUI.cs`
- Create: `Assets/Script/DamagePopup.cs`
- Create: `Assets/Script/CombatFeedback.cs`
- Modify: `Assets/Script/HeroController.cs`（MaxHP、Hurt、Die 接线）
- Modify: `Assets/Script/EnemyController.cs`（MaxHP、Hurt、Die 接线）
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\FeedbackRuntimeTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_feedback\`

**Interfaces:**
- Produces: `CombatFeedback.Instance`、`ShowDamage(Vector3,float,bool)`、`PlayHit(Vector3)`、`PlayKill(Vector3)`；`DamagePopup.Create(Vector3,float,bool)`；`HeroController.MaxHP`、`EnemyController.MaxHP`。
- Consumes: `RpgUI.CreateCanvas/CreateText`，以及现有 `Hurt`/`Die`。

- [x] **Step 1: 备份并写失败测试**

备份 `Assets/Script/*.cs` 和场景到上述目录，文件加 `before-feedback` 前缀。测试先检查类型存在，再通过反射执行行为：

```csharp
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

Type popupType = Type.GetType("DamagePopup, Assembly-CSharp");
Type feedbackType = Type.GetType("CombatFeedback, Assembly-CSharp");
if (popupType == null || feedbackType == null)
    throw new Exception("FAIL: DamagePopup/CombatFeedback types are missing");
var hero = UnityEngine.Object.FindObjectOfType<HeroController>();
var feedback = UnityEngine.Object.FindObjectOfType(feedbackType);
if (hero == null || feedback == null) throw new Exception("FAIL: scene feedback service missing");
var instance = feedbackType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
var showDamage = feedbackType.GetMethod("ShowDamage");
var playHit = feedbackType.GetMethod("PlayHit");
var log = new System.Text.StringBuilder();
double started = EditorApplication.timeSinceStartup;
int stage = 0;
double stageStart = started;
EditorApplication.CallbackFunction tick = null;
Func<int> popupCount = () => UnityEngine.Object.FindObjectsOfType(popupType).Length;
Func<int> hitCount = () => Array.FindAll(UnityEngine.Object.FindObjectsOfType<ParticleSystem>(), p => p.name.StartsWith("FX_BloodSplatter")).Length;
tick = () =>
{
    if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; throw new Exception("FAIL: play stopped"); }
    if (EditorApplication.timeSinceStartup - stageStart < 0.3) return;
    if (stage == 0)
    {
        int before = popupCount();
        showDamage.Invoke(feedback, new object[] { hero.transform.position, 0f, true });
        if (popupCount() != before) throw new Exception("FAIL: zero damage spawned popup");
        showDamage.Invoke(feedback, new object[] { hero.transform.position + Vector3.up * 2f, 20f, true });
        var popups = UnityEngine.Object.FindObjectsOfType(popupType);
        if (popups.Length != before + 1) throw new Exception("FAIL: damage popup count");
        var text = ((Component)popups[0]).GetComponentInChildren<Text>();
        if (text == null || text.text != "20") throw new Exception("FAIL: popup text is not 20");
        log.AppendLine("popup text/count PASS");
        stage = 1; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 1 && EditorApplication.timeSinceStartup - stageStart > 1.0)
    {
        if (popupCount() != 0) throw new Exception("FAIL: popup did not clean up");
        int before = hitCount();
        playHit.Invoke(feedback, new object[] { hero.transform.position });
        if (hitCount() != before + 1) throw new Exception("FAIL: hit effect missing");
        log.AppendLine("popup cleanup/hit spawn PASS");
        stage = 2; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 2 && EditorApplication.timeSinceStartup - stageStart > 2.1)
    {
        if (hitCount() != 0) throw new Exception("FAIL: hit effect did not clean up");
        float hp = hero.CurrentHP;
        hero.Hurt(0f, null);
        if (popupCount() != 0 || hero.CurrentHP != hp) throw new Exception("FAIL: zero Hurt produced feedback");
        hero.Hurt(20f, null);
        if (hero.CurrentHP != hp - 20f || popupCount() != 1) throw new Exception("FAIL: Hurt feedback mismatch");
        UnityEngine.Object.Destroy(feedback);
        hero.Hurt(20f, null);
        if (hero.CurrentHP != hp - 40f) throw new Exception("FAIL: damage failed without feedback service");
        log.AppendLine("effect cleanup/Hurt/degraded service PASS");
        EditorApplication.update -= tick;
        Debug.Log("FEEDBACK RUNTIME TESTS PASS\n" + log);
    }
};
EditorApplication.update += tick;
```

Expected before implementation: FAIL `DamagePopup/CombatFeedback types are missing`.

- [x] **Step 2: 创建共享 UI 构建器**

```csharp
using UnityEngine;
using UnityEngine.UI;

public static class RpgUI
{
    private static Font defaultFont;
    public static Font DefaultFont => defaultFont != null ? defaultFont : defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static Canvas CreateCanvas(string name, Transform parent, bool interactive)
    {
        var rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var canvas = rect.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = rect.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        if (interactive) rect.gameObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var image = CreateRect(name, parent, anchor, pivot, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    public static Text CreateText(string name, Transform parent, string value, int fontSize, Color color, TextAnchor alignment, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var text = CreateRect(name, parent, anchor, pivot, position, size).gameObject.AddComponent<Text>();
        text.font = DefaultFont;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    public static Slider CreateSlider(string name, Transform parent, Sprite sprite, Color fillColor, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var rect = CreateRect(name, parent, anchor, pivot, position, size);
        var slider = rect.gameObject.AddComponent<Slider>();
        var background = CreateImage("Background", rect, sprite, new Color(0.08f, 0.08f, 0.08f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.sizeDelta = Vector2.zero;
        var fill = CreateImage("Fill", rect, sprite, fillColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.sizeDelta = Vector2.zero;
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = background;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        return slider;
    }

    public static Button CreateButton(string name, Transform parent, Sprite sprite, string label, Vector2 position, Vector2 size)
    {
        var image = CreateImage(name, parent, sprite, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        var button = image.gameObject.AddComponent<Button>();
        CreateText("Label", image.transform, label, 26, new Color(1f, 0.94f, 0.78f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return button;
    }
}
```

- [x] **Step 3: 创建伤害弹字与反馈服务**

```csharp
using UnityEngine;
using UnityEngine.UI;

public class DamagePopup : MonoBehaviour
{
    private const float Lifetime = 0.85f;
    private CanvasGroup group;
    private float age;

    public static DamagePopup Create(Vector3 position, float damage, bool playerWasHit)
    {
        var canvas = RpgUI.CreateCanvas("DamagePopup", null, false);
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180f, 100f);
        rect.position = position;
        rect.localScale = Vector3.one * 0.005f;
        var popup = canvas.gameObject.AddComponent<DamagePopup>();
        popup.group = canvas.gameObject.AddComponent<CanvasGroup>();
        RpgUI.CreateText("Value", rect, Mathf.RoundToInt(damage).ToString(), 90,
            playerWasHit ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.56f, 0.29f),
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, rect.sizeDelta);
        return popup;
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.position += Vector3.up * (0.65f * Time.deltaTime);
        if (group != null) group.alpha = 1f - Mathf.Clamp01(age / Lifetime);
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        if (age >= Lifetime) Destroy(gameObject);
    }
}
```

```csharp
using UnityEngine;

public class CombatFeedback : MonoBehaviour
{
    public static CombatFeedback Instance { get; private set; }
    public GameObject hitEffect;
    public GameObject killEffect;

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void ShowDamage(Vector3 position, float damage, bool playerWasHit)
    {
        if (damage <= 0f) return;
        DamagePopup.Create(position, damage, playerWasHit);
    }

    public void PlayHit(Vector3 position) { PlayEffect(hitEffect, position + Vector3.up, 2f); }
    public void PlayKill(Vector3 position) { PlayEffect(killEffect, position + Vector3.up, 2.5f); }

    private void PlayEffect(GameObject prefab, Vector3 position, float lifetime)
    {
        if (prefab == null) return;
        Destroy(Instantiate(prefab, position, Quaternion.identity), lifetime);
    }
}
```

- [x] **Step 4: 接线实际伤害与死亡**

在两个控制器各加 `public float MaxHP { get { return HP_Max; } }`。

`HeroController.Hurt` 在 Clamp 前保存 `float oldHP = HP_Now;`，Clamp 后计算 `float actualDamage = oldHP - HP_Now;`，并仅在实际伤害大于 0 且服务存在时调用：

```csharp
if (actualDamage > 0f && CombatFeedback.Instance != null)
{
    CombatFeedback.Instance.ShowDamage(transform.position + Vector3.up * 2.1f, actualDamage, true);
    CombatFeedback.Instance.PlayHit(transform.position);
}
```

`HeroController.Die` 末尾（Destroy 前）调用：

```csharp
if (GameFlow.Instance != null) GameFlow.Instance.OnHeroDeath();
```

此调用要等 Task 3 创建 GameFlow 后再编译；Task 1 暂不加入该行。

`EnemyController.Hurt` 使用相同计算，但 `playerWasHit=false`。`EnemyController.Die` 在 Destroy 前调用：

```csharp
if (CombatFeedback.Instance != null) CombatFeedback.Instance.PlayKill(transform.position);
```

- [x] **Step 5: 创建场景服务并保存**

执行以下编辑器脚本：

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scene = SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/RPG游戏.unity") throw new System.Exception("Wrong active scene");
var systems = GameObject.Find("GameSystems");
if (systems == null) systems = new GameObject("GameSystems");
var feedback = systems.GetComponent<CombatFeedback>();
if (feedback == null) feedback = systems.AddComponent<CombatFeedback>();
feedback.hitEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MainAsset/MainAsset/Asset Packs/SimpleFX/Prefabs/FX_BloodSplatter.prefab");
feedback.killEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MainAsset/MainAsset/Asset Packs/SimpleFX/Prefabs/FX_Fireworks_Yellow_Small.prefab");
if (feedback.hitEffect == null || feedback.killEffect == null) throw new System.Exception("Feedback asset binding failed");
EditorUtility.SetDirty(feedback);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("FEEDBACK SCENE SETUP SAVED");
```

更新 progress.md，标记伤害反馈阶段已保存。

- [x] **Step 6: 编译、Play 测试**

先 request_compile，再进入 Play 并运行 Step 1 测试。Expected: `FEEDBACK RUNTIME TESTS PASS`，粒子与弹字均自动清理，服务销毁后伤害仍正确。失败则停止后续任务并修复。

---

### Task 2: 基础 HUD 与退出按钮移位

**Files:**
- Create: `Assets/Script/GameHUD.cs`
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\HudRuntimeTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_feedback\RPG游戏.before-hud.unity`

**Interfaces:**
- Consumes: `HeroController.MaxHP/CurrentHP/tarEnemy`、`EnemyController.MaxHP/CurrentHP/isDeath`、RpgUI。
- Produces: `GameHUD.heroHealth`、`heroHealthText`、`targetPanel`、`playerPanel`。

- [x] **Step 1: 写失败测试**

```csharp
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

Type hudType = Type.GetType("GameHUD, Assembly-CSharp");
if (hudType == null) throw new Exception("FAIL: GameHUD type is missing");
var hud = UnityEngine.Object.FindObjectOfType(hudType);
if (hud == null) throw new Exception("FAIL: GameHUD scene component missing");
FieldInfo Field(string name) => hudType.GetField(name);
var heroHealth = (Slider)Field("heroHealth").GetValue(hud);
var heroText = (Text)Field("heroHealthText").GetValue(hud);
var targetPanel = (GameObject)Field("targetPanel").GetValue(hud);
var playerPanel = (RectTransform)Field("playerPanel").GetValue(hud);
var hero = UnityEngine.Object.FindObjectOfType<HeroController>();
var enemy = UnityEngine.Object.FindObjectOfType<EnemyController>();
if (heroHealth == null || heroText == null || targetPanel == null || playerPanel == null || hero == null || enemy == null)
    throw new Exception("FAIL: HUD references incomplete");
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
    if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; throw new Exception("FAIL: play stopped"); }
    double elapsed = EditorApplication.timeSinceStartup - stageStart;
    if (stage == 0 && elapsed > 0.5)
    {
        if (Math.Abs(heroHealth.value - 1f) > 0.001f || heroText.text != "100 / 100") throw new Exception("FAIL: initial hero HUD");
        if (targetPanel.activeSelf) throw new Exception("FAIL: target panel visible without target");
        hero.Hurt(20f, null);
        hero.tarEnemy = enemy.gameObject;
        stage = 1; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 1 && elapsed > 0.3)
    {
        if (Math.Abs(heroHealth.value - 0.8f) > 0.001f || heroText.text != "80 / 100") throw new Exception("FAIL: damaged hero HUD");
        if (!targetPanel.activeSelf) throw new Exception("FAIL: target panel did not show");
        enemy.Hurt(1000f, hero.transform);
        stage = 2; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 2 && elapsed > 0.3)
    {
        if (targetPanel.activeSelf) throw new Exception("FAIL: dead target panel still visible");
        var canvas = ((Component)hud).GetComponentInChildren<Canvas>();
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize || scaler.referenceResolution != new Vector2(1920f, 1080f))
            throw new Exception("FAIL: HUD canvas scaling");
        var exitButton = GameObject.Find("Order").GetComponentInChildren<Button>();
        if (screenRect(playerPanel).Overlaps(screenRect(exitButton.GetComponent<RectTransform>())))
            throw new Exception("FAIL: player HUD overlaps exit button");
        ScreenCapture.CaptureScreenshot("C:/Users/22492/.claude/tools/hud-after.png");
        EditorApplication.update -= tick;
        Debug.Log("HUD RUNTIME TESTS PASS");
    }
};
EditorApplication.update += tick;
```

Expected before implementation: FAIL `GameHUD type is missing`.

- [x] **Step 2: 实现 GameHUD**

```csharp
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public HeroController hero;
    public Sprite panelSprite;
    public Sprite barSprite;
    public Slider heroHealth;
    public Text heroHealthText;
    public GameObject targetPanel;
    public RectTransform playerPanel;
    private Text targetName;
    private Slider targetHealth;

    void Awake()
    {
        if (hero == null) hero = FindObjectOfType<HeroController>();
        Build();
    }

    private void Build()
    {
        var canvas = RpgUI.CreateCanvas("GameHUDCanvas", transform, false);
        playerPanel = RpgUI.CreateImage("PlayerPanel", canvas.transform, panelSprite, new Color(1f, 1f, 1f, 0.92f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(360f, 92f)).rectTransform;
        RpgUI.CreateText("Title", playerPanel, "生命", 24, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -8f), new Vector2(320f, 28f));
        heroHealth = RpgUI.CreateSlider("HeroHealth", playerPanel, barSprite, new Color(0.25f, 0.9f, 0.3f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -40f), new Vector2(324f, 22f));
        heroHealthText = RpgUI.CreateText("Value", playerPanel, "100 / 100", 20, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -64f), new Vector2(324f, 24f));

        var targetImage = RpgUI.CreateImage("TargetPanel", canvas.transform, panelSprite, new Color(1f, 1f, 1f, 0.92f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(360f, 88f));
        targetPanel = targetImage.gameObject;
        targetName = RpgUI.CreateText("Name", targetPanel.transform, "敌人", 24, new Color(1f, 0.9f, 0.65f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(340f, 30f));
        targetHealth = RpgUI.CreateSlider("TargetHealth", targetPanel.transform, barSprite, new Color(0.9f, 0.22f, 0.18f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -48f), new Vector2(324f, 22f));
        targetPanel.SetActive(false);
    }

    void LateUpdate()
    {
        if (hero == null) hero = FindObjectOfType<HeroController>();
        float heroRatio = hero != null && hero.MaxHP > 0f ? hero.CurrentHP / hero.MaxHP : 0f;
        heroHealth.value = Mathf.Clamp01(heroRatio);
        heroHealthText.text = hero != null ? Mathf.CeilToInt(hero.CurrentHP) + " / " + Mathf.RoundToInt(hero.MaxHP) : "0 / 100";

        EnemyController enemy = null;
        if (hero != null && hero.tarEnemy != null) enemy = hero.tarEnemy.GetComponent<EnemyController>();
        bool show = enemy != null && !enemy.isDeath;
        targetPanel.SetActive(show);
        if (!show) return;
        targetName.text = enemy.name;
        targetHealth.value = enemy.MaxHP > 0f ? Mathf.Clamp01(enemy.CurrentHP / enemy.MaxHP) : 0f;
    }
}
```

- [x] **Step 3: 接线并保存**

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scene = SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/RPG游戏.unity") throw new System.Exception("Wrong active scene");
var systems = GameObject.Find("GameSystems");
if (systems == null) throw new System.Exception("GameSystems missing");
var hud = systems.GetComponent<GameHUD>();
if (hud == null) hud = systems.AddComponent<GameHUD>();
hud.hero = UnityEngine.Object.FindObjectOfType<HeroController>();
hud.panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Parts/Background_02.png");
hud.barSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Update 1.1/ScrollingBar_Long_size.png");
if (hud.hero == null || hud.panelSprite == null || hud.barSprite == null) throw new System.Exception("HUD binding failed");
var order = GameObject.Find("Order");
var exitButton = order != null ? order.GetComponentInChildren<Button>() : null;
if (exitButton == null) throw new System.Exception("Exit button missing");
var rect = exitButton.GetComponent<RectTransform>();
rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
rect.anchoredPosition = new Vector2(-20f, -20f);
EditorUtility.SetDirty(hud);
EditorUtility.SetDirty(rect);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("HUD SCENE SETUP SAVED");
```

更新 progress.md，标记 HUD 阶段已保存。

- [x] **Step 4: 运行 HUD 测试与截图**

Expected: 所有数值/显示断言通过，playerPanel 与退出按钮不重叠，截图文字可读。若中文缺字或布局重叠，停止并修正，不进入 Task 3。

---

### Task 3: 死亡重开面板

**Files:**
- Create: `Assets/Script/GameFlow.cs`
- Modify: `Assets/Script/HeroController.cs`（Die 添加 GameFlow 调用）
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\DeathFlowTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_feedback\RPG游戏.before-death-flow.unity`

**Interfaces:**
- Consumes: RpgUI、`HeroController.Die`。
- Produces: `GameFlow.Instance`、`OnHeroDeath()`；运行时对象 `DeathCanvas`、`RestartButton`、`MainMenuButton`。

- [x] **Step 1: 写失败测试**

```csharp
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;

if (Type.GetType("GameFlow, Assembly-CSharp") == null) throw new Exception("FAIL: GameFlow type is missing");
var hero = UnityEngine.Object.FindObjectOfType<HeroController>();
if (hero == null) throw new Exception("FAIL: Hero missing");
double stageStart = EditorApplication.timeSinceStartup;
int stage = 0;
EditorApplication.CallbackFunction tick = null;
Func<int> deathCanvasCount = () => UnityEngine.Object.FindObjectsOfType<Canvas>().Count(c => c.name == "DeathCanvas");
tick = () =>
{
    if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; throw new Exception("FAIL: play stopped"); }
    double elapsed = EditorApplication.timeSinceStartup - stageStart;
    if (stage == 0)
    {
        hero.Hurt(1000f, null);
        hero.Hurt(1000f, null);
        stage = 1; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 1 && elapsed > 0.3)
    {
        if (deathCanvasCount() != 1) throw new Exception("FAIL: expected exactly one DeathCanvas");
        if (Time.timeScale != 1f) throw new Exception("FAIL: death flow changed timeScale");
        ScreenCapture.CaptureScreenshot("C:/Users/22492/.claude/tools/death-panel.png");
        GameObject.Find("RestartButton").GetComponent<Button>().onClick.Invoke();
        stage = 2; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 2 && elapsed > 1.5)
    {
        var newHero = UnityEngine.Object.FindObjectOfType<HeroController>();
        var newEnemy = UnityEngine.Object.FindObjectOfType<EnemyController>();
        if (SceneManager.GetActiveScene().name != "RPG游戏" || newHero == null || newEnemy == null)
            throw new Exception("FAIL: restart scene or actors");
        if (newHero.CurrentHP != 100f || newEnemy.CurrentHP != 100f || deathCanvasCount() != 0)
            throw new Exception("FAIL: restart state did not reset");
        newHero.Hurt(1000f, null);
        stage = 3; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 3 && elapsed > 0.3)
    {
        GameObject.Find("MainMenuButton").GetComponent<Button>().onClick.Invoke();
        stage = 4; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 4 && elapsed > 1.5)
    {
        if (SceneManager.GetActiveScene().name != "界面") throw new Exception("FAIL: main menu scene");
        EditorApplication.update -= tick;
        Debug.Log("DEATH FLOW TESTS PASS");
    }
};
EditorApplication.update += tick;
```

Expected before implementation: FAIL `GameFlow type is missing`.

- [x] **Step 2: 实现 GameFlow**

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }
    public Sprite panelSprite;
    public Sprite buttonSprite;
    private GameObject deathPanel;

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void OnHeroDeath()
    {
        if (deathPanel != null) return;
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

    private void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    private void ReturnToMain() { SceneManager.LoadScene("界面"); }
}
```

- [x] **Step 3: 接线与保存**

在 `HeroController.Die` 的 Destroy 前加入：

```csharp
if (GameFlow.Instance != null) GameFlow.Instance.OnHeroDeath();
```

执行场景设置：

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scene = SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/RPG游戏.unity") throw new System.Exception("Wrong active scene");
var systems = GameObject.Find("GameSystems");
if (systems == null) throw new System.Exception("GameSystems missing");
var flow = systems.GetComponent<GameFlow>();
if (flow == null) flow = systems.AddComponent<GameFlow>();
flow.panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Parts/Background_01.png");
flow.buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Parts/Button_04.png");
if (flow.panelSprite == null || flow.buttonSprite == null) throw new System.Exception("Death panel binding failed");
EditorUtility.SetDirty(flow);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("DEATH FLOW SCENE SETUP SAVED");
```

更新 progress.md，标记死亡重开阶段已保存。

- [x] **Step 4: 编译并验证死亡流程**

Expected: 单次面板、重开状态复位、返回主界面全部通过。截取死亡面板 `death-panel.png`。测试失败则停止，不宣称完成。

---

### Task 4: 全量回归与阶段归档

**Files:**
- Update: `docs/progress.md`
- Create: `docs/superpowers/reports/2026-11-09-feedback-hud-verification.md`
- Create: `docs/verification/2026-11-09/` 截图归档

- [x] **Step 1: 最终回归**

重新进入 RPG游戏 Play，依次运行 CameraControlTests、FeedbackRuntimeTests、HudRuntimeTests、PlayProbe5、PlayProbe6、SceneSwitchProbe。每项必须读取实际输出；同一 Play 中注意前一测试造成的伤害/销毁，必要时重启 Play，不能复用已失效对象。

Expected: 相机 4/12 米和旋转通过；反馈清理通过；HUD 显示/隐藏通过；敌人 100→0、主角死亡只一次、场景往返成功。

- [x] **Step 2: 检查控制台与场景持久化**

记录所有 Console Error，区分既有树 shader/烘焙后端/Unknown Missing Script 与新增异常。停止 Play，从磁盘重载 RPG游戏，确认 GameSystems 的 CombatFeedback/GameHUD/GameFlow 引用完整，退出按钮仍在右上。

- [x] **Step 3: 保存进度**

将每项实际结果、截图路径、未验收的真实鼠标限制写入验收报告；更新 progress.md 的三个阶段复选框和下一批候选。无 Git，不提交。
