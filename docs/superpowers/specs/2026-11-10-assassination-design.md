# 子项目 B：细剑身份与背后刺杀设计

日期：2026-11-10
状态：连续实施已批准，刺杀规则采用 A

## 范围

替换玩家细剑模型与动作栏/背包匕首图标，增加 E 刺杀提示与执行。没有处决动画，复用现有剑击，在动画事件时结算。

## 刺杀条件

必须同时满足：主角未死亡且未在攻击中、目标存活、距离≤1.4米、玩家位于目标背后（enemy.forward 与 enemy→hero 点积≤-0.5）、玩家面向目标、GuardSenses.State=Patrol。没有 GuardSenses 不允许一击刺杀。

TryAssassinate 返回 bool，不满足条件不播放特殊攻击、不掉血。动画事件时再次校验；若目标已死亡、离开条件或进入警觉，则回退为普通攻击盒，避免延迟必杀。

## 组件

- `AssassinationController`：CanAssassinate(out EnemyController)、TryAssassinate()、TryConsumeTarget(out EnemyController)。
- HeroController 继续控制 isAttack、Animator attack 与 AttackEnd；刺杀使用同一动画时间。
- HeroAnimator.AttackEnemyEvent 优先尝试刺杀目标；成功时按 MaxHP 结算一次并返回，否则沿用原攻击盒。
- GameHUD 增加 `assassinationPrompt`，仅条件满足时显示“E 刺杀”。

## 身份素材

- 细剑实例挂到现有 Attack1pos，原剑 Renderer 禁用但保留引用与动画事件。
- GameHUD.weaponSprite 与 InventoryUI.weaponSprite 改为 `T_Dagger_01_Nobg.png`。
- 背包“当前武器”文本改为“细剑”。不宣称已有兜帽刺客模型或专门处决动画。

## 验证

- 背后未察觉可刺杀，动画事件后敌人死亡且只结算一次。
- 正面、超距、已 Chase、没有面向目标均拒绝。
- 提示只在可刺杀时出现。
- 刺杀掉药/击杀计数仍走原逻辑，普通直接攻击不回归。
- 场景保存、细剑截图、全量回归、进度和验收报告齐全。
