# RPG_Project 完整 UI/HUD 验收

日期：2026-11-09

## 已交付

### 游戏 HUD

- 左上主角生命、药水数量；顶部中央目标生命；右上目标提示。
- 底部中央三个动作槽：右键攻击提示、Q 药水按钮、I 背包按钮。
- 开局操作提示使用未缩放时间，在 8 秒后完成淡出并隐藏。
- 原退出按钮改为 60×60 齿轮菜单按钮，原事件替换为 PauseMenu.Toggle。

### Esc 暂停菜单

- 继续、重新开始、返回主界面、退出游戏。
- 打开时 Time.timeScale=0；继续/重开/返回/退出均恢复 1。
- Esc 优先关闭打开的背包，再切换暂停菜单。
- 死亡面板存在时，无论 Esc 还是菜单按钮都不能打开暂停菜单。
- HeroCamera 旧的 Esc 直接退出已删除。
- 背包新增 Button_04 关闭按钮与 `Close()`；背包仍保持实时。
- 死亡面板文字按钮改用 Menu_button_02_Little_On，纠正此前把关闭 X 当按钮底图的问题。

### 开始界面

- 新增 `RPG 冒险` 标题和“探索村庄，击败巡逻敌人”副标题。
- 开始游戏沿用 CLick.ButtonClick；新增退出游戏并接 CLick.ExitGame。
- Canvas 改为 Scale With Screen Size，参考 1920×1080。

## 红—绿测试

| 功能 | 实现前 | 实现后 |
|---|---|---|
| 动作栏 | `full HUD action bar members missing` | `FULL HUD TESTS PASS` |
| 暂停菜单 | `PauseMenu type missing` | `PAUSE MENU TESTS PASS` |
| 开始界面 | `start menu title/exit missing` | `START MENU TESTS PASS` |
| 开始界面退出 | 新增按钮不存在 | `START EXIT BUTTON PASS`，实际停止 Play |

## 功能与布局证据

- 动作栏恰好三个槽；动作栏与主角 HUD/菜单按钮矩形不相交。
- 药水按钮执行真实 TryUse：50 HP→80、库存 1→0。
- 背包按钮执行真实 Toggle，开关均正确。
- 操作提示到期自动隐藏。
- 暂停打开/继续、场景齿轮按钮、Esc 关闭背包优先、死亡保护全部通过。
- 开始按钮成功进入 RPG游戏；退出按钮停止 Play。
- 默认 1197×602 与 1920×1080 下 FullHudTests 均通过；Full HD 截图尺寸经 PIL 确认为 1920×1080。
- Full HD 下 InventoryUITests、PauseMenuTests 也通过；背包/暂停截图在稳定状态下单独重拍，避免测试切帧造成状态混杂。

## 最终回归

- 编译成功，CameraControlTests 通过。
- PotionRuntimeTests、FeedbackRuntimeTests、HudRuntimeTests、ObjectiveHudTests 均再次通过。
- 战斗：敌人 100→0 并销毁，主角正常受伤。
- 死亡：HP=0、尸体不移动、2 秒销毁。
- DeathFlowTests：重开生命/药水/目标/背包复位，返回主界面正常。
- SceneSwitchProbe：RPG游戏→界面→RPG游戏成功。

## 磁盘持久化

`FULL UI PERSISTENCE PASS`：

- RPG 场景 GameSystems 的 GameHUD/PauseMenu/InventoryUI/GameFlow 及全部 Sprite 引用存在。
- Order 菜单按钮为 Button_23、60×60、scale=1，唯一持久监听为 PauseMenu.Toggle。
- 开始界面标题/副标题/退出按钮存在，Canvas 设置正确。
- StartExitButton 唯一监听为 CLick.ExitGame；原开始按钮仍为 CLick.ButtonClick。
- 重载时 Time.timeScale=1。

## 限制与遗留

- 自动测试调用按钮事件/方法，未进行真人鼠标和键盘操作；最终手感仍需用户试玩。
- 运行时代码生成 UI，编辑态只保留服务组件和资源引用，不显示完整运行界面。
- 既有烘焙后端、地形树 Soft Occlusion、场景切换 Unknown Missing Script 提示仍单独保留，未宣称本轮修复。
- 第三批弓箭手、武器切换与短关卡未实施。

## 归档

- 截图：`docs/verification/2026-11-09-full-ui/`（默认与 1920 两套）。
- 备份：`D:\Unity\backups\RPG_Project_2026-11-09_full-ui\`。
- 进度：`docs/progress.md`。
