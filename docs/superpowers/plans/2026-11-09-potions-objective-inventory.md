# RPG_Project Potions Objective and Inventory Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 实现药水掉落/拾取/Q 使用、击杀目标提示和 I 开关的只读简化背包，并保持现有战斗与死亡流程不回归。

**Architecture:** Hero 上的 PotionInventory 只管库存和治疗；HealthPotionPickup 与 LootDropper 负责场景交互；GameFlow 记录去重后的击杀进度；GameHUD 扩展显示；InventoryUI 独立管理只读面板。RpgUI 继续作为共享 UI 构建器。

**Tech Stack:** Tuanjie 2022.3.62t5、C#、uGUI、现有 GameDev.tv 模型、Universal Gold Icons、Fantasy RPG UI。

## Global Constraints

- 治疗固定 30；满血/空库存/死亡不消耗；敌人只掉落一次，拾取只入库一次。
- 不改 ATK、HP 上限、攻击范围、寻路或仇恨。
- 背包保持实时，不改 Time.timeScale；不做拖拽、装备更换、商店或存档。
- 每阶段先运行失败测试，再实现；保存场景并更新 `docs/progress.md`。
- 工程无 Git，不 commit；Bridge 使用 `node C:\Users\22492\.claude\tools\rpg-bridge.mjs`。
- 真实 Q/I/鼠标仍需人工验收；自动测试调用对应方法。

---

### Task 1: 药水库存、治疗、掉落和拾取

**Files:**
- Create: `Assets/Script/PotionInventory.cs`
- Create: `Assets/Script/HealthPotionPickup.cs`
- Create: `Assets/Script/LootDropper.cs`
- Create: `Assets/Perfabs/HealthPotionPickup.prefab`
- Modify: `Assets/Script/HeroController.cs`（Heal）
- Modify: `Assets/Script/EnemyController.cs`（Die 掉落）
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\PotionRuntimeTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_potions\`

**Interfaces:**
- Produces: `PotionInventory.Count`、`Add(int)`、`TryUse()`；`HeroController.Heal(float)`；`LootDropper.Drop()`；`HealthPotionPickup` 触发拾取。
- Consumes: 现有 `CurrentHP/MaxHP/IsDead/hpCanvas` 和 `EnemyController.Die`。

- [x] **Step 1: 备份并写失败测试**

备份 Script/*.cs 和场景到备份目录，前缀 `before-potions`。测试代码：

```csharp
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

Type inventoryType = Type.GetType("PotionInventory, Assembly-CSharp");
Type pickupType = Type.GetType("HealthPotionPickup, Assembly-CSharp");
Type dropperType = Type.GetType("LootDropper, Assembly-CSharp");
if (inventoryType == null || pickupType == null || dropperType == null)
    throw new Exception("FAIL: potion inventory/pickup/dropper types missing");
var hero = UnityEngine.Object.FindObjectOfType<HeroController>();
var enemy = UnityEngine.Object.FindObjectOfType<EnemyController>();
var inventory = (Component)hero.GetComponent(inventoryType);
if (inventory == null || enemy == null) throw new Exception("FAIL: potion scene wiring missing");
var count = inventoryType.GetProperty("Count");
var add = inventoryType.GetMethod("Add");
var use = inventoryType.GetMethod("TryUse");
Func<int> pickupCount = () => UnityEngine.Object.FindObjectsOfType(pickupType).Length;
double stageStart = EditorApplication.timeSinceStartup;
int stage = 0;
EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; throw new Exception("FAIL: play stopped"); }
    double elapsed = EditorApplication.timeSinceStartup - stageStart;
    if (stage == 0 && elapsed > 0.3)
    {
        if ((int)count.GetValue(inventory) != 0) throw new Exception("FAIL: initial potion count");
        if ((bool)use.Invoke(inventory, null)) throw new Exception("FAIL: empty inventory used");
        add.Invoke(inventory, new object[] { 2 });
        if ((int)count.GetValue(inventory) != 2) throw new Exception("FAIL: Add(2)");
        if ((bool)use.Invoke(inventory, null) || (int)count.GetValue(inventory) != 2) throw new Exception("FAIL: full HP consumed potion");
        hero.Hurt(50f, null);
        if (!(bool)use.Invoke(inventory, null) || hero.CurrentHP != 80f || (int)count.GetValue(inventory) != 1) throw new Exception("FAIL: first heal");
        if (!(bool)use.Invoke(inventory, null) || hero.CurrentHP != 100f || (int)count.GetValue(inventory) != 0) throw new Exception("FAIL: second heal/clamp");
        if ((bool)use.Invoke(inventory, null)) throw new Exception("FAIL: empty inventory after heals");
        stage = 1; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 1)
    {
        enemy.Hurt(1000f, hero.transform);
        enemy.Hurt(1000f, hero.transform);
        stage = 2; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 2 && elapsed > 0.3)
    {
        if (pickupCount() != 1) throw new Exception("FAIL: expected exactly one dropped potion");
        var pickup = (Component)UnityEngine.Object.FindObjectOfType(pickupType);
        hero.nav.Warp(pickup.transform.position);
        stage = 3; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 3 && elapsed > 0.5)
    {
        if (pickupCount() != 0 || (int)count.GetValue(inventory) != 1) throw new Exception("FAIL: pickup did not add exactly one");
        hero.Hurt(1000f, null);
        add.Invoke(inventory, new object[] { 1 });
        if ((bool)use.Invoke(inventory, null) || hero.CurrentHP != 0f || (int)count.GetValue(inventory) != 2) throw new Exception("FAIL: dead hero used potion");
        EditorApplication.update -= tick;
        Debug.Log("POTION RUNTIME TESTS PASS");
    }
};
EditorApplication.update += tick;
```

Expected before implementation: FAIL `potion inventory/pickup/dropper types missing`.

- [x] **Step 2: 实现库存、拾取和掉落**

```csharp
using UnityEngine;

