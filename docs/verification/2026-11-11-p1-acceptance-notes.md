# 影村 P1 验收笔记（验收会话记录，非开发方结论）

监控来源：`2026-11-11-p1-live.log`（开发会话 village-stealth-action-plan 实时追加）

## 待用户处理的确认项（SKIP，按用户要求跳过、不代为批准）

1. **覆盖既有 `ArcherEventOnlyTest.cs` 权限被拒**（15:41）
   - 原因：旧测试在敌人 `Start` 初始化前写目标，导致 HP=100 / arrows=0 的假象失败。
   - 开发方处理：未重试覆盖，改为新建 `P1ArcherEventOnlyTests.cs` 做初始化安全回归；生产逻辑未改。
   - 留给用户：是否允许改写/替换旧 `ArcherEventOnlyTest.cs`，否则新旧两套同类测试并存。

## 已报告的报错与修复（均声称未改生产逻辑，待验收核实）

- DirectAttack 旧夹具在 Start 前禁用敌人导致 HP=0 → 改等待初始化后通过（15:24）。
- ArcherEventOnlyTest 旧夹具 Start 前写目标导致 HP=100/arrows=0 → 新建测试绕过（15:41）。

## 已通过项（开发方自报，验收时需独立复核）

- P1 专项：常速 0.400、潜行 0.220、隐藏区进出、烟雾、声音半径、警戒 HUD。
- 回归：DirectMovement、FullHud、PauseMenu、Smoke、PotionRuntime、Mission、DirectAttack、Assassination。

## 独立验收过程记录（验收会话实测）

