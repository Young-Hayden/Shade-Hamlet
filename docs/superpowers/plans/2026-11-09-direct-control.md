# RPG_Project Direct Action Control Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 为潜行动作方向实现 WASD/Shift 直接移动、相机相对控制和右键近身攻击，同时保留旧点选模式回归路径。

**Architecture:** HeroDirectMovement 独占直接位移与移动动画；HeroController 保留生命/攻击状态并通过 SetControlMode 切换直接/旧模式。直接攻击复用原动画事件和攻击盒，不复制伤害结算。

**Tech Stack:** Tuanjie 2022.3.62t5、C#、CharacterController、AnimatorController、现有 HumanoidRun/攻击动画。

## Global Constraints

- 步行 4、奔跑 6；输入 ClampMagnitude(1)；中键旋转、滚轮缩放、Q/I/Esc 不变。
- 直接模式 NavMeshAgent 禁用、CharacterController 启用、旧 CapsuleCollider 禁用；Rigidbody 保持 Kinematic。
- 右键不再自动追远敌；攻击仍由原 HeroAnimator 动画事件生成攻击盒。
- 本阶段不实现潜行、刺杀、警戒或关卡。
- 每阶段先失败后实现，保存场景并更新 progress.md；无 Git，不 commit。
- Bridge 使用 `node C:\Users\22492\.claude\tools\rpg-bridge.mjs`。

---

### Task 1: 直接移动、碰撞与奔跑动画

**Files:**
- Create: `Assets/Script/HeroDirectMovement.cs`
- Modify: `Assets/Script/HeroController.cs`
- Modify: `Assets/Animation/hero/heroController.controller`
- Modify: `Assets/Scenes/RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\DirectMovementTests.cs`
- Backup: `D:\Unity\backups\RPG_Project_2026-11-09_direct-control\`

**Interfaces:**
- Produces: `HeroDirectMovement.Move()`、`Move(Vector2,bool)`；`HeroController.directControl`、`directMovement`、`SetControlMode(bool)`。
- Consumes: CharacterController、Camera.main、HeroController.isAttack/IsDead、Animator。

- [x] **Step 1: 备份并写失败测试**

备份 Script、RPG游戏场景和 heroController.controller，前缀 `before-direct`。测试：

```csharp
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

Type type = Type.GetType("HeroDirectMovement, Assembly-CSharp");
if (type == null) throw new Exception("FAIL: HeroDirectMovement type missing");
var hero = UnityEngine.Object.FindObjectOfType<HeroController>();
var direct = (Component)hero.GetComponent(type);
var move = type.GetMethod("Move", new[] { typeof(Vector2), typeof(bool) });
var controllerField = type.GetField("controller");
if (direct == null || move == null || controllerField == null) throw new Exception("FAIL: direct movement API missing");
var controller = (CharacterController)controllerField.GetValue(direct);
var directControlField = typeof(HeroController).GetField("directControl");
if (directControlField == null) throw new Exception("FAIL: directControl field missing");
if (!(bool)directControlField.GetValue(hero) || hero.nav.enabled || !controller.enabled || hero.GetComponent<CapsuleCollider>().enabled)
    throw new Exception("FAIL: direct mode component state");
