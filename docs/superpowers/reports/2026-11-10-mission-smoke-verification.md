# 子项目 D：卷轴任务、撤离与烟雾验收

日期：2026-11-10

## 已交付

- `MissionController`：CollectIntel→Escape→Completed；情报只登记一次，未取情报不能撤离，撤离只完成一次并显示胜利面板。
- IntelCrate：E 取得卷轴并隐藏；MissionEscapePortal + Lightray：有情报后可按 E 撤离。
- `SmokeAbility`：G 或第四动作槽，持续 4 秒、冷却 12 秒、范围 3 米。
- `SmokeZone`：按线段距离判断视线是否穿过烟雾；GuardSenses 激活时不可见，到期恢复。
- HUD 第四槽与冷却文本、任务目标/交互提示同步；普通刺杀优先显示“E 刺杀”。

## 验证

- `MISSION TESTS PASS`：无情报撤离失败，取情报后进入 Escape，撤离完成一次并显示胜利面板。
- `SMOKE TESTS PASS`：烟雾遮挡、重复激活冷却、到期恢复、动作按钮均通过。
- `OBJECTIVE HUD TESTS PASS`：任务文本与完成面板在下一帧正确更新；初版同帧读取导致假失败，已拆分阶段。
- FullHudTests（四槽）、DirectAttackTests、AssassinationTests、DeathFlowTests（含任务复位）、SceneSwitchProbe、CoverNavTests 均通过。
- `MISSION SMOKE PERSISTENCE PASS`：任务重置、Portal 清理后无缺失脚本、Smoke prefab 与 Lightray 引用保留。
- `smoke-active.png` 已确认烟雾画面实际可见，四槽显示 G/冷却秒数。

## 限制

- 没有兜帽角色、专门刺杀/攀爬动画或大型开放世界；当前为村庄潜行垂直切片。
- 真实键鼠、操作手感和夜景明暗仍需用户试玩。
- 既有烘焙后端、地形树和场景切换 Unknown Missing Script 提示未清零。

## 归档

- 截图：`docs/verification/2026-11-10-mission-smoke/`
- 备份：`D:\Unity\backups\RPG_Project_2026-11-10_mission-smoke\`
- 进度：`docs/progress.md`
