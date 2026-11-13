# 子项目 C：弓箭卫兵与箭矢验收

日期：2026-11-10

## 已交付

- Archer 场景实例、Simple Bow 模型、12 米感知、RangedAttack（8/10 米射程、2 秒间隔、6.5 米停止距离）。
- `2Hand-Bow-Attack1.anim` 含 AttackHerroEvent，`archerController.overrideController` 仅覆盖攻击动画。
- `StealthArrow.prefab` 与 `ArrowProjectile`：固定方向飞行、命中一次、撞墙销毁、4 秒超时、移动可躲避。
- EnemyController/Animator 复用近战与远程分支；其它近战敌人保持原行为。
- 场景 KillTarget=2，HUD 改为通用击败敌人目标。

## 验证

- `ARROW STATIONARY HIT PASS`：静止玩家 HP 100→95。
- `ARROW DODGE PASS`：移动躲避不扣血。
- `ARROW TIMEOUT CLEANUP PASS`：超时自动销毁。
- `ARROW WALL COLLISION PASS`：实体墙后命中被阻断。
- `ISOLATED BOW EVENT PASS heroHP=95 arrowsAfterHit=0`：弓动画事件确实发射一次并命中；不是要求命中后箭仍存在。
- ObjectiveHudTests（2 人完成）、DirectAttackTests、AssassinationTests、DeathFlowTests、SceneSwitchProbe 均通过。
- `ARCHER PERSISTENCE PASS`：箭矢引用、发射点和覆盖控制器重载后存在。

## 测试修正

- 初版把“命中后箭已销毁”误判为未发射，已改为验证实际命中。
- 初版撞墙位置在玩家身后，已改为发射者与玩家之间后通过。
- 临时发射/状态日志已移除，生产代码无调试刷屏。

## 限制

- 卷轴、撤离和烟雾未实现。
- 既有控制台告警未清零，真实键鼠仍需用户试玩。

## 归档

- 截图：`docs/verification/2026-11-10-archer/`
- 备份：`D:\Unity\backups\RPG_Project_2026-11-10_archer\`
- 进度：`docs/progress.md`
