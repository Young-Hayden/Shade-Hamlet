# 2026-11-21 玩法冲突 / NPC 寻路 / 场景审计

> 2026-11-21 后续：本文发现的 4 项玩法/UI 冲突与 1 个巡逻点问题已修复，回归通过；修复内容与筛查结果见第 7 节。以下第 1–3 节保留修复前的审计记录。

范围：`RPG游戏` 场景当前磁盘版本（场景 MD5 `0bba043a98a82fccf3f2009b386e31b9`）。
方式：编辑器静态扫描 + Play 模式脚本实测；只读，未修改脚本、未保存场景。工程未安装 Input System，按键类结论由代码路径 + 直接调用处理方法复现，未做真实键鼠注入。

## 结论摘要

- **玩法/UI 冲突：存在 4 项可复现冲突 + 1 项代码级风险。** 都发生在“剧情暂停 / 菜单暂停 / 死亡”与其它输入系统叠加时；正常按流程操作不会阻断主线，但属于真实 bug。
- **NPC 寻路：没有功能性卡死。** 5 个敌人 20 秒巡逻全部 `onMesh`、`PathComplete`，位移 35–38m；队长到井边路径完整。唯一问题是 `SwordGuard_East_B` 的一个巡逻点不在 NavMesh 上（1.12m），会提前换点，但不会卡死。
- **场景问题：未发现缺失脚本、穿模、越界或碰撞缺失。** 建筑 22 个全部有碰撞；实体装饰不可行走；道具/植被相对地形无实际悬空。

## 1. 玩法与 UI 冲突（可复现）

### C1. 暂停菜单关闭后把剧情暂停“解除”了（高危）

- 现象：开场字幕/对话期间打开暂停菜单，再关闭，`Time.timeScale` 从 0 变为 1，但剧情 UI 仍在显示，`StoryDirector` 仍处于暂停状态。
- 实测（`play_audit.txt`）：
  - 字幕期间：`after close: pauseOpen=False storyUi=True timeScale=1 CONFLICT=True`
  - 对话期间：`pause close: storyUi=True timeScale=1 CONFLICT=True`
- 根因：`PauseMenu.Toggle()` 直接写 `Time.timeScale`（`PauseMenu.cs:62-67`），`StoryDirector.SetGameplayPaused()` 也直接写（`StoryDirector.cs:280-299`），二者互不知情。
- 影响：剧情界面还在，但世界时间已恢复；主角/敌人组件仍被剧情系统禁用，状态不一致；剧情期间的时间推进与表现可能异常。

### C2. 剧情 UI 打开时药水/烟雾/背包仍可使用（中危）

- 实测（对话打开、`timeScale=0`）：
  - `inventory while dialogue: open=True`
  - `potion while dialogue: used=True`（HP 100，仍可加/用药水）
  - `smoke while dialogue: activated=True zones=1`
- 根因：
  - `PotionInventory.Update`（`Items/PotionInventory.cs:17`）
  - `SmokeAbility.Update`（`Stealth/SmokeAbility.cs:27`）
  - `InventoryUI.Update`（`UI/InventoryUI.cs:38`）
  均只判断按键，不检查 `StoryDirector.IsUiOpen` 或暂停状态。`Time.timeScale=0` 不会停止 `Update` 和输入读取。
- 影响：对话/字幕时可消耗或获得资源、放烟雾、打开背包，背包还会叠在剧情 UI 上。

### C3. 暂停时右键攻击会进入“锁定攻击”状态（中危）

- 实测（`conflict_probe.txt`）：
  - 暂停中调用攻击：`isAttack=True boxes=0 timeScale=0`
  - 1 秒实时后仍为 `True`；解除暂停 1.3 秒后才 `isAttack=False`
- 根因：`HeroController.Update` 在 `timeScale=0` 下仍执行 `DoDirectAttack`（`Player/HeroController.cs:125-133, 266-313`）；`Invoke("AttackEnd", attackInterval)` 使用缩放时间，暂停期间不会触发。暂停菜单也没有暂停/禁用 `HeroController`。
- 影响：暂停中右键会让角色锁定攻击，恢复后短暂不能立即移动/再次攻击。

### C4. 死亡面板显示时背包仍可打开（低危）

- 实测：`deathPanel=True heroDead=True` 后 `inventoryOpenBehindDeath=True`。
- 根因：`PauseMenu` 有死亡判断，但 `InventoryUI.Update`（`UI/InventoryUI.cs:38`）没有检查 `GameFlow.IsDeathPanelShown`。
- 影响：死亡结算界面上会叠加背包。

### C5. 输入推进层叠（代码级风险，未注入真实按键）

- `SubtitleOverlay` 用 `Input.anyKeyDown` 推进（`Story/SubtitleOverlay.cs:70`），`PauseMenu` 用 Esc（`UI/PauseMenu.cs:39`）；字幕期间按 Esc 会同时推进字幕并打开暂停。
- `DialoguePanel` 用 E/左键推进（`Story/DialoguePanel.cs:122`），暂停菜单打开时不会阻断。
- `SceneTransition.BlocksGameplay` 只被 `HeroDirectMovement` 检查（`Player/HeroDirectMovement.cs:75`）；`HeroController` 攻击、`AssassinationController`、背包等不检查过渡状态，黑场 0.7 秒内仍可能触发攻击/交互。

