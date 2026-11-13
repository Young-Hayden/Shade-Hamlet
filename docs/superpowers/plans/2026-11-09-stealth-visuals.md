# RPG_Project Stealth Visuals Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 用现有资源实现更低的第三人称探索相机、黄昏潜行光照、可通行植被掩体和更克制的击杀特效。

**Architecture:** 三阶段分别修改相机、光照、掩体/击杀资源引用；每阶段独立备份、保存、回读和截图。不新增玩法脚本，不改卫兵 AI 或 NavMesh。

**Tech Stack:** Tuanjie 2022.3.62t5、C# 编辑器脚本、BOXOPHOBIC Night Skybox、SimpleFX、Synty 植被。

## Global Constraints

- 相机 offset=`(0,3.2,4.2)`，初始 pitch=25°、yaw=178.02°；缩放仍 4～12 米。
- 夜景使用现有 `Skybox Cubemap Extended Night.mat`；ambient=0.85、方向光=0.65、冷蓝 `(0.65,0.75,1,1)`。
- 两个暖色火点：range=5、intensity=1、shadows=None；不新增 NavMesh 障碍。
- 植被不启用 Collider，不写隐藏判定；击杀效果改 `FX_Dust_Prefab_02.prefab`。
- 每阶段保存场景并更新 docs/progress.md；无 Git，不 commit。

---

### Task 1: 第三人称探索相机

**Files:**
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\StealthCameraTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_stealth-visuals\RPG游戏.before-camera.unity`

- [x] **Step 1: 备份、写失败断言并记录旧图**

备份场景；Play 截取 `stealth-camera-before.png`。编辑器测试：

```csharp
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

if (SceneManager.GetActiveScene().path != "Assets/Scenes/RPG游戏.unity") throw new Exception("Wrong scene");
var camera = UnityEngine.Object.FindObjectOfType<HeroCamera>();
if (camera == null) throw new Exception("HeroCamera missing");
if (Vector3.Distance(camera.offset, new Vector3(0f, 3.2f, 4.2f)) > 0.001f)
    throw new Exception("FAIL: stealth camera offset not applied");
if (Math.Abs(Mathf.DeltaAngle(Camera.main.transform.eulerAngles.x, 25f)) > 0.1f)
    throw new Exception("FAIL: stealth camera pitch not applied");
Debug.Log("STEALTH CAMERA EDITOR PASS");
```

Expected before: FAIL offset/pitch not applied。

- [x] **Step 2: 应用并保存相机值**

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scene = SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/RPG游戏.unity") throw new System.Exception("Wrong scene");
var heroCamera = Object.FindObjectOfType<HeroCamera>();
heroCamera.offset = new Vector3(0f, 3.2f, 4.2f);
Camera.main.transform.rotation = Quaternion.Euler(25f, 178.02f, 0f);
EditorUtility.SetDirty(heroCamera);
EditorUtility.SetDirty(Camera.main.transform);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("STEALTH CAMERA SAVED");
```

- [x] **Step 3: 回读与 Play 验收**

从磁盘重载并运行编辑器断言，再运行 CameraControlTests、DirectMovementTests 和 DirectCollisionTest。Play 截取 `stealth-camera-after.png`，检查角色完整、前方视野和建筑遮挡。失败则回滚本阶段。

---

### Task 2: 黄昏光照与两个暖色火点

**Files:**
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\StealthLightingTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_stealth-visuals\RPG游戏.before-lighting.unity`

- [x] **Step 1: 备份并写失败测试**

```csharp
using System;
using UnityEngine;

if (RenderSettings.skybox == null || RenderSettings.skybox.name != "Skybox Cubemap Extended Night")
    throw new Exception("FAIL: night skybox not applied");
if (Math.Abs(RenderSettings.ambientIntensity - 0.85f) > 0.001f) throw new Exception("FAIL: ambient intensity");
var root = GameObject.Find("StealthLighting");
if (root == null || root.GetComponentsInChildren<Light>().Length != 2) throw new Exception("FAIL: expected two warm route lights");
foreach (var light in root.GetComponentsInChildren<Light>())
{
    if (light.type != LightType.Point || light.range != 5f || light.intensity != 1f || light.shadows != LightShadows.None)
        throw new Exception("FAIL: route light settings");
}
Debug.Log("STEALTH LIGHTING EDITOR PASS");
```

Expected before: FAIL night skybox/lighting missing。

- [x] **Step 2: 应用光照与火点**

```csharp
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scene = SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/RPG游戏.unity") throw new System.Exception("Wrong scene");
RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/MainAsset/MainAsset/Asset Packs/BOXOPHOBIC/Skybox Cubemap Extended/Demo/Materials/Skybox Cubemap Extended Night.mat");
RenderSettings.ambientIntensity = 0.85f;
var sun = Object.FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
if (sun == null) throw new System.Exception("Directional light missing");
sun.intensity = 0.65f;
sun.color = new Color(0.65f, 0.75f, 1f, 1f);
var old = GameObject.Find("StealthLighting");
if (old != null) Object.DestroyImmediate(old);
var root = new GameObject("StealthLighting");
var firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MainAsset/MainAsset/Asset Packs/SimpleFX/Prefabs/FX_Fire.prefab");
var terrain = Object.FindObjectOfType<Terrain>();
Vector3[] positions = { new Vector3(-12.16f, 0f, -18.10f), new Vector3(1.64f, 0f, -18.28f) };
foreach (var position in positions)
{
    Vector3 pos = position;
    if (terrain != null) pos.y = terrain.transform.position.y + terrain.SampleHeight(pos) + 0.05f;
    var fire = (GameObject)PrefabUtility.InstantiatePrefab(firePrefab);
    fire.name = "Route Fire";
    fire.transform.SetParent(root.transform);
    fire.transform.position = pos;
    var light = fire.GetComponent<Light>();
    if (light == null) light = fire.AddComponent<Light>();
    light.type = LightType.Point;
    light.color = new Color(1f, 0.55f, 0.25f);
    light.range = 5f;
    light.intensity = 1f;
    light.shadows = LightShadows.None;
}
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("STEALTH LIGHTING SAVED");
```

- [x] **Step 3: 回读与截图验收**

重载场景运行编辑器断言，Play 截取 `stealth-lighting.png`。确认无粉色天空、角色与地面仍可读、火点不悬空。若视觉变差，恢复 before-lighting 场景并报告，不强行接受。

---

### Task 3: 可通行植被掩体与击杀尘土

**Files:**
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\StealthCoverTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_stealth-visuals\RPG游戏.before-cover.unity`

