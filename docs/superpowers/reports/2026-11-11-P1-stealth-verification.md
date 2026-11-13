# P1 动作与感知手感验收报告

日期：2026-11-11  
项目：`D:\Unity\projects\RPG_Project`  
阶段：P1 潜行状态、隐藏区域、听觉、警戒解释与相机参数

## 本阶段交付

- 新增 `PlayerStealthState.cs`：区分正常、潜行、已被发现、隐藏中和烟雾掩护；Ctrl 潜行，速度 2.2。
- 新增 `StealthCoverArea.cs`：10 处高草/灌木对应的显式触发/重叠区域；不是看到植被就自动无敌。
- 新增 `NoiseEmitter.cs`：步行、奔跑、潜行脚步与普通战斗噪声；声音包含位置、半径、强度、时间和来源。
- `GuardSenses.cs`：警戒增长受距离、潜行和隐藏倍率影响；范围内声音只进入 Suspicious 并记录一次位置，不直接授予持续 Chase 坐标。
- `SmokeZone.cs`：提供烟雾内状态查询，仍由原有 SmokeAbility/SmokeZone 管理生命周期。
- `GameHUD.cs`：增加潜行状态、警戒百分比和“正在暴露/正在搜索/已发现/已脱离视线”说明；操作提示加入 Ctrl。
- `HeroCamera.cs`：保存探索/潜行/交战距离 5.28/4.75/5.54，保留滚轮、旋转和 SmoothDamp。
- `RangedAttack.cs`：目标统一从 GuardSenses/EnemyController 同步，感知重置后不再保留过期目标。

## 实际功能与回归结果

以下均为本轮当前版本实际运行结果：

| 测试 | 结果 |
|---|---|
| P1StealthTests | 通过：普通固定步长 0.400，潜行 0.220；隐藏区进入/离开、烟雾状态、范围内/外声音、HUD 均通过 |
| P1PerceptionTests | 通过：同位置同时段正常警戒 0.115、潜行 0.073；远程目标同步与清除通过 |
| P1GuardSensesTests | 通过：视锥、实体墙遮挡、Chase、Search、恢复 Patrol、受击与死亡禁用 |
| P1ArcherEventOnlyTests | 通过：heroHP=95；命中后箭矢销毁，arrowsAfterHit=0 属于正常生命周期 |
| P1CoverNavTests | 通过：近战卫兵的 4 个巡逻点全部 PathComplete |
| DirectMovementTests | 通过：walk=0.4、run=0.6、diagonal=0.4，死亡不移动 |
| DirectAttackTests | 通过：远距离不自动追敌，近身一次攻击扣 20 |
| AssassinationTests | 通过 |
| SmokeTests | 通过 |
| PotionRuntimeTests | 通过 |
| MissionTests | 通过 |
| FullHudTests / InventoryUITests / PauseMenuTests | 通过 |
| DeathFlowTests | 通过 |
| SceneSwitchProbe | 通过：RPG游戏→界面→RPG游戏，返回后 Hero/Enemy 存在 |

最终编译摘要：`isCompiling=false`、`errors=0`、`warnings=0`。

## 测试中发现并修正的问题

1. 外部新脚本首次编译前未被 AssetDatabase 导入；刷新并重新编译后通过。
2. 隐藏区仅依赖 Trigger 时，测试传送不能稳定登记；增加显式区域的确定重量叠检测，同时保留 Trigger，未放宽隐藏断言。
3. 第一个“离开隐藏区”测试点落入另一片灌木；移到所有区域外后通过。
4. 固定步长测试暂停 HeroController 后，脚步声仍沿用旧 IsMoving；生产代码增加控制状态检查，并在 HeroDirectMovement.OnDisable 清理移动状态。
5. 范围外声音测试把 ResetPerception 前的 LastNoiseTime 与清零后的值比较；修正取样顺序，状态仍要求 Patrol。
6. 旧直接攻击/弓箭测试在 Start 初始化 HP 前禁用敌人或写目标；增加初始化等待。旧感知测试还可能选到 19.6 米外弓箭手；新 P1 感知测试明确选择近战卫兵并隔离另一名敌人。
7. 编辑态 CoverNavTests 读取尚未赋值的 agent；新 P1 通路测试在 Play 中等待 NavMeshAgent 初始化后执行。

这些修正没有把失败断言改成跳过，也没有修改伤害、感知条件或任务通过条件来迎合测试。

## 持久化、引用与场景红线

- 修改前备份：`D:\Unity\backups\RPG_Project_2026-11-11_P1-stealth\`，包含场景和 33 个脚本，共 34 个文件。
- 停止 Play 并从磁盘重新打开 `RPG游戏.unity` 后：
  - Hero 的 PlayerStealthState、NoiseEmitter 存在；
  - 潜行速度 2.2 保持；
  - 10 个隐藏区域及其启用的 Trigger 碰撞器存在；
  - 相机三组距离保持；
  - 敌人保持启用，测试夹具未泄漏进保存场景；
  - 三个新脚本均有导入后的 MonoScript。
- 新增隐藏区域只有 Trigger，不阻挡移动，不需要重烘焙；仍重新验证了 4 个巡逻点完整路径。
- 未修改共享材质、原始动画 FBX、武器挂点或原始美术资源；未提交 Git。

## 视觉自检

已实际读取以下当前版本截图：

- `docs/verification/2026-11-11-p1/p1-hud-normal.png`：正常状态、100/100、警戒 0%、操作提示与四动作槽可见。
- `docs/verification/2026-11-11-p1/p1-hidden-stable.png`：主角在植被中可见，HUD 显示“隐藏中”，没有关键 UI 重叠。
- `docs/verification/2026-11-11-p1/p1-full-hud-regression.png`：完整 HUD 回归截图。

`p1-hidden.png` 是过渡帧，未作为最终视觉证据；另取稳定帧 `p1-hidden-stable.png` 后才判定视觉通过。

## 控制台

最终停止 Play、重载场景后的控制台只有本轮诊断/验收日志，无新增编译或运行时异常。场景切换测试仍出现此前已记录的：

- Progressive GPU 烘焙后端提示；
- 地形树 Soft Occlusion 提示；
- Unknown Missing Script 场景切换提示。

这些既有问题未被宣称为本轮修复或清零。

## 待用户处理的确认项

- 自动权限检查拒绝覆盖既有外部测试文件 `C:\Users\22492\.claude\tools\ArcherEventOnlyTest.cs`。已按要求跳过覆盖，没有重试；改用新文件 `P1ArcherEventOnlyTests.cs` 完成当前版本回归。建议后续由用户决定是否把初始化安全版本合并回旧测试文件。
- 无其它待确认的生产场景删除、资源删除、提交或发布操作。

## 限制与下一步

- 真实 Ctrl/WASD/鼠标手感仍需用户亲自试玩；Bridge 脚本和按钮调用不等于完整键鼠验收。
- 首版没有蹲伏/攀爬动画；潜行使用现有 Walk，不宣称跑酷。
- 声音首版不做墙体声学衰减；尸体发现与警报网络属于 P2，尚未实施。
- P1 阶段完成；P2 刺杀反馈、尸体发现与有限警报扩散尚未开始。
