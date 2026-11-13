# 子项目 C：弓箭卫兵与可躲避箭矢设计

日期：2026-11-10
状态：连续实施已批准

## 范围

新增一名远程卫兵，复用 EnemyController 的生命/受击/死亡/掉落和 GuardSenses 感知。弓、箭使用现有干净模型，原 RPG 缺失脚本不接入。

## RangedAttack

- 由 EnemyController.DoAttack 转发，参数：range=8、exitRange=10、interval=2 秒、chaseStoppingDistance=6.5。
- Chase 时接近到停止距离，进入射程站定，播放现有弓攻击动画；EnemyAnimator 动画事件调用 Fire。
- Fire 从 Attack1pos 发射一支箭，方向取发射时玩家位置，之后不追踪。
- 原近战敌人没有 RangedAttack，保持原行为。

## ArrowProjectile

- 使用 `Simple Arrow.prefab` 创建干净 `StealthArrow.prefab`。
- 每帧按固定方向前进，Raycast 检查路径；忽略发射者层级和 Trigger。
- 命中 HeroController 调用一次 Hurt，命中环境销毁，4 秒超时销毁。
- 玩家可在发射后移动躲避；不生成近战 AttackBox。

## 动画与场景

- 复制弓 FBX 动画为 `2Hand-Bow-Attack1.anim`，在 0.466 秒添加 `AttackHerroEvent`。
- 新建 `archerController.overrideController`，仅覆盖攻击 clip，巡逻/死亡沿用。
- 复制现有敌人为 Archer，换弓模型、控制器、RangedAttack 和 12 米感知；保留血条/掉落/巡逻点。
- 场景目标从 1 改为 2，HUD 文案改为通用“击败敌人”。

## 验证

- 远程攻击动画事件生成一支箭，不生成近战盒。
- 静止玩家被命中一次；移动躲避不扣血；命中墙或超时自动销毁。
- 近战回归、刺杀、药水、HUD、暂停、死亡和场景切换通过。
- 每阶段保存进度与截图，失败不强行接受。
