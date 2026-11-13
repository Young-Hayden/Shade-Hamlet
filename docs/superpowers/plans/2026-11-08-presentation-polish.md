# RPG_Project Presentation Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 在不改战斗规则的前提下，加入受限相机缩放/旋转、精修两条头顶血条并小幅改善画面亮度。

**Architecture:** 相机行为集中在 `HeroCamera.cs`，输入只负责修改 yaw 和 distance，位置/旋转由初始偏移和初始朝向推导，避免改变原有构图。血条与光照通过一次性编辑器脚本修改场景，分别备份、分别保存和验收。测试脚本放在项目外，不进入 Assets。

**Tech Stack:** Tuanjie 2022.3.62t5、C#、Unity uGUI、Codely Bridge、Windows 文件备份。

## Global Constraints

- 相机缩放距离固定限制为 4～12 米；中键旋转，右键仍只用于选敌攻击。
- 血条只改 localPosition/localScale，保留现有 Canvas、Slider 和 HPScript 引用。
- 方向光与环境光单项增幅不超过 15%；不改天空盒、材质、烘焙、画质和阴影距离。
- 不改敌人仇恨、攻击力、血量和攻击距离，不新增玩法系统。
- 工程无 Git：不执行 commit，用文件备份作为回滚路径。
- Bridge 命令统一通过 `node C:\Users\22492\.claude\tools\rpg-bridge.mjs <command> '<json>'` 执行。

---

### Task 1: 相机缩放与中键旋转

**Files:**
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\HeroCamera.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-08_presentation\HeroCamera.before.cs`
- Test: `C:\Users\22492\.claude\tools\CameraControlTests.cs`

**Interfaces:**
- Consumes: 现有 `HeroCamera.HeroController`、`offset`、`smoothTime`。
- Produces: 私有字段 `distance`、`yaw`；私有方法 `ApplyZoom(float scroll)`、`ApplyOrbit(float mouseX)`，供反射测试和 LateUpdate 使用。

- [x] **Step 1: 备份并写失败测试**

```csharp
using System;
using System.Reflection;
using UnityEngine;

GameObject target = null, cameraObject = null;
try
{
    target = new GameObject("CameraTarget");
    cameraObject = new GameObject("CameraUnderTest");
    cameraObject.SetActive(false);
    cameraObject.transform.rotation = Quaternion.Euler(40.2f, 178.02f, 0f);
    var camera = cameraObject.AddComponent<HeroCamera>();
    camera.HeroController = target.transform;
    camera.offset = new Vector3(0f, 5f, 4f);
    cameraObject.SetActive(true);

    var type = typeof(HeroCamera);
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    var distance = type.GetField("distance", Flags);
    var yaw = type.GetField("yaw", Flags);
    var zoom = type.GetMethod("ApplyZoom", Flags);
    var orbit = type.GetMethod("ApplyOrbit", Flags);
    if (distance == null || yaw == null || zoom == null || orbit == null)
        throw new Exception("FAIL: camera zoom/orbit implementation is missing");

    zoom.Invoke(camera, new object[] { 100f });
    if (Math.Abs((float)distance.GetValue(camera) - 4f) > 0.001f)
        throw new Exception("FAIL: minimum distance is not 4");
    zoom.Invoke(camera, new object[] { -100f });
    if (Math.Abs((float)distance.GetValue(camera) - 12f) > 0.001f)
        throw new Exception("FAIL: maximum distance is not 12");
    orbit.Invoke(camera, new object[] { 10f });
    if (Math.Abs((float)yaw.GetValue(camera)) < 0.001f)
        throw new Exception("FAIL: middle-mouse orbit did not change yaw");
    Debug.Log("CAMERA CONTROL TESTS PASS");
}
finally
{
    if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
    if (target != null) UnityEngine.Object.DestroyImmediate(target);
}
```

Run:
`node C:\Users\22492\.claude\tools\rpg-bridge.mjs execute_csharp_script '{"script_path":"C:/Users/22492/.claude/tools/CameraControlTests.cs","capture_logs":true}'`

Expected before implementation: FAIL with `camera zoom/orbit implementation is missing`.

- [x] **Step 2: 备份当前脚本**

```bash
python -I -c "import os,shutil; d=r'D:\Unity\backups\RPG_Project_2026-11-08_presentation'; os.makedirs(d,exist_ok=True); shutil.copy2(r'D:\Unity\projects\RPG_Project\Assets\Script\HeroCamera.cs',d+r'\HeroCamera.before.cs')"
```

Expected: `HeroCamera.before.cs` exists.

- [x] **Step 3: 实现相机控制**

将 `HeroCamera.cs` 完整替换为：

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public class HeroCamera : MonoBehaviour
{
    public Transform HeroController;
    public Vector3 offset = new Vector3(0f, 5f, 4f);
    public float smoothTime = 0.12f;

    [Header("缩放与旋转")]
    public float minDistance = 4f;
    public float maxDistance = 12f;
    public float zoomSpeed = 1.5f;
    public float rotationSensitivity = 5f;
    public float obstructionPadding = 0.25f;

    private Vector3 baseOffset;
    private Quaternion baseRotation;
    private Vector3 velocity;
    private float distance;
    private float yaw;

    void Awake()
    {
        if (offset == Vector3.zero) offset = new Vector3(0f, 5f, 4f);
        baseOffset = offset;
        baseRotation = transform.rotation;
        distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
    }

    private bool PointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void ApplyZoom(float scroll)
    {
        distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
    }

    private void ApplyOrbit(float mouseX)
    {
        yaw += mouseX * rotationSensitivity;
    }

    private bool IsActor(Transform value)
    {
        return value.GetComponentInParent<HeroController>() != null
            || value.GetComponentInParent<EnemyController>() != null;
    }

    void LateUpdate()
    {
        if (!PointerOverUI())
        {
            ApplyZoom(Input.mouseScrollDelta.y);
            if (Input.GetMouseButton(2)) ApplyOrbit(Input.GetAxis("Mouse X"));
        }

        if (HeroController != null)
        {
            Quaternion orbit = Quaternion.AngleAxis(yaw, Vector3.up);
            Vector3 desired = HeroController.position + orbit * (baseOffset.normalized * distance);
            Vector3 focus = HeroController.position + Vector3.up * 1.2f;
            Vector3 ray = desired - focus;
            RaycastHit hit;
            if (ray.sqrMagnitude > 0.001f
                && Physics.Raycast(focus, ray.normalized, out hit, ray.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && !IsActor(hit.transform))
            {
                desired = hit.point + hit.normal * obstructionPadding;
            }

            transform.position = smoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime)
                : desired;
            Quaternion desiredRotation = orbit * baseRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-15f * Time.deltaTime));
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
```

