# 子项目 A 实施计划

1. 备份脚本、场景和 NavMesh。
2. 编写 GuardSensesTests：类型不存在时先失败；覆盖视锥、遮挡、发现、丢失、搜索、受击和死亡。
3. 实现 GuardSenses，并接入 EnemyController 的目标选择、受击与死亡。
4. 编译并运行感知测试，修复根因。
5. 创建少量实体掩体，碰撞落地、标记导航静态并重烘焙 NavMesh。
6. 检查所有巡逻点路径完整，截图并保存阶段进度。
8. 旧模式发现攻击盒有效距离小于原停止距离，已将主角 attackRange/attackExitRange 修正为 1.4/2.0，重跑旧模式敌人 100→0、直接攻击继续通过。
9. 全部验收完成。

回滚目录：`D:\Unity\backups\RPG_Project_2026-11-10_guard-senses\`。
