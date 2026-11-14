# 2026-11-14 《影村》P2 四项修复验收报告

范围：头顶血条过宽、潜行步频不匹配、单手细剑使用双手剑动画、弓箭手近身不后撤。

## 修复内容

1. **血条**：三个世界空间 `HPScript` 根节点统一缩小，主角血条世界宽度由 `1.536m` 降为 `0.768m`，内部布局不变。
2. **潜行动画**：`heroController` 新增 `HumanoidSneak`，复用走路剪辑但速度为 `0.55`（2.2/4.0）。`HeroDirectMovement` 在潜行移动时进入该状态，并避免 `walk` 布尔把状态机切回普通走路。
3. **单手剑攻击**：从工程已有的 `RPG-Character@Sword-Attack-R6.FBX` 生成 `Assets/Animation/hero/Sword-Attack-R6.anim`，替换攻击状态 motion；保留 `AttackEnemyEvent`，时点按原动画比例设为新剪辑的 40%（0.32 秒）。
4. **弓箭手后撤**：`RangedAttack` 新增 `TryRetreat(Vector3)`。目标小于 3m 时向 NavMesh 上可完整到达的位置后撤，4.5m 后恢复射击；后撤时面向玩家。找不到完整路径时保持原行为，不穿墙。

## RED → GREEN 证据

修复前（`p2_visual_tests.txt` 首轮、`p2_retreat_tests.txt` 首轮）：

- `FAIL health bar world width=1.535999`
- `FAIL sneak state exists=False speed=-1`
- `FAIL attack motion=2Hand-Sword-Attack5 damageEvents=1`
- `FAIL archer retreat: RangedAttack.TryRetreat(Vector3) missing`

修复后静态测试：

- `PASS health bar world width=0.7679996`
- `PASS sneak state exists=True speed=0.55`
- `PASS attack motion=Sword-Attack-R6 damageEvents=1`

运行时测试：

- `PASS runtime sneak state`：当前状态 hash 等于 `HumanoidSneak`，stateSpeed=0.55。
- `PASS single-hand attack deals damage`：实际播放 `Sword-Attack-R6`，弓箭手 HP 100 → 80，伤害事件未丢失。
- `PASS runtime health width=0.7680006`。
- `PASS archer retreats from close target`：直接调用后距离 0.934m → 1.717m。
- `PASS retreat then ranged damage`：真实感知与战斗流程中，弓箭手后撤至 4.757m，随后箭生成，玩家 HP 100 → 80。

测试脚本与日志：`C:\Users\22492\.claude\tools\audit\P2*`，截图 `p2_healthbar_attack.png`。

## 全量回归

- 既有 12 项玩法回归 **12/12 PASS**，包括潜行、感知、弓箭动画事件、移动、普通攻击、刺杀、药水、烟雾、HUD、暂停、死亡与场景切换。原始日志：`C:\Users\22492\.claude\tools\audit\regression_p2.log`。
- 第一章三条剧情路径 **kill / kill_after_truth / spare 全部 PASS**。原始日志：`C:\Users\22492\.claude\tools\audit\story_paths_p2.log`。
- 最终编辑器状态：非 Play、`RPG游戏` 场景无未保存改动。


## 文件与回滚

修改：

- `Assets/Script/HeroDirectMovement.cs`
- `Assets/Script/RangedAttack.cs`
- `Assets/Animation/hero/heroController.controller`
- `Assets/Scenes/RPG游戏.unity`（三个血条缩放）

新增：

- `Assets/Animation/hero/Sword-Attack-R6.anim`（含伤害事件）

备份：`D:\Unity\backups\RPG_Project_2026-11-14_p2-fixes\`

MD5：

- 场景 `987173be98efce82112690cc7c130765`
- `HeroDirectMovement.cs` `279474acdbbd20f2ffd1f7615c534d0a`
- `RangedAttack.cs` `d7f088fb1d38704c1de32144292f2235`
- `heroController.controller` `9b657d03ff9b92c93f7daa3ff6a743b9`
- `Sword-Attack-R6.anim` `5031493fab37464bec4b65c07eab9a99`

限制：验证为脚本驱动与截图目检；潜行、攻击和鼠标视角的实际手感仍需用户实机试玩。