- [x] **Step 1: 备份并写失败测试**

```csharp
using System;
using UnityEngine;

var root = GameObject.Find("StealthCoverVisuals");
if (root == null || root.transform.childCount < 10) throw new Exception("FAIL: stealth cover clusters missing");
foreach (var collider in root.GetComponentsInChildren<Collider>(true))
    if (collider.enabled) throw new Exception("FAIL: visual cover must not block movement/nav");
var feedback = UnityEngine.Object.FindObjectOfType<CombatFeedback>();
if (feedback == null || feedback.killEffect == null || feedback.killEffect.name != "FX_Dust_Prefab_02")
    throw new Exception("FAIL: kill dust not bound");
Debug.Log("STEALTH COVER EDITOR PASS");
```

Expected before: FAIL cover/kill dust missing。

- [x] **Step 2: 创建掩体并绑定击杀尘土**

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scene = SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/RPG游戏.unity") throw new System.Exception("Wrong scene");
var old = GameObject.Find("StealthCoverVisuals");
if (old != null) Object.DestroyImmediate(old);
var root = new GameObject("StealthCoverVisuals");
string basePath = "Assets/MainAsset/MainAsset/Asset Packs/Synty - Polygon Student Sample/Prefabs/";
var grass = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "SM_Env_Grass_Large_01.prefab");
var bush = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "SM_Env_Bush_01.prefab");
var terrain = Object.FindObjectOfType<Terrain>();
Vector3[] positions = {
    new Vector3(-11f,0,-17f), new Vector3(-8f,0,-16f), new Vector3(-5f,0,-15f),
    new Vector3(-1f,0,-15f), new Vector3(2f,0,-16f), new Vector3(4f,0,-20f),
    new Vector3(1f,0,-23f), new Vector3(-3f,0,-25f), new Vector3(-7f,0,-24f), new Vector3(-10f,0,-22f)
};
for (int i = 0; i < positions.Length; i++)
{
    Vector3 pos = positions[i];
    if (terrain != null) pos.y = terrain.transform.position.y + terrain.SampleHeight(pos) + 0.02f;
    var prefab = i % 3 == 2 ? bush : grass;
    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    instance.name = i % 3 == 2 ? "Visual Bush" : "Visual Tall Grass";
    instance.transform.SetParent(root.transform);
    instance.transform.position = pos;
    instance.transform.rotation = Quaternion.Euler(0f, (i % 4) * 45f, 0f);
    instance.transform.localScale = Vector3.one * (i % 3 == 2 ? 1.2f : 1.35f);
    foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
}
var feedback = Object.FindObjectOfType<CombatFeedback>();
feedback.killEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MainAsset/MainAsset/Asset Packs/SimpleFX/Prefabs/FX_Dust_Prefab_02.prefab");
EditorUtility.SetDirty(feedback);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("STEALTH COVER SAVED");
```

- [x] **Step 3: 回读、截图和击杀清理**

重载场景运行编辑器断言，Play 截取 `stealth-cover.png`。对敌人调用一次致命 Hurt，确认只生成一个 `FX_Dust_Prefab_02(Clone)`，2.5 秒后销毁；重复 Hurt 不再生成。运行 DirectCollisionTest 确认通行和碰撞未受视觉植被影响。

---

### Task 4: 全量回归与归档

**Files:**
- Update: `docs/progress.md`
- Create: `docs/superpowers/reports/2026-11-09-stealth-visuals-verification.md`
- Create: `docs/verification/2026-11-09-stealth-visuals/`

- [x] **Step 1: 全量回归**

干净 Play 运行 DirectMovementTests、DirectCollisionTest、RunAnimationStateTest、DirectAttackTests、PotionRuntimeTests、FullHudTests、InventoryUITests、PauseMenuTests、DeathFlowTests、SceneSwitchProbe；旧模式 PlayProbe5 另行验证。记录实际输出，不复用销毁对象。

- [x] **Step 2: 持久化与控制台**

停止 Play，从磁盘重载，回读相机/夜景/光照/火点/植被/击杀资源引用。记录既有烘焙后端、地形树和 Unknown Missing Script 提示，不宣称清零。

- [x] **Step 3: 保存进度与截图**

归档全部 before/after 与最终截图，写验收报告，更新 progress.md；下一项仍是卫兵感知 AI，植被目前只有视觉遮蔽，不宣称已经能隐藏玩家。无 Git，不提交。