## 2. NPC 寻路

### 2.1 20 秒巡逻实测（关闭感知，隔离路径）

| 敌人 | 位移 | 最大停滞 | onMesh | 路径状态 |
|---|---:|---:|---|---|
| SwordGuard_North_A | 35.17m | 0.25s | True | PathComplete |
| SwordGuard_East_A | 36.51m | 0.25s | True | PathComplete |
| SwordGuard_North_B | 35.83m | 0.25s | True | PathComplete |
| Enemy（队长） | 38.39m | 0.25s | True | PathComplete |
| SwordGuard_East_B | 36.64m | 0.25s | True | PathComplete |

- 5 个巡逻点根节点均独立于人物（`rootIsChildOfEnemy=False`），不会随人物移动绕圈。
- 全部敌人 → 主角、→ 井边剧情点 `PathComplete=True`。
- 队长 → 井边：`pathComplete=True`，实际 3 秒移动 4.42m，`onMesh=True`。

### 2.2 唯一数据问题：East_B `Patrol_1` 离网

- 位置 `(31.43, 1.53, 8.46)`，最近 NavMesh 点在 `(30.37, 1.56, 8.81)`，距离 **1.12m**；精确路径 `PathInvalid`。
- 最近点周围无碰撞体。
- 实际影响有限：`SetDestination` 会吸附到最近点，卫兵停在离原始巡逻点约 1.12m 处；巡逻换点阈值是 1.5m，因此会正常换点，不会卡死。表现为该点巡逻略微提前/不自然。

### 2.3 穿模与实体障碍

- 静止时人物与地图穿插 0；20 秒实时巡逻穿插 0。
- 实体装饰（木桶、树桩、边界树）内部不可行走；玩家撞木桶最近停在 0.50m。
- 全部 PASS（`audit/map_collision_tests.txt`）。

## 3. 场景问题

| 检查项 | 结果 |
|---|---|
| 缺失脚本/组件（编辑态+运行态） | 0 |
| 重复单例/重复组件 | 无；StoryDirector/GameFlow/GameMusic/CombatFeedback/MissionController/Hero/HUD 各 1 |
| AudioListener / Camera / EventSystem | 1 / 1 / 1 |
| 旧任务系统 | MissionController、IntelPickup、EscapePoint 均未启用，不与剧情冲突 |
| NavMesh | 6643 顶点 / 2965 三角形；运行时主角与 5 个敌人全部 onMesh |
| 建筑碰撞 | 22 栋建筑全部有 Collider，缺失 0 |
| 角色越界 | 0 |
| 道具/植被落地 | 无实际悬空；9 个 `Prop_BarrelLid` 与地形差 0.67m 是因为放在木桶上 |
| Build Settings | `界面.scene`、`RPG游戏.unity` 均在且启用 |
| 标签 | earth / Enemy / AttackBox 均已定义；编辑态找不到 AttackBox 实例是正常的（运行时才生成） |
| 场景保存状态 | 未保存、未改动，MD5 仍为 `0bba043a98a82fccf3f2009b386e31b9` |

## 4. 建议修复顺序（本次未实施）

1. **统一暂停仲裁（C1）**：给 `StoryDirector` 暴露 `IsGameplayPaused`，`PauseMenu.Toggle` 在剧情 UI 打开时禁止打开，或关闭时只在没有剧情暂停时才恢复 `timeScale`；更彻底的做法是引入一个 `PauseCoordinator` 统一管理暂停来源。
2. **剧情期间屏蔽非剧情输入（C2）**：`PotionInventory`、`SmokeAbility`、`InventoryUI` 的 `Update` 增加 `StoryDirector.Instance?.IsUiOpen == true` 或 `Time.timeScale <= 0` 判断；暂停/死亡时同样阻断。
3. **暂停时冻结主角攻击（C3）**：`HeroController.Update` 在 `PauseMenu.IsOpen` 或 `Time.timeScale <= 0` 时直接返回；或暂停菜单打开时禁用主角控制组件。
4. **死亡时禁止背包（C4）**：`InventoryUI.Update` 增加 `GameFlow.IsDeathPanelShown` 判断。
5. **字幕/对话不响应暂停按键（C5）**：`SubtitleOverlay`/`DialoguePanel` 在 `PauseMenu.IsOpen` 时不推进；过渡期间统一阻断所有交互。
6. **East_B 巡逻点**：把 `Patrol_1` 吸附到最近 NavMesh 点，或在场景里微调 1–2m。

## 5. 证据文件

