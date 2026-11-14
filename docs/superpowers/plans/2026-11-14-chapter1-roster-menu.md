# 第一章敌人阵容、场景过渡与主菜单实施计划

日期：2026-11-14
配套设计：`2026-11-14-chapter1-roster-menu-design.md`

## Task 1：备份与敌人阵容 RED 测试

- 备份 `RPG游戏.unity`、NavMesh、`界面.scene`、`CLick/GameFlow/HPScript/StoryDirector`。
- 写 `RosterTests.cs`：无 Archer、4 名 `SwordGuard_*`、组件齐全、无 `StoryTarget`、出生距离 ≥22m、巡逻点有效、队长保留、关键路径完整。
- 首轮结果：`FAIL archer removed`、`FAIL sword guard count=0`，确认测试有效。

## Task 2：删除弓手并加入 4 名剑兵

- 删除 `Archer` 场景实例。
- 以东/北两组巡逻区创建 4 个剑兵副本，移除 `StoryTarget`，保留战斗、感知、血条和掉落。
- 北侧初始巡逻区因 NavMesh 分区不连通，按网格搜索迁移到 `(16,28)` 与 `(-20,28)` 两处连通开阔带；剔除不可达巡逻点。
- 退役 `P1ArcherEventOnlyTests`。
- 验证：静态 roster 全 PASS；运行时 4/4 巡逻移动；击杀一名剑兵 `kills 0→1` 且剧情阶段保持 `Investigate`；20 秒地图穿插 0。

## Task 3：场景过渡

- 新增 `SceneTransition`：
  - 0.7s 淡出、1.0s 章节标题最短停留、0.8s 淡入；
  - `DontDestroyOnLoad`、重复请求忽略、加载失败回退；
  - `BlocksGameplay` 在过渡期间冻结主角移动。
- 接入 `CLick.ButtonClick/BackToStart`、`GameFlow.RestartGame/ReturnToMain`、`HPScript.EnterRPGGame`。
- `StoryDirector.InitializeStory` 等待淡入完成再播放开场字幕。
- 验证：黑场/标题/异步加载/字幕顺序正确；真实按钮点击端到端进入 RPG。

## Task 4：主菜单资产与重做

- 生成 `Menu_Background_01.png`（1920×1080，隐藏角色/HUD）和 `Menu_Vignette.png`。
- 重建 `界面.scene` Canvas：
  - 背景、暗角、左侧金色装饰线；
  - `SHADE HAMLET`、`影 村`、`第一章 · 回村的人`、标语；
  - `Menu_Start` / `Menu_Exit` 两个左对齐按钮，持久化事件接线。
- 首轮截图发现按钮文字与木框对比不足，改为暖白加粗加阴影后复查。
- 验证：`MenuTests` 7 项全 PASS；真实按钮点击进入过渡；截图归档。

## Task 5：回归与交付

- 运行更新后的回归清单（不含弓手专项）和第一章三路径。
- 归档菜单、过渡、剑兵截图和文本证据。
- 更新 `docs/progress.md` 与验收报告；Git 提交/推送等待用户另行确认。
