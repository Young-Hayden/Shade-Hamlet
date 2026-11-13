# P0 基线保护与自检验收

日期：2026-11-11

## 基线备份

`D:\Unity\backups\RPG_Project_2026-11-11_P0-baseline\`，包含当前脚本、RPG游戏、界面和 NavMesh。

## 实际运行结果

- 编译成功。
- WeaponAttachmentTests、BowPoseTests 通过。
- DirectMovementTests：walk=0.4、run=0.6、diagonal=0.4，通过。
- DirectAttackTests 通过。
- AssassinationTests 通过。
- ArcherEventOnlyTest：heroHP=95、命中后箭矢已销毁，通过。
- PotionRuntimeTests 通过。
- MissionTests 通过。
- SmokeTests 通过。
- FullHudTests 通过。
- PauseMenuTests 通过。
- DeathFlowTests 通过。
- SceneSwitchProbe：RPG游戏→界面→RPG游戏成功，返回后 Hero/Enemy 存在。

## 控制台

场景切换仍出现既有的 Progressive GPU 烘焙后端、地形树 Soft Occlusion、Unknown Missing Script 提示；未将其冒充为本轮新增问题，也未宣称清零。

## 红线结论

本轮未修改生产功能，无新增编译/运行时错误；基线证据已实际运行。后续阶段仍须按已批准方案中的自检、红线、持久化与人工限制要求执行。
