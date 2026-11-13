# 人物与 NPC 动作优化验收

日期：2026-11-10

## 弓箭手姿态

- 根因：弓原挂右手，且丢失 Equipped Bow 的局部偏移与旋转，呈右手长柄武器姿态。
- 修复：改为左手持弓，恢复 `localPosition=(0.269,-0.02,0.8)`、`localRotation=(0,90,0)`，发射点同步到左手。
- `BOW POSE TESTS PASS`，`archer-bow-attack.png` 可见左手持弓、右手拉弦。

## 动作状态优化

- HeroDirectMovement 使用 CrossFade，并按 requestedState 去重，避免过渡期间每帧重启；攻击打断后允许重新进入移动状态。
- HeroController/EnemyController 的 walk 在 isAttack 时强制关闭，避免移动和攻击叠加。
- RangedAttack 开始攻击时显式关闭 walk，保证持弓/拉弓姿态。

## 验证

- `HUMANOID RUN STATE PLAY PASS`：奔跑过渡正常完成。
- DirectAttackTests、AssassinationTests 通过。
- `ISOLATED BOW EVENT PASS heroHP=95 arrowsAfterHit=0`：弓动画事件命中一次。
- BowPoseTests、WeaponAttachmentTests 通过。
- 编译通过，挂点持久化已核验。

备份：`D:\Unity\backups\RPG_Project_2026-11-10_bow-pose\`。
