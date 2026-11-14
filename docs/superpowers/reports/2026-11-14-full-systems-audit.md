# 2026-11-14 《影村》全系统审计报告

范围：寻路、攻击、角色/敌人/剧情状态机、菜单与场景过渡。审计在 Play 模式脚本驱动下完成，并配合既有回归与三剧情路径。

## 1. 角色动画状态机

依次驱动并读取 Animator 状态哈希：

| 动作 | 状态 |
|---|---|
| 静止 | `HumanoidIdle` 189409114 |
| 走路 | `HumanoidWalk` 1526720538 |
| 奔跑 | `HumanoidRun` 859178273 |
| 潜行 | `HumanoidSneak` -1383648164 |
| 停止后 | 回到 `HumanoidIdle` |
| 攻击 | 攻击状态 `2Hand-Sword-Attack5`，实际剪辑 `Sword-Attack-R6` |
| 死亡 | `Armed-Death1`，0.2 秒完成过渡并正常播放 |

死亡时碰撞体全部禁用、死亡面板出现、`timeScale=1`。

## 2. 攻击

- 2m 外：不造成伤害（HP 100 不变），仅生成挥剑攻击盒，随后自动清理。
- 1m 内：普通攻击扣 20（100→80）。
- 冷却期间再次点击：不重复扣血（保持 80）。
- 冷却结束再次攻击：扣到 60。
- 攻击结束后场上攻击盒数量为 0，无泄漏。
- 敌人近战：4 秒内主角 HP 100→85，共 3 次、每次 5 点；敌人保持 Chase、`isAttack=True`。
- **发现并修复**：直接模式下攻击动画期间移动逻辑仍在执行，60 帧滑行 0.85m。已在 `HeroDirectMovement.Move` 增加 `hero.isAttack` 站定门槛；修复后攻击期间位移 0.00m，攻击结束后可正常移动 0.91m。

## 3. 敌人状态机与寻路

- Guard 状态机：`Patrol → Chase`（受击）→ `Search`（丢失视线 2.6s）→ `Patrol`（搜索超时），全部符合预期。
- 5 个敌人（队长 + 4 剑兵）均 `onMesh=True`、`pathStatus=PathComplete`，1 秒内位移 1.16–1.76m。
- 4 个剑兵巡逻点根节点均独立于人物；6 秒实测位移 3.09–5.44m，巡逻点静止 4/4，无原地转圈。
- 20 秒人物与地图碰撞扫描 `uniquePairs=0`。

## 4. 剧情、菜单与过渡

- 第一章三路径：`kill` / `kill_after_truth` / `spare` 全部通过。
- 主菜单：背景/暗角/标题/副标题/两个按钮与持久化接线 7 项全 PASS。
- 过渡：黑场淡出、章节标题、异步加载、淡入、字幕延后全部通过；真实 `Menu_Start.onClick` 进入 RPG。
- 旧 MissionController/EscapePoint 在剧情模式处于 disabled，不会因击杀新增剑兵误触发旧任务。
- 死亡重开、返回主菜单、暂停与场景切换通过现有回归。

## 5. 发现项与结论

真正缺陷：

1. 攻击期间可滑行 0.85m（已修复）。

已排除的假阳性：

1. 潜行状态曾按“动画剪辑名”显示为 `HumanoidWalk`，实际状态哈希为 `HumanoidSneak`（复用同一剪辑，速度 0.55）。
2. 死亡动画曾在 `timeScale=0` 的剧情暂停中测试，Animator 停住；正常游戏状态下 `Armed-Death1` 正常播放。

回归结果：

- 既有玩法回归 **11/11 PASS**。
- 第一章三剧情路径 **kill / kill_after_truth / spare 全部 PASS**。
- 修复后 `RPG游戏` 场景无未保存改动，最终 MD5 `0bba043a98a82fccf3f2009b386e31b9`；`HeroDirectMovement.cs` MD5 `17a0ce1ebb2a831a63eafa25f9703d5e`。


## 6. 证据

- `docs/verification/2026-11-14-roster-menu/full_systems_audit.txt`
- `docs/verification/2026-11-14-roster-menu/attack_movement_tests.txt`
- `docs/verification/2026-11-14-roster-menu/death_anim_diag.txt`
- `docs/verification/2026-11-14-roster-menu/patrol_motion_tests.txt`
- 既有回归日志与三剧情路径日志

限制：脚本驱动审计覆盖了当前逻辑分支，但不等于真人键鼠手感验收；攻击盒命中基于胶囊体，不覆盖极端动画穿插。
