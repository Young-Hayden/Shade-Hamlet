# RPG_Project 潜行动作控制基础验收

日期：2026-11-09

## 已交付

- `HeroDirectMovement.cs`：WASD 相机相对移动、Shift 奔跑、转向、重力、CharacterController 碰撞。
- `HeroController.SetControlMode(bool)`：直接模式与旧点选模式显式切换。
- 直接模式默认启用：NavMeshAgent 和旧 CapsuleCollider 禁用，CharacterController 启用，Rigidbody 保持 Kinematic。
- `TryDirectAttack()`：右键近身前方目标选择、朝向、挥剑；不再自动长距离追敌；复用原动画事件和攻击盒。
- `HumanoidRun` 状态使用现有 HumanoidRun.fbx，直接移动器按 Idle/Walk/Run 状态播放，攻击时不覆盖动画。
- 开局提示与暂停菜单更新为 WASD/Shift/右键/中键/Q/I/Esc。
- 旧点选模式保留并可运行。

## 测试证据

### 直接移动

实现前按预期失败：`HeroDirectMovement type missing`。固定步长入口实现前再次失败：`fixed-delta Move overload missing`。

最终 `DIRECT MOVEMENT FIXED-DELTA TESTS PASS`：

- 0.1 秒步行：0.4000006 米。
- 0.1 秒奔跑：0.6000009 米。
- 0.1 秒斜向输入：0.4000006 米，无斜向加速。
- 墙体完成物理注册后，持续前进停在 along=1.419999 米，没有穿墙。
- 死亡后继续输入，位移为 0。
- 动画帧后 `HUMANOID RUN STATE PLAY PASS`。

### 直接攻击

实现前按预期失败：`TryDirectAttack missing`。最终 `DIRECT ATTACK TESTS PASS`：

- 远距离挥剑不改变位置、不扣敌人 HP，不自动追击。
- 近身后挥剑，敌人 HP 100→80；同帧第二次调用不重复结算。
- `tarEnemy` 正确设置供 HUD 使用。

### 旧模式与系统回归

- `SetControlMode(false)` 后运行旧 PlayProbe5：敌人 100→80→60→40→20→0 并销毁，主角正常受伤。
- PotionRuntimeTests、FullHudTests、InventoryUITests、PauseMenuTests、DeathFlowTests、SceneSwitchProbe 均在直接模式下通过。
- 死亡重开继续复位 HP、药水、目标和背包；Time.timeScale=1。

## 测试过程中排除的假问题

1. 使用 EditorApplication.update 驱动位移时，0.50 秒调用 228 次，deltaTime 累计 0.83 秒；普通 Update 驱动也暴露了本机计时口径差异。最终改用固定 deltaTime 核心测试真实算法，不用放宽断言掩盖问题。
2. Animator.Play 同帧读取仍可能是旧状态；将状态存在与动画帧后实际播放分开验证。
3. 新建墙体在同帧尚未完成物理注册；等待物理帧后碰撞测试通过。
4. 编辑态 HeroController.nav 原引用为空，导致磁盘中的 NavMeshAgent 仍启用；已将真实组件引用写回场景并重验，运行逻辑不变。

## 持久化

`DIRECT CONTROL PERSISTENCE PASS`：directControl=true、CharacterController 启用、NavMeshAgent 禁用、旧 CapsuleCollider 禁用、HeroDirectMovement 引用正确、HumanoidRun 存在、Time.timeScale=1。

## 限制与下一步

- 自动测试使用显式输入和方法调用；真实 WASD、Shift、鼠标手感仍需用户试玩。
- 本阶段没有卫兵感知、潜行、刺杀或新关卡。
- 既有烘焙后端、地形树 shader、场景切换 Unknown Missing Script 提示仍保留。
- 下一子项目建议：卫兵 Patrol→Suspicious→Search→Chase→Attack 感知状态机；弓箭手可随后作为远程卫兵接入。

## 归档

- 截图：`docs/verification/2026-11-09-direct-control/direct-control.png`
- 备份：`D:\Unity\backups\RPG_Project_2026-11-09_direct-control\`
- 进度：`docs/progress.md`
