# RPG_Project 完整 UI/HUD 与菜单设计

日期：2026-11-09
状态：方案 A 已获用户批准

## 目标

在不新增玩法系统的前提下，把现有基础 HUD、背包和死亡面板扩展为风格统一、可操作、适配不同分辨率的完整界面，并补齐 Esc 暂停菜单与开始界面。

## 视觉规范与素材

- 深色棕底、金色边框、米白正文；玩家生命绿色、敌人生命红色、药水红色图标。
- `Button_05.png`：方形动作槽。
- `Menu_button_02_Little_On.png`：宽文字按钮。
- `Button_23.png`：齿轮菜单。
- `Button_04.png`：关闭 X，不再用作文字按钮。
- `Background_01.png`：中央面板；`Background_02.png`：HUD 底框。
- 继续使用 Universal Gold Icons 的药水/护甲图标与 Fantasy RPG UI 的武器图标。
- 不使用带缺失脚本的 GameDev.tv 功能预制体作为运行逻辑。

## 阶段 1：完整游戏 HUD

### 布局

- 左上：现有 PlayerPanel，生命与药水数量保留。
- 顶部中央：现有 TargetPanel，无目标/死亡时隐藏。
- 右上：阶段 1 保留现有退出按钮与目标提示；齿轮菜单在阶段 2 接入。
- 底部中央：三个 84×84 动作槽，间距 16；整体锚定屏幕底部中央，位置 y=28。
  1. `AttackSlot`：武器图标与“右键”提示，只表达现有攻击方式，不提供假技能按钮。
  2. `PotionSlot`：药水图标、Q 快捷键和数量；Button 点击调用 `PotionInventory.TryUse()`。
  3. `InventorySlot`：背包图标、I 快捷键；Button 点击调用 `InventoryUI.Toggle()`。
- 操作提示位于动作槽上方，开局显示，8 秒后用未缩放时间淡出；不阻挡射线点击。

### `GameHUD` 扩展

- 新增动作槽/提示引用与 `InventoryUI` 接线；PauseMenu 字段到阶段 2 再添加，避免提前引用未创建类型。
- 保持现有 `heroHealth`、`heroHealthText`、`potionCountText`、`objectiveText` 字段可用，避免破坏既有测试。
- 每帧只更新文本和显示状态；动作槽按钮不重复实现治疗或背包逻辑。

## 阶段 2：Esc 暂停菜单与界面一致性

### `PauseMenu.cs`

- GameSystems 上的独立组件，运行启动时创建隐藏 `PauseCanvas`。
- `Toggle()`：打开时显示面板并设 Time.timeScale=0；继续时隐藏并恢复 1。
- Esc 优先级：死亡面板存在则不打开；背包打开则先关闭背包；否则切换暂停菜单。
- 按钮：继续、重新开始、返回主界面、退出游戏。
- 重开/返回前先恢复 Time.timeScale=1，再调用 GameFlow 的公开场景流程方法。
- 退出在编辑器停止 Play，构建版 Application.Quit。
- 暂停面板显示固定操作说明，不依赖开局提示仍可见。

### 既有代码调整

- 删除 `HeroCamera` 的 Esc 直接退出逻辑，由 PauseMenu 独占 Esc。
- `GameFlow.RestartGame()`、`ReturnToMain()` 改为 public；新增 `IsDeathPanelShown` 只读属性。
- 死亡面板继续使用不暂停的设计，且按钮图片改为 Menu_button_02_Little_On。
- `InventoryUI` 增加 `Close()`；Esc 关闭时调用。保留实时背包，不与暂停菜单同时显示。
- 原 Order 下按钮文本改为“菜单”，点击事件改接 `PauseMenu.Toggle`；保留目标提示位置并检查不相交。

## 阶段 3：开始界面

- 修改 `界面.scene`，保留现有 CLick.ButtonClick。
- 添加游戏标题“RPG 冒险”、副标题“探索村庄，击败巡逻敌人”。
- 开始游戏按钮继续加载 RPG游戏；新增退出游戏按钮，编辑器/构建行为与 PauseMenu 一致。
- 新元素使用 1920×1080 参考分辨率和明确锚点；现有 Canvas 改为 Scale With Screen Size。
- 不添加设置页、存档页或背景音乐系统。

## 错误处理

- 缺少库存、背包或 PauseMenu 引用时，动作槽禁用对应交互但不影响战斗。
- 重复开关不重复创建 Canvas；场景重开后所有面板回到默认状态。
- 暂停/死亡/场景切换不能留下 Time.timeScale=0。
- 目标销毁、Hero 销毁和场景切换继续使用 Unity 对象判空。

## 验证

1. 每个阶段的新逻辑先写失败测试，再实现通过。
2. 动作槽数量、位置与文本正确；药水点击与 TryUse 结果一致；背包点击与 Toggle 一致。
3. Esc 优先级正确；暂停时 timeScale=0，继续/重开/返回均恢复 1；HeroCamera 不再直接退出。
4. 死亡时不能打开暂停菜单；死亡面板只创建一次。
5. 开始按钮进入 RPG游戏，退出按钮行为正确，开始界面新元素可见。
6. 默认 1197×602 与 1920×1080 下，动作槽、目标提示、菜单、背包和死亡面板无关键矩形重叠，截图可读。
7. 全量回归：相机、伤害反馈、药水、目标、背包、战斗、死亡、场景往返与磁盘持久化。
8. 真实键鼠仍由用户验收；自动测试直接调用按钮事件和对应方法。

## 分阶段保存与回滚

- 备份目录：`D:\Unity\backups\RPG_Project_2026-11-09_full-ui\`。
- 分别保存 HUD 前、暂停菜单前、开始界面修改前的场景与脚本。
- 每阶段更新 `docs/progress.md`；失败时停留在当前阶段，不继续叠加功能。
- 工程无 Git，不提交。