Vector2 input = Vector2.zero;
bool running = false;
Vector3 start = hero.transform.position;
Vector3 forward = Camera.main.transform.forward;
forward.y = 0f; forward.Normalize();
float walkDistance = 0f;
GameObject wall = null;
Vector3 deathPos = Vector3.zero;
double stageStart = EditorApplication.timeSinceStartup;
int stage = 0;
EditorApplication.CallbackFunction tick = null;
Action reset = () =>
{
    controller.enabled = false;
    hero.transform.position = start;
    controller.enabled = true;
};
tick = () =>
{
    try
    {
        if (!EditorApplication.isPlaying) throw new Exception("FAIL: play stopped");
        move.Invoke(direct, new object[] { input, running });
        double elapsed = EditorApplication.timeSinceStartup - stageStart;
        if (stage == 0 && elapsed > 0.5)
        {
            walkDistance = Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(hero.transform.position.x, 0f, hero.transform.position.z));
            if (walkDistance < 0.8f) throw new Exception("FAIL: walk did not move");
            reset(); input = Vector2.up; running = true;
            stage = 1; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 1 && elapsed > 0.5)
        {
            float runDistance = Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(hero.transform.position.x, 0f, hero.transform.position.z));
            if (runDistance < walkDistance * 1.25f) throw new Exception("FAIL: run not faster");
            if (!hero.anim.GetCurrentAnimatorStateInfo(0).IsName("HumanoidRun")) throw new Exception("FAIL: HumanoidRun state not playing");
            reset(); input = new Vector2(1f, 1f); running = false;
            stage = 2; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 2 && elapsed > 0.5)
        {
            float diagonal = Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(hero.transform.position.x, 0f, hero.transform.position.z));
            if (diagonal > walkDistance * 1.2f) throw new Exception("FAIL: diagonal speed boost");
            reset(); input = Vector2.up;
            wall = new GameObject("DirectMovementTestWall", typeof(BoxCollider));
            wall.transform.position = start + forward * 2f + Vector3.up * 1.5f;
            wall.transform.rotation = Quaternion.LookRotation(forward);
            wall.transform.localScale = new Vector3(4f, 3f, 0.3f);
            stage = 3; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 3 && elapsed > 1.0)
        {
            float along = Vector3.Dot(hero.transform.position - start, forward);
            if (along > 1.8f) throw new Exception("FAIL: CharacterController crossed wall");
            UnityEngine.Object.Destroy(wall);
            wall = null;
            hero.Hurt(1000f, null);
            input = Vector2.up;
            deathPos = hero.transform.position;
            stage = 4; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 4 && elapsed > 0.3)
        {
            if (Vector3.Distance(deathPos, hero.transform.position) > 0.001f) throw new Exception("FAIL: dead hero moved");
            EditorApplication.update -= tick;
            Debug.Log("DIRECT MOVEMENT TESTS PASS");
        }
    }
    catch (Exception ex)
    {
        if (wall != null) UnityEngine.Object.Destroy(wall);
        EditorApplication.update -= tick;
        Debug.LogException(ex);
    }
};
EditorApplication.update += tick;
input = Vector2.up;
```

Expected before implementation: FAIL `HeroDirectMovement type missing`.

- [x] **Step 2: 实现 HeroDirectMovement**

```csharp
using UnityEngine;

public class HeroDirectMovement : MonoBehaviour
{
    public HeroController hero;
    public CharacterController controller;
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public float rotationSpeed = 12f;
    public float gravity = -20f;
    private float verticalVelocity;
    private bool reportedMissingState;

    void Awake()
    {
        if (hero == null) hero = GetComponent<HeroController>();
        if (controller == null) controller = GetComponent<CharacterController>();
    }