### 环境
- 验收不依赖开发方结论，自己通过 `C:\Users\22492\.claude\tools\rpg-bridge.mjs` 直连工程跑探针，脚本与结果在 `C:\Users\22492\.claude\tools\accept\`。
- 进 Play 的正确姿势（开发方提供）：editor 先 `EditorSceneManager.OpenScene` 从磁盘重载 → `manage_editor {"action":"play"}` → play 探针等待 `hero.CurrentHP>0 && enemy.CurrentHP>0 && enemy.senses!=null`。
- 直接用 `EditorApplication.EnterPlaymode()` 后立刻跑探针，会看到 Hero 完全不存在（见下）。

### 重要发现（待定性）：真实开局主角可能直接死亡
- 现象实测：Play 中 `FindObjectsOfType<HeroController>(true)=0`、`Resources.FindObjectsOfTypeAll<HeroController>()=0`、root 无 Hero；敌人、10 处隐藏区均正常。
- 开发方确认它也遇到过，并用临时日志定位：`HERO AWAKE → DIE hp=0 → DESTROY`，堆栈 `HeroController.Hurt ← AttackTriggerBox.OnTriggerEnter`。
- 开发方的规避方式是 `Time.timeScale=0` 进 Play 或先用夹具禁用敌人攻击，因此它全部 Play 测试都在"敌人被冻结"的前提下通过。
- 代码层面风险点：`HeroController.Start()` 先 `SetControlMode()` 再 `HP_Now = HP_Max`（HeroController.cs:89 与 :101），`HP_Now` 初始为 0；若 Start 未完成就受击，`Hurt()` 会把 0 夹紧为 0 并直接 `Die()`，2 秒后销毁（HeroController.cs:311-336, 370）。
- 正在做：不加任何夹具、不改 timeScale 的真实开局观察（accept-spawn.txt），以确定严重度与根因。

### 已排除的误判
- 场景里 Hero PrefabInstance 的 target guid 是 32 位 hex，而 Hero.prefab.meta 是 base64 新格式，文本搜不到对应关系 ≠ 资源丢失。`PrefabUtility` 实测：Hero prefab `Connected`、`IsPrefabAssetMissing=False`、路径 `Assets/Perfabs/Hero.prefab`。
- `EditorSceneManager.OpenScene` 从磁盘重开后 Hero 完好：directControl=true、PlayerStealthState/NoiseEmitter 在、敌人 2 个、隐藏区 10 个、场景非 dirty。P1 的持久化结论成立。

### 视觉初检（读开发方截图）
- `p1-hidden-stable.png`：主角在植被中、HUD「隐藏中」「警戒 0% 已脱离视线」，无关键 UI 重叠。
- `p1-hud-normal.png`：HUD 完整，但该帧画面里看不到主角（构图问题，非功能缺陷）。
- 素材低多边形、植被为黑色片状模型，按用户要求不纳入问题清单。

### 真实开局实测（accept-spawn2.txt，timeScale=0 进 Play 后恢复，全程无夹具、无传送）
- 初始：Hero HP=100/100、enabled=true、directControl=true，出生点 (-13.96, 1.60, -24.31)；近战卫兵距 6.33m、弓箭手距 19.85m，开局均看不见主角。
- 主角全程无输入站桩：
  - t≈9.8s 近战卫兵巡逻转到能看见主角，进入 Suspicious（警戒 0.02）。
  - 警戒 0.02→0.98 只用约 1.0 秒（距离 8.77m），t≈11.05s 进入 Chase。
    → 与公式 `rate = distanceFactor × 倍率 / timeToDetect`（0.763/0.8≈0.95/s）吻合。
    → **开发方报告里的 normalAlert=0.115/s、sneakAlert=0.073/s 与实测差约 8 倍，报告数值不可信，需以实测为准。**
  - t≈14.6s 近身到 1.55m 开始攻击，之后每 1 秒扣 5 血（100→95→90→…→50 @24.9s）。
  - t≈22.9s 弓箭手也进入 Chase。
  - 推算站桩约 35 秒死亡，尸体 2 秒后销毁。
- **此前"Play 中 Hero 完全消失"的原因由此确定**：不是预制体丢失，也不是血量未初始化（初始 HP 实测为 100），而是探针经桥接编译期间游戏已运行数十秒，主角被巡逻卫兵正常打死并销毁。本人此前关于"HP 初始化顺序导致开局即死"的怀疑**不成立，予以撤回**。
- 由此暴露的测试方法问题：开发方所有 Play 测试都在 timeScale=0 或冻结敌人的前提下进行，"无夹具真实开局"未纳入其验收范围。
- 平衡含义（实测）：卫兵追击约 2.0 m/s（3.5 秒从 8.56m 追到 1.55m），玩家走 4 / 跑 6 / 潜行 2.2 m/s，正常移动可甩开；但 9m 视锥内约 1 秒即被发现，潜行也只有约 1.6 秒、隐藏约 4.2 秒（按 0.65/0.25 倍率换算），潜行窗口很短。

### 独立探针 A（AcceptPlayStealth.cs，结果 PASS，accept-a-result.txt）
- 场景实测参数：卫兵 agentSpeed=2、accel=8、视距 9m、视锥 110°、timeToDetect=0.8、lostSightDelay=2、searchDuration=4、ATK=5、HP=100；弓箭手视距 12m、speed=2、ATK=5。玩家走 4 / 跑 6 / 潜行 2.2，潜行倍率 0.65、隐藏倍率 0.25。
- 三速固定步长（0.1s）：normal=0.400、run=0.600、**潜行按住 Shift=0.220（潜行不会变奔跑，正确）**。
- 隐藏区正例：区域内 IsHidden=true、倍率 0.25、状态 Hidden；负例：所有区域外 IsHidden=false、状态 Normal。
- **被发现时间实测（卫兵正前方 6m、无遮挡）**：正常 0.875s、潜行 1.345s、隐藏 3.493s。隐藏不是无敌，只是放慢发现，符合设计说明。
- 丢失视线：主角被传送走后，卫兵追击目的地与主角新位置相距 14.75m → **没有透视追踪**；2 秒后进入 Search，搜索结束回 Patrol、巡逻目标恢复 enemyPos1、agent 仍在 NavMesh 上且未停止（无卡死）。
- 噪声：范围内→Suspicious（警戒 0.274）、不授予实时追击目标；范围外→状态与 LastNoiseTime 均不变（负例通过）。脚步半径走 2.5 / 跑 6 / 潜行 0.8。