- `C:\Users\22492\.claude\tools\rpg-1121\scene_static_audit.txt`
- `C:\Users\22492\.claude\tools\rpg-1121\tag_patrol_probe.txt`
- `C:\Users\22492\.claude\tools\rpg-1121\play_audit.txt`
- `C:\Users\22492\.claude\tools\rpg-1121\conflict_probe.txt`
- `C:\Users\22492\.claude\tools\rpg-1121\path_probe.txt`
- `C:\Users\22492\.claude\tools\rpg-1121\prop_terrain_audit.txt`
- `C:\Users\22492\.claude\tools\audit\map_collision_tests.txt`

## 6. 限制

- 工程未安装 Input System，Bridge 无法注入真实鼠标/键盘；按键类冲突由直接调用处理方法复现，代码路径可复现，但真人操作手感仍需实机确认。
- 本次只做检查，未修改任何生产脚本或场景。

## 7. 修复记录与收尾筛查（2026-11-21 实施）

### 7.1 修复方式

- 新增 `Assets/Script/Core/GameplayInputGate.cs`：统一判断 `SceneTransition.BlocksGameplay`、`Time.timeScale <= 0`、`StoryDirector.IsUiOpen`、`GameFlow.IsDeathPanelShown`。
- `StoryDirector` 新增 `IsGameplayPaused` 只读属性；`PauseMenu` 新增静态 `Instance`，关闭暂停时若剧情仍在暂停则保持 `timeScale=0`；场景过渡期间禁止打开暂停。
- `SubtitleOverlay`/`DialoguePanel` 在暂停菜单打开时不推进，避免同一次按键同时触发暂停与剧情推进。
- `HeroController.Update`/`TryDirectAttack`、`PotionInventory`、`SmokeAbility`、`InventoryUI`、`NpcInteractor`、`CluePickup`、`AssassinationController`、`EavesdropZone` 以及旧 `IntelPickup`/`EscapePoint` 均接入输入闸门；`TryUse`/`TryActivate`/`Toggle`/`CanInteract` 等公开入口也做同样判断，防止 HUD 按钮或测试直接调用时绕过。

### 7.2 巡逻点修复（两次）

1. 第一次将 `SwordGuard_East_B/Patrol_1` 吸附到最近 NavMesh 点：`(31.43,1.53,8.46) → (30.37,1.56,8.81)`，解决原路径 `PathInvalid`。
2. 全量碰撞筛查发现该路线在 `(29.44,1.45,7.37)` 处出现 0.05m 地形穿插（NavMesh 略低于地形）。经候选点实跑（30 秒）验证后，最终改为 `(27.00,1.60,8.00)`：
   - 与另外两点距离 4.64m / 2.61m，仍保持三角巡逻；
   - 30 秒巡逻最大穿地 `0.026m`、超阈值采样 `0`；
   - 精确路径 `PathComplete`，最近 NavMesh 距离 0.00m。
3. 场景已保存：MD5 `0bba043a98a82fccf3f2009b386e31b9` → `32166935763109c52690a079b26f6637`（首次吸附）→ `cb282353cb3ac6240bc765fda4ad8463`（最终点）。

### 7.3 修复后定点验证

| 项目 | 修复前 | 修复后 |
|---|---|---|
| 剧情字幕中关闭暂停菜单 | `timeScale=1`，剧情仍暂停 | `timeScale=0`，一致 |
| 剧情对话中打开背包 | 打开 | 拒绝打开 |
| 剧情对话中用药水 | 成功 | 拒绝，数量不变 |
| 剧情对话中放烟雾 | 成功 | 拒绝，烟雾区域 0 |
| 暂停中右键攻击 | `isAttack=True` 锁定 | `isAttack=False` |
| 死亡面板中打开背包 | 打开 | 拒绝打开 |
| 队长到井边 | 路径完整 | 路径完整，3 秒移动 5.35m 到 0.40m |
| 攻击期间水平位移（固定步长） | 0.85m（历史） | `0.000m` |

### 7.4 收尾筛查结果

- 编译：**0 错误 / 0 警告**。
- 玩法回归（最终场景状态复跑）：**11/11 PASS**（P1Stealth、P1GuardSenses、DirectMovement、DirectAttack、Assassination、PotionRuntime、Smoke、FullHud、PauseMenu、DeathFlow、SceneSwitch）。
- 剧情路径（最终场景状态复跑）：**kill / kill_after_truth / spare 全部 PASS**。
- 场景碰撞：人物静止穿插 0、20 秒巡逻穿插 0、实体装饰不可行走、玩家撞桶停在 0.50m，全部 PASS。
- 全系统审计：动画状态 Idle/Walk/Run/Sneak、攻击距离/冷却/攻击盒清理、敌人近战、Guard 状态机 Chase→Search→Patrol、5 个敌人 1 秒位移 1.16–1.76m 全部符合预期。
- 说明：`FullSystemsAudit` 记录的攻击期位移 0.30m 为传送后落地高度差；固定步长水平复测为 `0.000m`，不构成滑步回归。
- 最终编辑态：Play 已停止、场景无未保存改动、`timeScale` 已恢复为 1。

### 7.5 备份

`D:\Unity\backups\RPG_Project_2026-11-21_conflict-fix\`（修改前脚本、Scenes、progress.md，共 117 个文件）。
