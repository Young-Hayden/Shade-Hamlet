# 子项目 B：细剑身份与刺杀验收

日期：2026-11-10

## 已交付

- SM_Wep_Rapier_01 已挂到现有 Attack1pos，原剑 Renderer 禁用但引用/动画事件保留。
- 动作栏与背包使用 T_Dagger_01_Nobg，背包武器文本为“细剑”。
- `AssassinationController`：距离≤1.4、背后点积≤-0.5、面向目标、GuardState.Patrol 才允许 E 刺杀。
- 使用现有剑击动画，动画事件再次校验并按 MaxHP 结算一次；失效时回退普通攻击盒。
- GameHUD 仅条件满足时显示“E 刺杀”。

## 验证

- `ASSASSINATION TESTS PASS`：正面拒绝、超距拒绝、未面向拒绝、Chase 拒绝；背后 Patrol 提示可见并死亡一次，重复执行拒绝。
- DirectAttackTests、FullHudTests、InventoryUITests、DeathFlowTests、SceneSwitchProbe 均通过。
- `assassination-ready.png` 已验证细剑与 E 提示实际可见；初版同帧截图被攻击隐藏，已拆分复拍。

## 限制

- 没有专门处决动画或兜帽角色，当前是细剑潜行者原型。
- 弓箭、卷轴、撤离和烟雾未实现。
- 既有控制台告警未清零，真实按键仍需用户试玩。

## 归档

- 截图：`docs/verification/2026-11-10-assassination/`
- 备份：`D:\Unity\backups\RPG_Project_2026-11-10_assassination\`
- 进度：`docs/progress.md`
