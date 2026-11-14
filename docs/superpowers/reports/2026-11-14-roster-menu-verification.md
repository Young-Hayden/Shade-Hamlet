# 2026-11-14 第一章敌人阵容、过渡与主菜单验收

范围：删除弓手、新增 4 名剑兵、进入游戏的渐进过渡、主菜单电影化实景重做。未改第二章。

## 1. 敌人阵容

- `Archer` 场景实例已删除；弓手专项回归已从测试清单退役，脚本/资产暂留。
- 新增 4 名剑兵：
  - `SwordGuard_East_A/B`：东侧 `(24,3)`、`(29,7)` 附近，距出生点约 47/53m。
  - `SwordGuard_North_A/B`：北侧 `(16,28)`、`(-20,28)`，距出生点约 60/53m。
- 每名 3 个巡逻点，全部在 NavMesh 上；均无 `StoryTarget`，保留战斗、感知、血条、掉落。
- 北侧原巡逻区因 NavMesh 分区不连通（2 名只移动 0.08/0.14m），按网格搜索迁到已验证连通的开阔带，并剔除不可达点。
- **修复原地转圈**：4 个 `*_PatrolPoints` 原本被挂在各自剑兵子物体下，巡逻目标随人物移动/转向，形成绕圈。已全部移到独立的 `SwordGuardPatrolPoints` 根节点，世界坐标保持不变；6 秒实测 4 名剑兵位移 3.09–5.44m，巡逻点保持静止。
- 验证：
  - 静态 roster：`PASS archer removed`、`PASS sword guard count=4`、4 名组件/距离/巡逻点全 PASS、队长 `StoryTarget` 保留、故事路径完整。
  - 运行时：4/4 剑兵巡逻移动；击杀剑兵 `kills 0→1`，剧情阶段保持 `Investigate`。
  - 巡逻点结构：4/4 独立根节点；6 秒运动测试位移 3.09–5.44m，巡逻点稳定 4/4，不再原地旋转。
  - 20 秒人物与地图碰撞扫描：`uniquePairs=0`。

## 2. 场景过渡

- 新增 `SceneTransition`：0.7s 淡出、章节标题至少 1.0s、异步加载、0.8s 淡入；重复请求忽略，加载失败回退。
- 过渡期间冻结主角移动；`StoryDirector` 等淡入结束再播放开场字幕。
- 接入开始游戏、返回主菜单、重新挑战、旧入口。
- 实测：
  - 0.45s 黑场 alpha=1；
  - 1.0s 场景已切换但仍黑，`storyUi=False`；
  - 最终 `busy=False`、overlay 关闭、`storyUi=True`、`hero` 存在。
- 新菜单真实 `Menu_Start.onClick` 点击同样通过：persistent listeners=1，最终进入 `RPG游戏` 并显示剧情。

## 3. 主菜单

- 生成 `Assets/Art/UI/Menu/Menu_Background_01.png`（1920×1080、无角色/HUD）与 `Menu_Vignette.png`。
- 重建 `界面.scene` Canvas：实景背景、暗角、左侧金色装饰线、`SHADE HAMLET`、`影 村`、`第一章 · 回村的人`、标语、两个左对齐按钮。
- 首轮截图发现按钮文字与木质底对比不足，改为暖白加粗+阴影后复查。
- `MenuTests` 全 PASS：背景、暗角、标题 112px、副标题 34px、两个按钮及持久化接线、左对齐。
- 截图：`menu_before.png`、`menu_after.png`、`transition_loading.png`、`transition_arrived.png`、`sword_guards.png`。

## 4. 回归

首轮完整回归有两项失败：

- `P1GuardSensesTests`：测试按枚举顺序随机选中了新加的远处剑兵，视线被地形遮挡。已改为选择距离主角最近的剑兵（仍是剧情队长），属于测试夹具修正。
- `DeathFlowTests`：重开后的过渡尚未结束就再次点击返回主菜单，第二次请求被 `SceneTransition` 正确忽略，旧测试只等 1.5 秒。已把两个等待窗口改为 3.2 秒以等待过渡结束；生产逻辑未放宽。

修正后重点复测两项均 PASS。最终结果：

- 11 项玩法回归 **11/11 PASS**（弓手专项已退役）。
- 第一章三剧情路径 **kill / kill_after_truth / spare 全部 PASS**。
- 最终编辑态、`RPG游戏` 场景无未保存改动。



## 5. 指纹与回滚

- 场景 MD5：`RPG游戏.unity` `0bba043a98a82fccf3f2009b386e31b9`；`界面.scene` `07e7ec01f7131937a2fae4212618650f`。
- NavMesh MD5：`065bd3bb5b75d642c5601d1dac0a1a07`。
- `SceneTransition.cs` MD5：`4b6a57ffebd118a2cd351610f2f2a456`。
- 菜单背景/暗角：`214cc6ac65c7480158e65acfed09bcac` / `3da09d1f76069738ea7489a47a219c09`。
- 备份：`D:\Unity\backups\RPG_Project_2026-11-14_chapter1_roster_menu\`
- 设计：`docs/superpowers/specs/2026-11-14-chapter1-roster-menu-design.md`
- 计划：`docs/superpowers/plans/2026-11-14-chapter1-roster-menu.md`
- 证据：`docs/verification/2026-11-14-roster-menu/`

限制：菜单与美术仍建议用户实机看一次；删除弓手只删除场景实例，未清理未引用脚本资产。最终回归 **11/11 PASS**，三剧情路径全 PASS。