- [x] **Step 4: 编译并运行测试**

Run:
`node C:\Users\22492\.claude\tools\rpg-bridge.mjs manage_editor '{"action":"request_compile"}'`
then the test command from Step 1.

Expected: compilation completed successfully and `CAMERA CONTROL TESTS PASS`.

- [x] **Step 5: Play 模式输入与战斗回归**

Enter Play, call `manage_input` with `create_virtual_devices`, then `mouse_scroll` at Game View center using `scroll_y=1` and `scroll_y=-1`; sample camera-to-hero distance before/after. Use `mouse_drag` with `button=2` from Game View center to 120 px right and verify camera yaw changes while `HeroController.tarEnemy` remains unchanged. Finally run `PlayProbe5.cs` unchanged.

Expected: distance stays within 4～12, orbit changes only while middle button is held, right-click combat still damages enemy, and no new Console exception appears. If virtual input is unavailable, record that limitation and use reflection-driven Play verification instead of claiming real mouse input passed.

---

### Task 2: 两条头顶血条精修

**Files:**
- Modify: `D:\Unity\projects\RPG_Project\Assets\Scenes\RPG游戏.unity`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-08_presentation\RPG游戏.before-hp.unity`
- Apply/Test: `C:\Users\22492\.claude\tools\ApplyHealthBarPolish.cs`

**Interfaces:**
- Consumes: 场景中父物体分别为 Hero、Enemy 的两个 `HPScript`。
- Produces: Hero 血条 localPosition `(0,2.05,0)`；Enemy 血条 `(-0.03,2.00,0.01)`；两者 localScale 均为 `(0.008,0.008,0.008)`。

- [x] **Step 1: 备份场景并记录基线**

```bash
python -I -c "import shutil; shutil.copy2(r'D:\Unity\projects\RPG_Project\Assets\Scenes\RPG游戏.unity',r'D:\Unity\backups\RPG_Project_2026-11-08_presentation\RPG游戏.before-hp.unity')"
```

Baseline already measured: Hero y=2.39, Enemy y=2.31, scale=0.01.

- [x] **Step 2: 写场景修改脚本并应用**

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

if (SceneManager.GetActiveScene().path != "Assets/Scenes/RPG游戏.unity")
    throw new System.Exception("Wrong active scene");
int changed = 0;
foreach (var hp in Object.FindObjectsOfType<HPScript>(true))
{
    if (hp.transform.parent == null) continue;
    var rect = hp.GetComponent<RectTransform>();
    if (rect == null) throw new System.Exception("HPCanvas is missing RectTransform");
    if (hp.transform.parent.name == "Hero")
    {
        rect.anchoredPosition = new Vector2(0f, 2.05f);
        rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0f);
    }
    else if (hp.transform.parent.name == "Enemy")
    {
        rect.anchoredPosition = new Vector2(-0.03f, 2.00f);
        rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0.01f);
    }
    else
        continue;
    rect.localScale = Vector3.one * 0.008f;
    EditorUtility.SetDirty(rect);
    changed++;
}
if (changed != 2) throw new System.Exception("Expected exactly 2 health bars, got " + changed);
EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
Debug.Log("HEALTH BAR POLISH APPLIED count=" + changed);
```

