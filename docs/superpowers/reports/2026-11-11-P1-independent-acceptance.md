# 《影村》P1 独立验收报告

日期：2026-11-11
验收方：独立验收会话（非 P1 开发会话）
对象：`D:\Unity\projects\RPG_Project`，P1 动作与感知手感（潜行状态、隐藏区域、听觉、警戒解释、情境相机）
开发方自报：`2026-11-11-P1-stealth-verification.md`（全绿）

**独立验收结论：P1 功能通过——无穿模、无巡逻卡死、无透视追踪，刺杀与任务闭环可用。需处理 2 个中等项（出生点无安全区且不在开发方验收覆盖内、开发方报告警戒数值失真）与 3 个轻微项；真实键鼠手感仍待用户试玩。**

## 验收方式

不复述开发方结论。通过 `C:\Users\22492\.claude\tools\rpg-bridge.mjs` 直连工程，自写 5 组独立探针在真实 Play 中实测，探针与原始数据保存在 `C:\Users\22492\.claude\tools\accept\`：

| 探针 | 内容 | 结果文件 |
|---|---|---|
| 环境/持久化 | 磁盘重开场景、预制体状态、组件启用状态 | accept-reopen.txt / accept-prefab.txt / accept-components.txt |
| 开局观察 | 无夹具真实开局 25 秒 | accept-spawn2.txt |
| A 潜行感知 | 三速、隐藏区正负例、三态被发现时间、丢失视线、噪声 | accept-a-result.txt（PASS） |
| B 世界 | 巡逻卡死、通路、实战、逃跑平衡、截图 | accept-b-result.txt（PASS） |
| C/C2 穿模 | 实体掩体撞击，胶囊重叠复验 | accept-c-result.txt / accept-c2-result.txt（PASS） |
| D 玩法闭环 | 刺杀正负例、卷轴、撤离胜利 | accept-d-result.txt |

探针只读运行状态、传送角色、调用公开逻辑，**未修改任何生产脚本、未保存场景**。全程未改伤害、感知条件或通过条件。

## 一、功能验收（独立实测通过）

- **持久化**：`EditorSceneManager.OpenScene` 从磁盘重开后，Hero 存在、`directControl=true`、`PlayerStealthState`/`NoiseEmitter` 在、敌人 2 个、隐藏区 10 个、场景非 dirty。Hero 预制体 `Connected`、`IsPrefabAssetMissing=False`。
- **移动三速**（0.1s 固定步长）：正常 0.400、奔跑 0.600、潜行 0.220；**潜行按住 Shift 仍是 0.220**，不会被 Shift 顶成奔跑。
- **隐藏区正/负例**：区域内 `IsHidden=true`、侦测倍率 0.25、状态 Hidden；所有区域外 `IsHidden=false`、状态 Normal。
- **听觉**：范围内噪声 → Suspicious（警戒 0.274）、只记录一次性位置、不授予实时追击目标；范围外噪声 → 状态与 `LastNoiseTime` 完全不变。
- **丢失视线**：主角转移后，卫兵追击目的地与主角新位置相距 14.75m，**确认无透视追踪**；2 秒后进入 Search，搜索结束回 Patrol、巡逻目标恢复 `enemyPos1`、agent 仍在 NavMesh 上。
- **巡逻**：4 个巡逻点全部 `PathComplete`；连续 20 秒自由巡逻位移 37.65m、最长静止 0.5s、速度 2.00、`hasPath=true`——**没有卡死**。
- **穿模**：围栏被挡住（停在原侧 1.24m）、小岩石可跨过、土坡可登顶（Y 1.62→2.39），三者的**玩家胶囊与掩体碰撞体重叠均为 False**，即没有陷入或穿墙。
  （初版探针用 `Collider.ClosestPoint` 判定，对非凸 MeshCollider 产生 2 条假阳性；改用 `Physics.OverlapCapsule` 复验后排除，两次原始结果均保留备查。）
- **战斗**：贴身被追时每秒扣 5 血；全速逃跑 5 秒距离由 1.55m 拉开到 22.11m，卫兵由 Chase 转 Search——**玩家跑得掉，不会被无限黏住**。

- **刺杀**：正面 `CanAssassinate=false`、`TryAssassinate=false`；背后 1.2m 可刺杀，模拟攻击动画事件后敌人 100→0、`isDeath=true`（一击必杀且走真实动画事件结算）。
- **任务闭环**：未拿卷轴时撤离点 `CanInteract=false`；拾取卷轴后 Stage=Escape、拾取物消失；撤离 `Interact=true`、Stage=Completed、胜利面板生成，截图 `accept-victory.png` 目检正常（任务完成 / 重新挑战 / 返回主界面）。

## 二、平衡实测数据

| 项目 | 实测值 |
|---|---|
| 玩家速度 | 走 4 / 跑 6 / 潜行 2.2 m/s |
| 卫兵 | 追击 2 m/s、视距 9m、视锥 110°、ATK 5、HP 100 |
| 弓箭手 | 视距 12m、追击 2 m/s、ATK 5 |
| 警戒参数 | timeToDetect 0.8s、lostSightDelay 2s、searchDuration 4s |
| **被发现时间（正前方 6m 无遮挡）** | **正常 0.875s / 潜行 1.345s / 隐藏 3.493s** |
| 脚步噪声半径 | 走 2.5 / 跑 6 / 潜行 0.8 m |
| 真实开局站桩 | 约 9.8s 被巡逻卫兵看见、11.05s 被锁定、14.6s 近身挨打、之后每秒 -5 血，约 35 秒死亡 |

## 三、问题清单（按严重度）

### 中等 1：出生点没有安全区，站桩约 35 秒被打死，且不在开发方验收覆盖内
真实开局主角出生 6.33m 内就有近战卫兵巡逻，巡逻转向后约 1 秒锁定。玩家不动约 35 秒死亡。
开发方全部 Play 测试都以 `timeScale=0` 或冻结敌人为前提，**"无夹具真实开局"从未纳入它的验收**，所以它的全绿不代表开局体验正常。
建议（不擅自改）：出生点后移或加掩体、开局给 2~3 秒无敌/教学提示、或把最近巡逻路径拉远。

### 中等 2：开发方报告中的警戒数值与实测差约 8 倍
报告称 `normalAlert=0.115/s、sneakAlert=0.073/s`（相当于 8.7s / 13.7s 才被发现）。
独立实测为正常 0.875s、潜行 1.345s（6m 处，速率约 1.14/s 与 0.74/s），与代码公式 `distanceFactor × 倍率 / timeToDetect` 一致。
**结论：功能实现是对的，是报告数字失真。后续做平衡请以本次实测为准。**

### 平衡建议（非缺陷）：潜行窗口很短
9m 视锥内正常 0.875 秒即被发现，潜行只延长到 1.345 秒，隐藏 3.493 秒。潜行带来的收益主要是"噪声半径 0.8m"而非"更难被看见"。
若希望潜行成为主流打法，可考虑提高 `timeToDetect`、加大潜行倍率差距，或把警戒拆成"起疑→确认"两段给玩家反应时间。是否调整由用户决定。

### 轻微 1：`StealthCoverArea.detectionMultiplier` 是死字段
`PlayerStealthState.DetectionMultiplier` 固定使用自身 `HiddenDetectionMultiplier=0.25`，从不读取各隐藏区自己的 `detectionMultiplier`，因此无法按区域调整隐蔽效果。

### 轻微 2：`PlayerStealthState.Update` 每帧 `FindObjectsOfType<GuardSenses>()`
仅用于判断是否有人 Chase。敌人增多后是无谓开销，建议缓存或改成事件通知。

### 轻微 3：潜行没有蹲伏姿态
沿用 Walk 动画、仅速度降到 2.2，视觉上有滑步感。开发方已在报告中声明，素材层面按用户要求不算缺陷。

### 既有未修（开发方如实保留，未宣称清零）
场景切换的 Unknown Missing Script 提示、Progressive GPU 烘焙后端提示、地形树 Soft Occlusion 提示。

## 四、待用户处理的确认项（开发方已按规则跳过）

- 覆盖既有外部测试 `C:\Users\22492\.claude\tools\ArcherEventOnlyTest.cs` 被权限拒绝，开发方未重试，改为新建 `P1ArcherEventOnlyTests.cs`。
  **需要用户决定**：是否允许把初始化安全写法合并回旧测试文件；不合并则新旧两套弓箭测试并存，功能无影响。
- 无其它待确认的生产场景删除、资源删除、提交或发布操作。

## 五、验收范围与限制

- 本次独立复测重点：P1 新增能力（潜行/隐藏/听觉/警戒）、穿模、卡死、平衡、真实开局、刺杀与任务闭环。
- 药水、背包、暂停、死亡重开、场景往返、完整 HUD 等沿用开发方回归结论，本次未逐条重跑。
- **真实键鼠手感仍未验收**：工程未装 Input System，桥接无法注入真实鼠标键盘，探针是显式调用移动/交互逻辑。Ctrl 潜行、E 刺杀/拾取/撤离的实际按键体验需用户亲自试玩。
- 刺杀、拾取、撤离都绑定 **E 键**，三者近距离同时满足时的优先级未做真人验证。

## 六、编辑器交接状态

验收结束后：停止 Play、`Time.timeScale=1`、从磁盘重载 `RPG游戏.unity`，不保留任何探针残留；未保存场景、未改生产脚本、未提交 Git。
