# RPG_Project 潜行动作控制基础设计

日期：2026-11-09
状态：方案 A（CharacterController 直接移动）已获用户批准

## 目标

将主角从纯点选寻路扩展为 WASD 直接控制，支持 Shift 奔跑、现有中键视角/滚轮缩放和右键动作攻击，为后续潜行与刺杀提供稳定控制基础。

## 范围

本阶段只实现动作控制，不实现卫兵视野、警戒、搜索、草丛、刺杀、任务或新关卡。

## 控制方案

- WASD：相对相机水平朝向移动，输入向量长度 Clamp 为 1，斜向不加速。
- Shift：奔跑；步行速度沿用 4，奔跑速度 6。
- 中键拖动：现有 HeroCamera 旋转；滚轮缩放 4～12 米不变。
- 右键：挥剑；优先朝向攻击范围内且位于前方的最近活敌，没有目标时朝相机水平前方挥剑。
- 右键不再触发旧模式的长距离自动追敌。直接模式需要玩家自己接近敌人。
- Q/I/Esc 不变；背包实时、暂停菜单暂停的规则不变。

## 组件与兼容

### `HeroDirectMovement.cs`

- 与 HeroController 同物体，持有 CharacterController、Animator 和相机引用。
- `Move()` 读取真实输入；`Move(Vector2 input, bool running)` 接收显式输入供测试，二者共用同一个位移核心。
- CharacterController 负责重力、地面、坡度和碰撞；walkSpeed=4、runSpeed=6、slopeLimit=45、stepOffset=0.3。
- 角色朝移动方向平滑转向；无输入时只处理重力。
- 直接状态使用 HumanoidIdle/HumanoidWalk/新 HumanoidRun；攻击期间不强行切移动状态，死亡由 HeroController 接管。
- 新增 Run 状态来自现有 `Unarmed/HumanoidRun.fbx`，不改原 FBX。

### `HeroController`

- 新增 `public bool directControl` 和 HeroDirectMovement 引用。
- 直接模式 Start 时：确保 CharacterController 存在，停用 NavMeshAgent，禁用旧 CapsuleCollider 以免与 CharacterController 双重碰撞；Rigidbody 仍保持 Kinematic 供触发检测。
- Update：死亡仍直接返回；directControl=true 时调用 `directMovement.Move()` 与 `DoDirectAttack()`，否则继续旧 `DoMove()/DoAttack()`。
- 旧模式代码保留，用于对照和回归；场景默认 directControl=true。
- `DoDirectAttack` 沿用 attackInterval、isAttack、Animator attack Trigger、Invoke(AttackEnd) 和现有动画事件/攻击盒，不复制伤害结算。
- 直接模式目标只用于朝向与 HUD；不把远处目标变成自动追击目的地。

## 动画状态处理

- 编辑器为 heroController 添加无过渡的 HumanoidRun 状态；直接移动器在移动状态变化时显式 `Animator.Play`，避免给旧 Walk/Idle 转换再叠加条件冲突。
- 攻击仍由原 AnyState/Trigger 转换处理；直接移动器在 HeroController.isAttack 时不覆盖攻击状态。
- 死亡仍由原 death Bool 处理。
- 若 Run 状态未接线，启动时报明确错误并停止使用，不静默退回错误动画。

## UI 文案

- 开局提示与暂停菜单说明改为：`WASD 移动　Shift 奔跑　右键攻击　中键旋转　滚轮缩放　Q 药水　I 背包　Esc 菜单`。
- 开始界面和任务文本本阶段不改。

## 验证

1. 新类型/直接模式测试在实现前失败，再实现通过。
2. 显式输入 `(0,1)` 使角色沿相机前方移动；左右/斜向长度正确；斜向速度不超过单轴速度。
3. Shift 对应速度 6，步行 4；松开恢复；角色落地且不穿过房屋。
4. 直接模式 NavMeshAgent 禁用，CharacterController 启用，旧 CapsuleCollider 不重复启用。
5. 右键在远距离不自动追敌；近身挥剑仍能通过动画事件和攻击盒造成伤害。
6. 移动状态与 Run 状态存在；攻击不被移动状态每帧覆盖；死亡后不再移动。
7. HUD、药水、背包、暂停、死亡重开、场景往返回归。
8. 真实按键仍需用户试玩；自动测试通过显式输入和按钮事件验证。

## 保存与回滚

- 备份目录：`D:\Unity\backups\RPG_Project_2026-11-09_direct-control\`。
- 分为“直接移动与碰撞”“动作攻击与动画”“UI 文案与全量回归”三个保存点。
- 每点更新 docs/progress.md；旧 directControl=false 分支和脚本/场景备份共同作为回退路径。
- 工程无 Git，不提交。