Expected: exactly two bars changed and scene saved. Do not run if scene path is not `Assets/Scenes/RPG游戏.unity`.

- [x] **Step 3: 回读与视觉验收**

Re-read both transforms in editor, then enter Play and capture `C:\Users\22492\.claude\tools\hp-after.png` using `ScreenCapture.CaptureScreenshot`.

Expected: exact values match Step 2; both bars visible, upright, smaller, and clear of character heads. Run `PlayProbe5.cs`; Slider values must still track damage. If a bar disappears or covers the head, restore `RPG游戏.before-hp.unity` and report failure instead of accepting it.

---

### Task 3: 小幅调光与最终回归

**Files:**
- Modify: `D:\Unity\projects\RPG_Project\Assets\Scenes\RPG游戏.unity`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-08_presentation\RPG游戏.before-lighting.unity`
- Apply/Test: `C:\Users\22492\.claude\tools\ApplyLightingPolish.cs`

**Interfaces:**
- Consumes: Task 2 已验收的场景。
- Produces: `RenderSettings.ambientIntensity = 1.12f`；Directional Light `intensity = 1.08f`。回滚值均为 `1.0f`。

- [x] **Step 1: 单独备份光照前场景**

Copy the current scene to `RPG游戏.before-lighting.unity`; this preserves the approved health-bar changes.

- [x] **Step 2: 应用固定数值并保存**

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

if (SceneManager.GetActiveScene().path != "Assets/Scenes/RPG游戏.unity")
    throw new System.Exception("Wrong active scene");
RenderSettings.ambientIntensity = 1.12f;
var lights = Object.FindObjectsOfType<Light>();
int changed = 0;
foreach (var light in lights)
{
    if (light.type != LightType.Directional) continue;
    light.intensity = 1.08f;
    EditorUtility.SetDirty(light);
    changed++;
}
if (changed != 1) throw new System.Exception("Expected one directional light, got " + changed);
EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
Debug.Log("LIGHTING POLISH APPLIED ambient=1.12 directional=1.08");
```

- [x] **Step 3: 截图对比并决定保留或回滚**

Capture `lighting-after.png` in Play with the same default camera state. Compare against `hp-after.png`: terrain and characters should be slightly brighter, with no clipped white highlights and shadows still readable. If worse, restore ambient=1.0 and directional=1.0, save, and report the rollback.

- [x] **Step 4: 最终回归**

Run compilation, camera tests, PlayProbe5 combat, hero death probe, and scene-switch probe. Read Console errors. Stop Play and verify the saved scene still contains exactly the approved health-bar and lighting values.

Expected: combat and death behavior remain as previously verified; only the pre-existing terrain-tree and lighting-backend messages may remain. Do not claim real mouse/UI usability without actual input or screenshot evidence.

---

### Task 4: 基于现有素材的玩法与 UI 优化建议

**Files:**
- Create: `D:\Unity\projects\RPG_Project\docs\2026-11-08-gameplay-ui-opportunities.md`
- Read: existing Fantasy RPG UI Sample, Universal Gold Icons Sample, GameDev.tv Assets, SimpleFX, Synty environment, animation and weapon folders.

**Interfaces:**
- Consumes: verified asset paths only.
- Produces: prioritized recommendations, each listing reusable assets, player value, implementation cost and risk. No scene or gameplay implementation.

- [x] **Step 1: 核实素材路径与可用类型**

Inventory existing weapon prefabs, equipment/inventory UI, icons, environment buildings and SimpleFX effects. Exclude demo scripts from claims about currently working gameplay.

- [x] **Step 2: 写优先级建议**

Organize into low-cost combat feedback, inventory/equipment progression, patrol/encounter structure, HUD/menu consistency, and death/restart flow. Every recommendation must name its existing assets and distinguish “asset exists” from “feature already implemented”.

- [x] **Step 3: 自检与交付**

Check every referenced path exists, avoid presenting unimplemented systems as complete, and summarize the three implemented presentation fixes separately from future proposals.