    public void Move()
    {
        Move(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
    }

    public void Move(Vector2 input, bool running)
    {
        if (hero == null || hero.IsDead || controller == null || !controller.enabled) return;
        input = Vector2.ClampMagnitude(input, 1f);
        Camera cam = Camera.main;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();
        Vector3 planar = forward * input.y + right * input.x;
        bool moving = planar.sqrMagnitude > 0.001f;

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        else verticalVelocity += gravity * Time.deltaTime;
        float speed = running ? runSpeed : walkSpeed;
        controller.Move((planar * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (moving)
        {
            Quaternion desired = Quaternion.LookRotation(planar.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, rotationSpeed * Time.deltaTime);
        }
        UpdateAnimation(moving, running);
    }

    private void UpdateAnimation(bool moving, bool running)
    {
        if (hero.anim == null || hero.isAttack) return;
        string desired = moving ? (running ? "HumanoidRun" : "HumanoidWalk") : "HumanoidIdle";
        if (hero.anim.GetCurrentAnimatorStateInfo(0).IsName(desired)) return;
        if (!hero.anim.HasState(0, Animator.StringToHash(desired)))
        {
            if (!reportedMissingState) Debug.LogError("Missing direct movement animator state: " + desired);
            reportedMissingState = true;
            enabled = false;
            return;
        }
        hero.anim.Play(desired, 0, 0.08f);
    }
}
```

- [x] **Step 3: HeroController 模式切换**

添加字段：

```csharp
[Header("直接动作控制")]
public bool directControl = false;
public HeroDirectMovement directMovement;
```

添加方法：

```csharp
public void SetControlMode(bool value)
{
    directControl = value;
    if (directMovement == null) directMovement = GetComponent<HeroDirectMovement>();
    CharacterController cc = GetComponent<CharacterController>();
    CapsuleCollider capsule = GetComponent<CapsuleCollider>();
    if (value)
    {
        if (cc == null) cc = gameObject.AddComponent<CharacterController>();
        cc.height = 2.4f; cc.center = new Vector3(0f, 1.2f, 0f); cc.radius = 0.35f;
        cc.slopeLimit = 45f; cc.stepOffset = 0.3f;
        cc.enabled = true;
        if (capsule != null) capsule.enabled = false;
        if (nav != null) nav.enabled = false;
        if (directMovement != null) directMovement.enabled = true;
    }
    else
    {
        if (cc != null) cc.enabled = false;
        if (capsule != null) capsule.enabled = true;
        if (nav != null) nav.enabled = true;
        if (directMovement != null) directMovement.enabled = false;
    }
}
```

Start 在获取 nav/anim 后调用 `SetControlMode(directControl)`。Update 改为：

```csharp
if (isDeath) return;
if (directControl)
{
    if (directMovement != null) directMovement.Move();
}
else
{
    DoMove();
    DoAttack();
}
```

Task 1 的直接模式只接移动，不创建 DoDirectAttack/TryDirectAttack；Task 2 再添加这两个方法和 Update 中的右键入口，确保直接攻击红测试确实因功能缺失而失败。

- [x] **Step 4: 接线场景与 Run 状态**

```csharp
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

var scene = SceneManager.GetActiveScene();
if (scene.path != "Assets/Scenes/RPG游戏.unity") throw new System.Exception("Wrong active scene");
var hero = Object.FindObjectOfType<HeroController>();
var cc = hero.GetComponent<CharacterController>();
if (cc == null) cc = hero.gameObject.AddComponent<CharacterController>();
var direct = hero.GetComponent<HeroDirectMovement>();
if (direct == null) direct = hero.gameObject.AddComponent<HeroDirectMovement>();
direct.hero = hero;
direct.controller = cc;
hero.directMovement = direct;
hero.SetControlMode(true);

var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animation/hero/heroController.controller");
var runClip = AssetDatabase.LoadAllAssetsAtPath("Assets/MainAsset/MainAsset/Asset Packs/Animations/Unarmed/HumanoidRun.fbx")
    .OfType<AnimationClip>().FirstOrDefault();
if (controller == null || runClip == null) throw new System.Exception("Animator or run clip missing");
var root = controller.layers[0].stateMachine;
if (!root.states.Any(s => s.state.name == "HumanoidRun"))
{
    var runState = root.AddState("HumanoidRun");
    runState.motion = runClip;
}
EditorUtility.SetDirty(controller);
AssetDatabase.SaveAssets();
EditorUtility.SetDirty(hero);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("DIRECT CONTROL SCENE SAVED");
```

编译并 Play 运行 DirectMovementTests。Expected: `DIRECT MOVEMENT TESTS PASS`。若测试失败，修复实际移动/测试夹具，不进入 Task 2。

---

### Task 2: 直接模式右键攻击

**Files:**
- Modify: `Assets/Script/HeroController.cs`
- Test: `C:\Users\22492\.claude\tools\DirectAttackTests.cs`
- Backup: 同 Task 1 目录，额外保存 `HeroController.before-direct-attack.cs`

**Interfaces:**
- Produces: `HeroController.TryDirectAttack()`；私有 `DoDirectAttack()` 处理右键和 PointerOverUI。
- Consumes: attackRange、attackInterval、isAttack、现有动画事件/攻击盒。

- [x] **Step 1: 写失败测试**

```csharp
using System;
using UnityEngine;
using UnityEditor;

var hero = UnityEngine.Object.FindObjectOfType<HeroController>();
var enemy = UnityEngine.Object.FindObjectOfType<EnemyController>();
var attack = typeof(HeroController).GetMethod("TryDirectAttack");
if (attack == null) throw new Exception("FAIL: TryDirectAttack missing");
enemy.enabled = false;
enemy.agent.enabled = false;
double stageStart = EditorApplication.timeSinceStartup;
int stage = 0;
Vector3 farPos = hero.transform.position;
EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    try
    {
        double elapsed = EditorApplication.timeSinceStartup - stageStart;
        if (stage == 0)
        {
            attack.Invoke(hero, null);
            stage = 1; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 1 && elapsed > 1.1)
        {
            if (enemy.CurrentHP != 100f || Vector3.Distance(farPos, hero.transform.position) > 0.01f)
                throw new Exception("FAIL: direct attack auto-chased or damaged distant enemy");
            var cc = hero.GetComponent<CharacterController>();
            cc.enabled = false;
            hero.transform.position = enemy.transform.position + Vector3.forward * 1.2f;
            Vector3 dir = enemy.transform.position - hero.transform.position; dir.y = 0f;
            hero.transform.rotation = Quaternion.LookRotation(dir.normalized);
            cc.enabled = true;
            attack.Invoke(hero, null);
            attack.Invoke(hero, null);
            stage = 2; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 2 && elapsed > 0.8)
        {
            if (enemy.CurrentHP != 80f) throw new Exception("FAIL: direct melee did not deal exactly 20, HP=" + enemy.CurrentHP);
            if (hero.tarEnemy != enemy.gameObject) throw new Exception("FAIL: direct attack target/HUD selection");
            EditorApplication.update -= tick;
            Debug.Log("DIRECT ATTACK TESTS PASS");
        }
    }
    catch (Exception ex) { EditorApplication.update -= tick; Debug.LogException(ex); }
};
EditorApplication.update += tick;
```

Expected before implementation: FAIL `TryDirectAttack missing`。

- [x] **Step 2: 实现直接攻击**

Update 的直接分支在 `directMovement.Move()` 后添加 `DoDirectAttack();`。

```csharp
private void DoDirectAttack()
{
    if (Input.GetMouseButtonDown(1) && !PointerOverUI()) TryDirectAttack();
}

public void TryDirectAttack()
{
    if (isDeath || isAttack) return;

    EnemyController best = null;
    float bestDistance = float.MaxValue;
    foreach (EnemyController enemy in FindObjectsOfType<EnemyController>())
    {
        if (enemy == null || enemy.isDeath) continue;
        Vector3 dir = enemy.transform.position - transform.position;
        dir.y = 0f;
        float distance = dir.magnitude;
        if (distance > attackRange + 0.3f || distance < 0.001f) continue;
        if (Vector3.Dot(transform.forward, dir.normalized) < 0f) continue;
        if (distance < bestDistance) { best = enemy; bestDistance = distance; }
    }

    Vector3 facing = Vector3.zero;
    if (best != null)
    {
        tarEnemy = best.gameObject;
        enemyScript = best;
        facing = best.transform.position - transform.position;
    }
    else
    {
        tarEnemy = null;
        enemyScript = null;
        if (mainCamera != null) facing = mainCamera.transform.forward;
    }
    facing.y = 0f;
    if (facing.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(facing.normalized);

    isAttack = true;
    anim.ResetTrigger("attackEnd");
    anim.SetTrigger("attack");
    Invoke("AttackEnd", attackInterval);
}
```

- [x] **Step 3: 编译并验证**

先运行 DirectAttackTests，预期 `DIRECT ATTACK TESTS PASS`。再调用 `hero.SetControlMode(false)` 后运行旧 PlayProbe5，确认旧追击模式仍可用；每次测试使用干净 Play，避免控制器状态串扰。

---

### Task 3: UI 文案、保存点与全量回归

**Files:**
- Modify: `Assets/Script/GameHUD.cs`（操作提示文案）
- Modify: `Assets/Script/PauseMenu.cs`（操作说明文案）
- Update: `docs/progress.md`
- Create: `docs/superpowers/reports/2026-11-09-direct-control-verification.md`

- [x] **Step 1: 更新文案**

两处统一为：

`WASD 移动　Shift 奔跑　右键攻击　中键旋转　滚轮缩放　Q 药水　I 背包　Esc 菜单`

暂停菜单可分三行，但不得继续显示“左键移动”。

- [x] **Step 2: 全量回归**

直接模式下运行 DirectMovementTests、DirectAttackTests、FullHudTests、InventoryUITests、PauseMenuTests、PotionRuntimeTests、FeedbackRuntimeTests、PlayProbe6、DeathFlowTests、SceneSwitchProbe。旧模式通过 SetControlMode(false) 运行 PlayProbe5。必要时重启 Play，不能复用已销毁角色。

- [x] **Step 3: 持久化与归档**

停止 Play，从磁盘重载 RPG游戏，确认 Hero 的 directControl=true、CharacterController/HeroDirectMovement 存在且状态正确、AnimatorController 含 HumanoidRun。截取直接控制画面；归档测试输出与截图，更新 progress.md。记录真实 WASD/鼠标仍需用户试玩，既有控制台告警不冒充已修复。