public class PotionInventory : MonoBehaviour
{
    public int Count { get; private set; }
    public float healAmount = 30f;
    private HeroController hero;

    void Awake() { hero = GetComponent<HeroController>(); }
    void Update() { if (Input.GetKeyDown(KeyCode.Q)) TryUse(); }
    public void Add(int amount) { if (amount > 0) Count += amount; }

    public bool TryUse()
    {
        if (hero == null || hero.IsDead || Count <= 0) return false;
        float healed = hero.Heal(healAmount);
        if (healed <= 0f) return false;
        Count--;
        return true;
    }
}
```

```csharp
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class HealthPotionPickup : MonoBehaviour
{
    private bool picked;
    void Update() { transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World); }

    private void OnTriggerEnter(Collider other)
    {
        if (picked) return;
        var hero = other.GetComponentInParent<HeroController>();
        var inventory = hero != null ? hero.GetComponent<PotionInventory>() : null;
        if (inventory == null || hero.IsDead) return;
        picked = true;
        inventory.Add(1);
        Destroy(gameObject);
    }
}
```

```csharp
using UnityEngine;

public class LootDropper : MonoBehaviour
{
    public GameObject potionPickupPrefab;
    private bool dropped;

    public void Drop()
    {
        if (dropped) return;
        dropped = true;
        if (potionPickupPrefab != null) Instantiate(potionPickupPrefab, transform.position + Vector3.up * 0.25f, Quaternion.identity);
    }
}
```

在 HeroController 添加：

```csharp
public float Heal(float amount)
{
    if (isDeath || amount <= 0f) return 0f;
    float oldHP = HP_Now;
    HP_Now = Mathf.Clamp(HP_Now + amount, 0f, HP_Max);
    float healed = HP_Now - oldHP;
    if (healed > 0f && hpCanvas != null) hpCanvas.SetHP(HP_Now, HP_Max);
    return healed;
}
```

在 EnemyController.Die 的 Destroy 前加入：

```csharp
LootDropper dropper = GetComponent<LootDropper>();
if (dropper != null) dropper.Drop();
```

- [x] **Step 3: 创建拾取预制体并接线保存**

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scene = SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/RPG游戏.unity") throw new System.Exception("Wrong active scene");
var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MainAsset/MainAsset/Game/Pickups/Health Pickup.prefab");
if (source == null) throw new System.Exception("Health Pickup source missing");
var instance = Object.Instantiate(source);
instance.name = "HealthPotionPickup";
foreach (var child in instance.GetComponentsInChildren<Transform>(true))
{
    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
}
var trigger = instance.GetComponent<SphereCollider>();
if (trigger == null) trigger = instance.AddComponent<SphereCollider>();
trigger.isTrigger = true;
trigger.radius = 0.8f;
if (instance.GetComponent<HealthPotionPickup>() == null) instance.AddComponent<HealthPotionPickup>();
const string prefabPath = "Assets/Perfabs/HealthPotionPickup.prefab";
PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
Object.DestroyImmediate(instance);

var hero = Object.FindObjectOfType<HeroController>();
if (hero.GetComponent<PotionInventory>() == null) hero.gameObject.AddComponent<PotionInventory>();
var enemy = Object.FindObjectOfType<EnemyController>();
var dropper = enemy.GetComponent<LootDropper>();
if (dropper == null) dropper = enemy.gameObject.AddComponent<LootDropper>();
dropper.potionPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
if (dropper.potionPickupPrefab == null) throw new System.Exception("Potion prefab binding failed");
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("POTION SCENE SETUP SAVED");
```

