# 子项目 A：卫兵感知与实体掩体设计

日期：2026-11-10
状态：用户已批准连续实施

## 范围

实现 Patrol→Suspicious→Chase→Search→Patrol 感知循环，以及真正影响视线的围栏/岩石/土坡。Attack 继续由现有 EnemyController 近战逻辑负责。刺杀、弓箭、卷轴、撤离和烟雾不在本子项目内。

## 参数

- 视距 9 米、水平视角 110°。
- 持续可见 0.8 秒进入 Chase。
- Chase 丢失视线 2 秒进入 Search；Search 持续 4 秒后返回 Patrol。
- Suspicious 停止巡逻并朝玩家观察；警戒值在不可见时衰减。
- Chase 只追逐当前可见位置或最后目击位置，不透视读取玩家坐标。

## GuardSenses

- 独立组件，保存 State、Alertness、LastSeenPosition 和 Destination。
- 眼部 y+1.5 到玩家胸口 y+1.0 做 Physics.Raycast，QueryTriggerInteraction.Ignore。
- 命中玩家所属 HeroController 才视为可见；命中其它物体则遮挡。
- `EvaluateVisibility()` 公开供测试；`NotifyDamaged(Transform)` 受击直接进入 Chase。
- EnemyController 读取 State/Destination；Search 使用最后目击位置，Suspicious 停步观察。
- 死亡后禁用 GuardSenses。

## 场景掩体

新增 StealthPhysicalCover：少量围栏、岩石和土坡，按地形高度落地，保留可达通路。围栏用 BoxCollider，岩石/土坡用静态 MeshCollider，设置 NavigationStatic 并重烘焙 NavMesh。

修改前备份场景、EnemyController.cs 和 NavMesh.asset。烘焙失败或路径不可达则回滚。

## 验收

- 正面无遮挡约 0.8 秒进入 Chase；视角外不可见。
- 实体墙后不可见；移除遮挡后能发现。
- 丢失视线进入 Search，超时返回 Patrol。
- 受击立即 Chase，死亡后感知禁用。
- 烘焙后所有巡逻点 NavMeshPath 完整；直接移动、战斗、药水、HUD、背包、暂停和场景往返回归。
- 每阶段保存 docs/progress.md，工程无 Git 不提交。
