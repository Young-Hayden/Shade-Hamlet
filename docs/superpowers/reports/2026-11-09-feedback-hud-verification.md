# RPG_Project 战斗反馈、HUD 与死亡流程验收

日期：2026-11-09

## 已交付

- `RpgUI.cs`：共享 Canvas/Image/Text/Slider/Button 构建。
- `DamagePopup.cs`：世界空间伤害数字，上浮、淡出、0.85 秒销毁。
- `CombatFeedback.cs`：实际伤害弹字、命中粒子、击杀粒子，资源为空不影响战斗。
- `GameHUD.cs`：左上主角生命、顶部中央选中目标生命，Scale With Screen Size 1920×1080。
- `GameFlow.cs`：死亡面板、重新挑战、返回主界面，重复死亡不重复建面板。
- 场景 GameSystems 持有三个服务组件；原退出按钮移至右上，原点击事件保留。

## 红—绿验证

| 功能 | 修复/实现前 | 实现后 |
|---|---|---|
| 伤害反馈 | `DamagePopup/CombatFeedback types are missing` | `FEEDBACK RUNTIME TESTS PASS` |
| HUD | `GameHUD type is missing` | `HUD RUNTIME TESTS PASS` |
| 死亡流程 | `GameFlow type is missing` | `DEATH FLOW TESTS PASS` |
| 弹字朝向 | dot=0.546，镜像失败 | dot=1，`DAMAGE POPUP ORIENTATION PASS` |

## 功能证据

### 反馈

- 0 伤害不生成弹字；20 点伤害生成一个文本为 20 的弹字。
- 弹字到期自动清理；命中特效只生成一次并在 2 秒后清理。
- `Hurt(0)` 不改变 HP、不产生反馈；`Hurt(20)` 正确扣血并弹字。
- 销毁反馈服务后继续扣血仍正确，无空引用。

### HUD

- 初始生命条=1、文本 `100 / 100`，无目标时目标框隐藏。
- 主角受伤后生命条=0.8、文本 `80 / 100`。
- 选中敌人后目标框显示；敌人死亡后隐藏。
- Canvas Scaler 与 1920×1080 参考分辨率断言通过。
- playerPanel 与退出按钮的屏幕矩形不相交。
- `hud-target.png` 可见顶部 Enemy 血条、左上主角 HUD 和正向伤害数字 20。

### 死亡流程

- 连续两次致命伤害只生成一个 DeathCanvas，Time.timeScale 保持 1。
- 调用重新挑战按钮后返回 RPG游戏，新 Hero/Enemy HP 均为 100，DeathCanvas 为 0。
- 再次死亡并调用返回主界面后，活动场景为 `界面`。
- `death-panel-only.png` 为不发生场景切换的单独截图，确认标题与两个按钮可见。

## 最终回归

- 编译成功，CameraControlTests 再次通过。
- 战斗：敌人 100→80→60→40→20→0 并销毁，主角 100→80。
- 主角死亡：HP=0、尸体位移 0.000、死亡动画播放、2 秒销毁。
- 场景往返：RPG游戏→界面→RPG游戏成功，返回后 Hero/Enemy 存在。
- 停止 Play 并从磁盘重载后：`SYSTEMS PERSISTENCE PASS`；三个组件、全部素材引用和退出按钮右上布局均保留。

## 实施中发现并修正的问题

1. GameDev.tv `Level Up.prefab` 的 SH_VFX 脚本缺失，会在击杀时产生 Console Error；改用无脚本缺失的 `SimpleFX/FX_Fireworks_Yellow_Small.prefab`。
2. 伤害弹字原朝向导致镜像；改为直接使用相机旋转，并以朝向测试证明修复。
3. 初版截图与场景切换/目标死亡同帧执行，截到了后续状态；这些图不作为验收证据，已用延迟截图重新验证。

## 限制与遗留

- 按钮通过 `Button.onClick.Invoke()` 验证，不等于真人鼠标点击；项目仍未新增 Input System 依赖。
- 控制台仍有既有的 Progressive GPU 烘焙后端、地形树 Soft Occlusion、场景切换 Unknown Missing Script 提示；本轮未宣称修复这些旧问题。
- 新 HUD/弹字在运行时由代码生成，编辑态不会显示完整 UI；这是当前实现方式，不是场景丢失。
- 未实现对象池；当前低频率 Instantiate/Destroy 已验证生命周期，但大量敌人时需重新评估。

## 归档

- 截图：`docs/verification/2026-11-09/`
- 备份：`D:\Unity\backups\RPG_Project_2026-11-09_feedback\`
- 进度：`docs/progress.md`