- [x] **Step 4: 编译并运行 Play 测试**

Expected: `POTION RUNTIME TESTS PASS`。若自动拾取不触发，检查实际碰撞层/半径并修复；不得直接把 Add 调用塞进 Drop 来伪造拾取通过。

---

### Task 2: HUD 药水数量与击杀目标

**Files:**
- Modify: `Assets/Script/GameFlow.cs`
- Modify: `Assets/Script/GameHUD.cs`
- Modify: `Assets/Script/EnemyController.cs`（Die 注册击杀）
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\ObjectiveHudTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_potions\RPG游戏.before-objective.unity`

**Interfaces:**
- Produces: `GameFlow.KillCount`、`KillTarget`、`RegisterEnemyKill(int)`；`GameHUD.potionCountText`、`objectiveText`、`objectivePanel`、`potionInventory`。
- Consumes: Task 1 的库存和敌人死亡。

- [x] **Step 1: 写失败测试**

```csharp
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

var hud = UnityEngine.Object.FindObjectOfType<GameHUD>();
if (hud == null) throw new Exception("FAIL: GameHUD missing");
var potionField = typeof(GameHUD).GetField("potionCountText");
var objectiveField = typeof(GameHUD).GetField("objectiveText");
if (potionField == null || objectiveField == null) throw new Exception("FAIL: potion/objective HUD fields missing");
var potionText = (Text)potionField.GetValue(hud);
var objectiveText = (Text)objectiveField.GetValue(hud);
var hero = UnityEngine.Object.FindObjectOfType<HeroController>();
var enemy = UnityEngine.Object.FindObjectOfType<EnemyController>();
var inventory = hero.GetComponent<PotionInventory>();
double stageStart = EditorApplication.timeSinceStartup;
int stage = 0;
EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!EditorApplication.isPlaying) { EditorApplication.update -= tick; throw new Exception("FAIL: play stopped"); }
    double elapsed = EditorApplication.timeSinceStartup - stageStart;
    if (stage == 0 && elapsed > 0.5)
    {
        if (potionText.text != "× 0" || objectiveText.text != "目标：击败巡逻敌人 0 / 1") throw new Exception("FAIL: initial objective HUD");
        inventory.Add(2);
        stage = 1; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 1 && elapsed > 0.3)
    {
        if (potionText.text != "× 2") throw new Exception("FAIL: potion HUD count");
        enemy.Hurt(1000f, hero.transform);
        GameFlow.Instance.RegisterEnemyKill(enemy.GetInstanceID());
        stage = 2; stageStart = EditorApplication.timeSinceStartup;
    }
    else if (stage == 2 && elapsed > 0.3)
    {
        if (GameFlow.Instance.KillCount != 1 || objectiveText.text != "目标完成：巡逻敌人已击败") throw new Exception("FAIL: objective completion/deduplication");
        ScreenCapture.CaptureScreenshot("C:/Users/22492/.claude/tools/objective-hud.png");
        EditorApplication.update -= tick;
        Debug.Log("OBJECTIVE HUD TESTS PASS");
    }
};
EditorApplication.update += tick;
```

Expected before implementation: FAIL `potion/objective HUD fields missing`.

- [x] **Step 2: 实现击杀进度**

GameFlow 添加：

```csharp
private readonly System.Collections.Generic.HashSet<int> killedEnemies = new System.Collections.Generic.HashSet<int>();
public int KillCount { get { return killedEnemies.Count; } }
public int KillTarget = 1;
public void RegisterEnemyKill(int enemyId) { killedEnemies.Add(enemyId); }
```

EnemyController.Die 在 Destroy 前加入：

```csharp
if (GameFlow.Instance != null) GameFlow.Instance.RegisterEnemyKill(GetInstanceID());
```

- [x] **Step 3: 扩展 GameHUD**

添加公开字段：

```csharp
public PotionInventory potionInventory;
public Sprite potionSprite;
public Text potionCountText;
public Text objectiveText;
public RectTransform objectivePanel;
```

Build 末尾、targetPanel 隐藏后添加：

```csharp
RpgUI.CreateImage("PotionIcon", playerPanel, potionSprite, Color.white,
    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -10f), new Vector2(36f, 36f));
