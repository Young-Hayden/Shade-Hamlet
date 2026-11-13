# 子项目 D：情报、撤离与烟雾设计

日期：2026-11-10
状态：连续实施已批准

## 任务

- MissionController：CollectIntel→Escape→Completed，情报只登记一次，撤离必须有情报。
- IntelPickup：木箱处按 E 取得卷轴，领取后隐藏。
- EscapePoint：使用清理后的 Portal 副本和 Lightray，有情报时启用；按 E 完成并显示胜利面板。
- GameHUD 目标文案与交互提示随阶段变化；击杀计数保留但不再作为唯一通关条件。

## 烟雾

- Hero 新增 SmokeAbility，G 或动作栏按钮使用；默认持续 4 秒、冷却 12 秒、范围 3 米。
- FX_Smoke 只负责视觉，SmokeZone 负责真实遮挡。
- GuardSenses.EvaluateVisibility 在射线通过激活 SmokeZone 时返回 false，离开或到期后恢复。
- 动作栏增加第 4 槽 G 烟雾及冷却文本；控制提示同步更新。

## 验证

- 无情报不能撤离；取得卷轴后进入 Escape；撤离只完成一次并显示面板。
- 烟雾生成时视线被阻断，重复激活受冷却限制，到期后视线恢复。
- 普通攻击、刺杀、弓箭、药水、HUD、背包、暂停、死亡和场景往返回归。
- 分阶段保存场景、测试、截图、备份和验收报告。
