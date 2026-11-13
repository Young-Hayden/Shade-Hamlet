# 子项目 A：卫兵感知与实体掩体验收

日期：2026-11-10

## 已交付

- `GuardSenses.cs`：Patrol/Suspicious/Chase/Search，9 米视距、110°视角、0.8 秒发现、2 秒丢失、4 秒搜索。
- 视线使用 Physics.Raycast 与 Trigger Ignore；只认玩家碰撞，环境实体可遮挡。
- Chase/Search 只追踪可见位置或最后目击位置；Suspicious 停步观察；Search 后巡逻点正确复位。
- EnemyController 复用感知目标，受击强制 Chase，死亡关闭感知；原生命、攻击、掉落保留。
- StealthPhysicalCover：围栏 BoxCollider、岩石/土坡 MeshCollider、NavigationStatic；NavMesh 已重烘焙。

## 验证

- `GUARD SENSES TESTS PASS`：正面/背后、实体墙遮挡、发现、Search、Patrol 复位、受击、死亡均通过。
- `PHYSICAL COVER AND NAV TESTS PASS`：全部巡逻点 NavMeshPath 完整。
- DirectAttackTests、PotionRuntimeTests、FullHudTests、PauseMenuTests、DeathFlowTests、SceneSwitchProbe 均通过。
- 旧模式首次暴露主角在 1.8 米停下但攻击盒可靠命中更近，出现连续空挥并死亡；已将 attackRange/exitRange 修正为 1.4/2.0。修正后旧模式敌人 100→0 并销毁、主角 80 HP，DirectAttackTests 继续通过。

## 限制

- 刺杀、弓箭、卷轴、撤离和烟雾尚未实现。
- 真实键鼠手感需用户试玩。
- 既有烘焙后端、地形树和场景切换 Unknown Missing Script 提示仍存在，未宣称清零。

## 归档

- 截图：`docs/verification/2026-11-10-guard-senses/guard-cover-wide.png`
- 备份：`D:\Unity\backups\RPG_Project_2026-11-10_guard-senses\`
- 进度：`docs/progress.md`