potionCountText = RpgUI.CreateText("PotionCount", playerPanel, "× 0", 20, Color.white, TextAnchor.MiddleRight,
    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-52f, -14f), new Vector2(60f, 28f));
var objectiveImage = RpgUI.CreateImage("ObjectivePanel", canvas.transform, panelSprite, new Color(1f, 1f, 1f, 0.9f),
    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -230f), new Vector2(340f, 54f));
objectivePanel = objectiveImage.rectTransform;
objectiveText = RpgUI.CreateText("ObjectiveText", objectivePanel, "目标：击败巡逻敌人 0 / 1", 20, Color.white, TextAnchor.MiddleCenter,
    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330f, 44f));
```

Awake 在 Build 前/后确保 `potionInventory = hero != null ? hero.GetComponent<PotionInventory>() : null`。LateUpdate 末尾添加：

```csharp
if (potionInventory == null && hero != null) potionInventory = hero.GetComponent<PotionInventory>();
potionCountText.text = potionInventory != null ? "× " + potionInventory.Count : "× 0";
int kills = GameFlow.Instance != null ? GameFlow.Instance.KillCount : 0;
int target = GameFlow.Instance != null ? GameFlow.Instance.KillTarget : 1;
objectiveText.text = kills >= target ? "目标完成：巡逻敌人已击败" : "目标：击败巡逻敌人 " + kills + " / " + target;
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
var hero = Object.FindObjectOfType<HeroController>();
if (hud == null || hero == null) throw new System.Exception("HUD or Hero missing");
hud.potionInventory = hero.GetComponent<PotionInventory>();
hud.potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Universal Gold Icons Sample/Potion_01(Red)_No BG.png");
if (hud.potionInventory == null || hud.potionSprite == null) throw new System.Exception("Potion HUD binding failed");
EditorUtility.SetDirty(hud);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("OBJECTIVE HUD SCENE SAVED");
```

Play 运行测试，预期 `OBJECTIVE HUD TESTS PASS`，截图目标与药水数量可读。

---

### Task 3: 简化背包

**Files:**
- Create: `Assets/Script/InventoryUI.cs`
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\InventoryUITests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_potions\RPG游戏.before-inventory.unity`

**Interfaces:**
- Produces: `InventoryUI.Toggle()`、`InventoryPanel`、`PotionValue`。
- Consumes: PotionInventory、RpgUI、现有图标。

- [x] **Step 1: 写失败测试**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

