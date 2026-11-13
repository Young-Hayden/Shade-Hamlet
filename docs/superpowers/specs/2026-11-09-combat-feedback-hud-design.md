# RPG_Project 战斗反馈、基础 HUD 与死亡重开设计

日期：2026-11-09
状态：方案 A 已获用户批准

## 目标

复用现有 SimpleFX、Fantasy RPG UI 和 GameDev.tv 美术资源，补齐“命中反馈—HUD 状态—主角死亡—重新挑战/返回主界面”的闭环，同时不改变现有战斗数值和操作规则。

## 方案边界

采用新写轻量逻辑、复用美术资源的方式。不直接依赖带缺失脚本引用的 GameDev.tv 功能预制体；可参考其布局和 TextMesh 样式。

非目标：药水、背包、弓箭手、武器切换、主动仇恨、新关卡、完整 Esc 菜单、对象池、存档和音效扩展。

## 组件与职责

### `DamagePopup.cs`

- 世界空间 TextMesh 伤害数字。
- 初始化接口接收伤害值、世界坐标和颜色类别。
- 在约 0.8 秒内上浮并淡出，随后销毁。
- 不读取战斗组件，不决定伤害是否命中。

### `CombatFeedback.cs`

- 场景级服务，持有命中粒子、击杀粒子和伤害数字的默认样式。
- 接口：`ShowDamage(Vector3 position, float damage, bool playerWasHit)`、`PlayHit(Vector3 position)`、`PlayKill(Vector3 position)`。
- 每次实际扣血只调用一次；粒子播放完毕后销毁，避免场景残留。
- 资源未接线或为空时跳过粒子/特效并记录有限诊断，不让反馈失败影响扣血。

### `GameHUD.cs`

- 屏幕空间 Overlay Canvas，Canvas Scaler 使用 Scale With Screen Size，参考分辨率 1920×1080。
- 左上：主角生命条与 `当前值/最大值` 文本。
- 顶部中央：选中敌人名称与生命条；`HeroController.tarEnemy` 为空、目标死亡或销毁时隐藏。
- 每帧只更新显示值，不修改战斗状态。
- 现有头顶血条保留。
- 现有“退出游戏”按钮移至右上角，避免与主角 HUD 重叠；其原有点击事件不变。

### `GameFlow.cs`

- 持有 HUD 引用，并提供死亡入口 `OnHeroDeath()`。
- 主角死亡时只创建一次结算面板，使用 Fantasy RPG UI 的背景和按钮图片。
- “重新挑战”调用 `SceneManager.LoadScene` 加载当前 RPG 场景；“返回主界面”加载 `界面`。
- 不修改 `Time.timeScale`，保证死亡动画和延迟销毁继续执行。
- 重复死亡信号不会重复创建面板；场景切换时不保留旧面板引用。

## 战斗接线

- `HeroController` 和 `EnemyController` 增加只读最大生命属性，供 HUD 计算比例。
- 两者在 `Hurt` 实际扣血且伤害大于 0 后调用 `CombatFeedback.ShowDamage` 与 `PlayHit`。
- 敌人 `Die` 调用 `PlayKill`；主角 `Die` 调用 `GameFlow.OnHeroDeath`。
- 不改 ATK、HP、攻击范围、攻击间隔、追击、寻路或动画事件。

## 场景设置与保存

编辑器一次性脚本创建并接线 GameFlow/CombatFeedback/GameHUD，资源通过 AssetDatabase 显式绑定，不依赖运行时按路径加载。

分三个阶段保存：

1. 伤害数字与命中/击杀粒子：代码和场景服务接线，备份并保存。
2. 基础 HUD 与退出按钮移位：备份并保存，截图验收。
3. 死亡重开面板：备份并保存，实测两个按钮对应流程。

每阶段更新 `docs/progress.md`。工程无 Git，不提交。

## 错误处理

- 场景服务不存在时，战斗仍须正常扣血；反馈功能静默降级，并在测试环境报告缺失。
- 目标销毁后 HUD 使用 Unity 对象判空并立即隐藏。
- 重复死亡不重复生成结算面板。
- 重新开始后生命、敌人、HUD 和死亡面板状态全部由新场景重建。
- 不新增 Input System 依赖；真实鼠标点击仍需人工验收，脚本可直接调用按钮事件验证场景切换。

## 验证

1. 所有新逻辑先写失败测试，再实现并运行通过。
2. 实际伤害产生一个数值正确的伤害数字；0 伤害不产生；约 0.8 秒后自动清理。
3. 命中/击杀粒子只生成一次并自动销毁；资源缺失不影响战斗。
4. HUD 主角生命与数值文本正确；选中敌人时显示，目标死亡/销毁后隐藏。
5. 新 HUD 与退出按钮不重叠；默认 Game View 截图检查文本、血条和布局。
6. 主角死亡只出现一个面板；重新挑战后 Hero/Enemy 生命均为 100，面板消失；返回主界面成功。
7. 完整回归：追击战斗、双方死亡、相机测试、场景往返和编译。
8. 控制台既有树 shader/烘焙后端/Unknown Missing Script 问题单独记录，不冒充本轮已修复。

## 回滚

- 本阶段前完整备份 `Assets/Script` 与 `RPG游戏.unity`。
- 三个小阶段分别保留场景备份。
- 新增脚本可单独删除；战斗接线恢复前需同步还原 HeroController/EnemyController，避免留下无效调用。