Type type = Type.GetType("InventoryUI, Assembly-CSharp");
if (type == null) throw new Exception("FAIL: InventoryUI type missing");
var ui = (Component)UnityEngine.Object.FindObjectOfType(type);
if (ui == null) throw new Exception("FAIL: InventoryUI scene component missing");
var panel = (GameObject)type.GetField("InventoryPanel").GetValue(ui);
var potionValue = (Text)type.GetField("PotionValue").GetValue(ui);
var toggle = type.GetMethod("Toggle");
var inventory = UnityEngine.Object.FindObjectOfType<HeroController>().GetComponent<PotionInventory>();
inventory.Add(3);
toggle.Invoke(ui, null);
double start = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (EditorApplication.timeSinceStartup - start < 0.3) return;
    EditorApplication.update -= tick;
    if (!panel.activeSelf || potionValue.text != "数量：3" || Time.timeScale != 1f) throw new Exception("FAIL: inventory open/content/pause");
    ScreenCapture.CaptureScreenshot("C:/Users/22492/.claude/tools/inventory-open.png");
    toggle.Invoke(ui, null);
    if (panel.activeSelf) throw new Exception("FAIL: inventory did not close");
    Debug.Log("INVENTORY UI TESTS PASS");
};
EditorApplication.update += tick;
```

Expected before implementation: FAIL `InventoryUI type missing`.

- [x] **Step 2: 实现 InventoryUI**

```csharp
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public PotionInventory inventory;
    public Sprite panelSprite;
    public Sprite potionSprite;
    public Sprite weaponSprite;
    public Sprite armorSprite;
    public GameObject InventoryPanel;
    public Text PotionValue;

    void Awake()
    {
        if (inventory == null)
        {
            var hero = FindObjectOfType<HeroController>();
            if (hero != null) inventory = hero.GetComponent<PotionInventory>();
        }
        Build();
    }

    void Update() { if (Input.GetKeyDown(KeyCode.I)) Toggle(); }
    public void Toggle() { InventoryPanel.SetActive(!InventoryPanel.activeSelf); }

    private void Build()
    {
        var canvas = RpgUI.CreateCanvas("InventoryCanvas", transform, true);
        var panel = RpgUI.CreateImage("InventoryPanel", canvas.transform, panelSprite, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 420f));
        InventoryPanel = panel.gameObject;
        RpgUI.CreateText("Title", panel.transform, "背包", 38, new Color(1f, 0.92f, 0.72f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(540f, 50f));
        RpgUI.CreateImage("PotionIcon", panel.transform, potionSprite, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(45f, -100f), new Vector2(64f, 64f));
        RpgUI.CreateText("PotionName", panel.transform, "治疗药水", 24, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(125f, -105f), new Vector2(200f, 32f));
        PotionValue = RpgUI.CreateText("PotionValue", panel.transform, "数量：0", 22, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(125f, -140f), new Vector2(200f, 32f));
        RpgUI.CreateImage("WeaponIcon", panel.transform, weaponSprite, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(45f, -210f), new Vector2(64f, 64f));
        RpgUI.CreateText("Weapon", panel.transform, "当前武器：铁剑", 24, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(125f, -220f), new Vector2(360f, 36f));
        RpgUI.CreateImage("ArmorIcon", panel.transform, armorSprite, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(45f, -290f), new Vector2(64f, 64f));
        RpgUI.CreateText("Armor", panel.transform, "当前护甲：无", 24, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(125f, -300f), new Vector2(360f, 36f));
        RpgUI.CreateText("Hint", panel.transform, "Q 使用药水　I 关闭背包", 20, new Color(1f, 0.9f, 0.7f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(540f, 32f));
        InventoryPanel.SetActive(false);
    }

    void LateUpdate() { PotionValue.text = "数量：" + (inventory != null ? inventory.Count : 0); }
}
```

- [x] **Step 3: 接线保存并测试**

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scene = SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/RPG游戏.unity") throw new System.Exception("Wrong active scene");
var systems = GameObject.Find("GameSystems");
var hero = Object.FindObjectOfType<HeroController>();
if (systems == null || hero == null) throw new System.Exception("GameSystems or Hero missing");
var ui = systems.GetComponent<InventoryUI>();
if (ui == null) ui = systems.AddComponent<InventoryUI>();
ui.inventory = hero.GetComponent<PotionInventory>();
ui.panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Parts/Background_01.png");
ui.potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Universal Gold Icons Sample/Potion_01(Red)_No BG.png");
ui.weaponSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Fantasy RPG UI Sample/UI/Parts/InventoryButton_weapon.png");
ui.armorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainAsset/MainAsset/Asset Packs/Universal Gold Icons Sample/T_Chest_04_Nobg.png");
if (ui.inventory == null || ui.panelSprite == null || ui.potionSprite == null || ui.weaponSprite == null || ui.armorSprite == null)
    throw new System.Exception("InventoryUI binding failed");
EditorUtility.SetDirty(ui);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("INVENTORY UI SCENE SAVED");
```

Play 运行测试。Expected: `INVENTORY UI TESTS PASS`；截图检查中文、图标与布局，Time.timeScale 始终为 1。

---

### Task 4: 最终回归与归档

**Files:**
- Update: `docs/progress.md`
- Create: `docs/superpowers/reports/2026-11-09-potions-inventory-verification.md`
- Create: `docs/verification/2026-11-09-potions/`

- [x] **Step 1: 重开状态复位**

扩展 DeathFlowTests 的重开断言：新 Hero 的 PotionInventory.Count=0、GameFlow.KillCount=0、InventoryUI.InventoryPanel 默认隐藏。运行死亡流测试，重开和返回主界面仍须通过。

- [x] **Step 2: 全量回归**

分别用干净 Play 运行 PotionRuntimeTests、ObjectiveHudTests、InventoryUITests、FeedbackRuntimeTests、HudRuntimeTests、PlayProbe5、PlayProbe6、SceneSwitchProbe；编辑态运行 CameraControlTests。读取每项实际结果，不复用已经销毁的服务或角色。

- [x] **Step 3: 持久化与归档**

停止 Play、从磁盘重载 RPG游戏，确认 Hero/PotionInventory、Enemy/LootDropper、GameSystems/InventoryUI 及所有 Sprite/Prefab 引用存在。归档 objective-hud.png、inventory-open.png 和战斗截图；写验收报告，更新 progress.md。记录既有控制台告警和真实按键未自动验收的限制。无 Git，不提交。
