# RPG_Project 工作进度

最近更新：2026-11-12

## 已完成并保存

- [x] 战斗核心修复：追击、攻击距离、Root Motion、刚体/死亡、仇恨、攻击盒去重等。
  - 原始代码备份：`D:\Unity\backups\RPG_Project_2026-11-08_pre-fix\`
  - 报告：`优化报告_2026-11-08.md`
- [x] 阶段 1：相机滚轮缩放（4～12 米）、中键旋转、简单遮挡检测。
  - 修改：`Assets/Script/HeroCamera.cs`
  - 回滚：`D:\Unity\backups\RPG_Project_2026-11-08_presentation\HeroCamera.before.cs`
  - 验证：修复前测试失败，修复后编译/单元测试/Play 内测试通过。
- [x] 阶段 2：两条血条降低并缩小 20%，保存后从磁盘重载验证。
  - 场景回滚：`RPG游戏.before-hp.unity`
  - 关键教训：RectTransform 高度必须写 `anchoredPosition`，只写 localPosition 会在重载后回退。
- [x] 阶段 3：环境光 1.12、方向光 1.08，保存并截图对比。
  - 场景回滚：`RPG游戏.before-lighting.unity`
- [x] 最终回归：战斗、主角死亡、场景往返、编译和相机测试均完成。
- [x] 现有素材驱动的玩法/UI 优化建议已整理，未实施新玩法。

## 交付文档

- 设计：`docs/superpowers/specs/2026-11-08-presentation-polish-design.md`
- 计划：`docs/superpowers/plans/2026-11-08-presentation-polish.md`
- 验收：`docs/superpowers/reports/2026-11-08-presentation-verification.md`
- 截图：`docs/verification/2026-11-08/`
- 玩法/UI 建议：`docs/2026-11-08-gameplay-ui-opportunities.md`

## 2026-11-09 第一批实施进度

- [x] 阶段 1（伤害反馈）已保存并验证：伤害弹字、命中/击杀粒子、实际伤害接线、自动清理、服务缺失降级均通过。
  - 新增：`RpgUI.cs`、`DamagePopup.cs`、`CombatFeedback.cs`
  - 场景：GameSystems/CombatFeedback 已绑定并保存。
  - 备份：`D:\Unity\backups\RPG_Project_2026-11-09_feedback\`
- [x] 阶段 2（基础 HUD）已保存并验证：主角生命、目标显示/隐藏、1920×1080 适配、退出按钮右上移且无重叠。
  - 修复了 Level Up 预制体 SH_VFX 缺失脚本：击杀特效改用 SimpleFX 黄色小型烟花。
  - 修复伤害弹字镜像：朝向直接采用相机旋转；判别测试由 dot=0.546 失败变为 dot=1 通过。
- [x] 阶段 3（死亡重开）已保存并验证：重复死亡仅一个面板，重新挑战状态复位，返回主界面成功，已单独截图确认面板。

- [x] 第一批最终回归与磁盘重载：编译、相机、反馈、HUD、战斗、死亡、场景往返和服务引用持久化均完成。
  - 验收：`docs/superpowers/reports/2026-11-09-feedback-hud-verification.md`
  - 截图：`docs/verification/2026-11-09/`

- [x] 第二批最终回归与磁盘重载：编译、相机、反馈、HUD、药水、目标、背包、战斗、死亡、场景往返均通过；实战 potions=1/kills=1。
  - 验收：`docs/superpowers/reports/2026-11-09-potions-inventory-verification.md`
  - 截图：`docs/verification/2026-11-09-potions/`

- [x] 完整 UI/HUD 最终回归与磁盘重载：默认/Full HD 动作栏与背包、暂停、死亡、战斗、场景往返和两个场景按钮接线均通过。
  - 验收：`docs/superpowers/reports/2026-11-09-full-ui-verification.md`
  - 截图：`docs/verification/2026-11-09-full-ui/`

## 下一步候选（尚未批准实施）

1. 第三批：弓箭手、剑/弓切换、三段短关卡。

## 2026-11-09 第二批实施进度

- [x] 阶段 1（药水循环）已保存并通过：30 点治疗、Q 对应 TryUse、满血/空库存/死亡不消耗、敌人单次掉落、真实碰撞拾取一次入库。
  - 新增 PotionInventory/HealthPotionPickup/LootDropper 与 HealthPotionPickup.prefab。
  - 原拾取资源副本移除了 2 个缺失脚本，原资源未改。
  - 测试改为条件等待；修正死亡测试数量期望（拾取 1 + Add 1 = 2）。
- [x] 阶段 2（目标 HUD）已保存并通过：药水 ×N、击杀进度 0/1、完成文本、重复注册不增加。
  - 布局测试先发现目标提示与退出按钮重叠，下移后 Rect 不相交并截图验收。
- [x] 阶段 3（简化背包）已保存并通过：I 对应 Toggle、药水数量同步、实时不暂停、布局不相交、中文与图标截图验收。

## 2026-11-09 完整 UI/HUD 实施进度

- [x] 阶段 1（完整 HUD）已保存并通过：三个动作槽、Q 药水按钮、I 背包按钮、操作提示 8 秒消退、布局不相交。
  - 备份：`D:\Unity\backups\RPG_Project_2026-11-09_full-ui\`
- [x] 阶段 2（Esc 暂停菜单）已保存并通过：暂停/继续、Esc 优先关闭背包、死亡时禁止暂停、齿轮菜单接线、旧 HeroCamera Esc 退出移除、死亡按钮素材纠正。
- [x] 阶段 3（开始界面）已保存并通过：标题/副标题、开始游戏进入 RPG、退出游戏停止 Play、1920×1080 Canvas 设置。

## 2026-11-09 潜行动作控制进度

- [x] 阶段 1（直接移动）已保存并通过：WASD 核心、Shift 奔跑、斜向 Clamp、CharacterController 碰撞/重力、Run 状态、死亡停止。
  - 固定 0.1s 验证：walk=0.400、run=0.600、diagonal=0.400；墙体碰撞 along=1.42。
  - 测试计时问题通过固定步长核心与物理注册等待解决，未靠放宽生产逻辑通过。
  - 备份：`D:\Unity\backups\RPG_Project_2026-11-09_direct-control\`
- [x] 阶段 2（直接攻击）已保存并通过：远距离不自动追敌、近身单击只扣 20、目标 HUD 正确；旧点选模式 PlayProbe5 敌人 100→0 仍通过。
- [x] 阶段 3（文案与全量回归）已保存并通过：WASD/Shift 文案、直接移动/碰撞/奔跑/攻击、药水、HUD、背包、暂停、死亡重开、场景往返和旧点选战斗均通过。
  - 验收：`docs/superpowers/reports/2026-11-09-direct-control-verification.md`
  - 截图：`docs/verification/2026-11-09-direct-control/`

## 下一步候选（尚未批准实施）

1. 卫兵感知 AI：Patrol→Suspicious→Search→Chase→Attack。
2. 高草/灌木隐藏、背后刺杀与 E 互动。
3. 村庄任务闭环：取得卷轴→刺杀/避开卫兵→撤离；远程弓箭卫兵可作为感知 AI 的扩展。

## 2026-11-09 潜行场景与视觉进度

- [x] 阶段 1（第三人称探索相机）已保存并通过：offset=(0,3.2,4.2)、pitch25°，缩放/旋转、固定步长移动与墙体碰撞回归通过；前后截图已保存。
- [x] 阶段 2（黄昏光照）已保存并通过：夜景材质、ambient0.85、方向光0.65冷蓝、两个暖色火点；截图确认角色与地面可读。
- [x] 阶段 3（掩体与击杀尘土）已保存并通过：10 处高草/灌木、无启用碰撞、不改 NavMesh、击杀尘土单次生成并 2.5 秒回收；近景与拉远截图已保存。
- [x] 潜行视觉最终回归与磁盘重载完成：移动/碰撞/攻击、药水、HUD、背包、暂停、死亡、场景往返和旧模式延长观察均通过。
  - 验收：`docs/superpowers/reports/2026-11-09-stealth-visuals-verification.md`
  - 截图：`docs/verification/2026-11-09-stealth-visuals/`

## 2026-11-10 子项目 A：卫兵感知与实体掩体

- [x] GuardSenses 与 EnemyController 接入已保存：视锥/遮挡、Chase/Search/Patrol、受击和死亡测试通过；Search 后巡逻点正确复位。
- [x] StealthPhysicalCover 已创建并重烘焙 NavMesh：围栏/岩石/土坡碰撞启用，全部巡逻点路径完整。
- [x] 子项目 A 全量回归与验收归档完成：直接攻击、药水、HUD、背包、暂停、死亡、场景往返均通过；旧模式攻击距离修正为 1.4/2.0 后敌人 100→0。
  - 验收：`docs/superpowers/reports/2026-11-10-guard-senses-verification.md`
- [x] 子项目 B 已完成：细剑模型/匕首图标、E 刺杀提示、Patrol+背后+近距+面向才一击必杀、动画事件复核与普通攻击回退均通过；普通攻击、HUD、背包、死亡和场景往返回归通过。
  - 验收：`docs/superpowers/reports/2026-11-10-assassination-verification.md`
- [x] 子项目 C 已完成：Archer 场景实例、弓动画覆盖控制器、StealthArrow、远程攻击与真实命中/躲避/撞墙/超时回收通过；双目标、刺杀、普通攻击、死亡和场景往返回归通过。
  - 验收：`docs/superpowers/reports/2026-11-10-archer-verification.md`
- [x] 子项目 D 已完成：卷轴→撤离任务闭环、胜利面板、Portal/Lightray、烟雾遮挡/冷却/恢复、第四动作槽均通过；任务重置、四槽 HUD、刺杀、普通攻击、NavMesh 和场景往返回归通过。
  - 验收：`docs/superpowers/reports/2026-11-10-mission-smoke-verification.md`

## 全部六个潜行方向已交付

1. 细剑与刺杀（A/B 已完成）。
2. 弓箭卫兵（C 已完成）。
3. 围栏/岩石/土坡实体遮挡与 NavMesh（A 已完成）。
4. 木箱卷轴与任务状态（D 已完成）。
5. Portal/Lightray 撤离（D 已完成）。
6. Smoke 真实视线遮挡（D 已完成）。

## 2026-11-10 武器挂点修复

- [x] 细剑与弓已改挂 `Animator.GetBoneTransform(RightHand)`，不再挂 Attack1pos/Attack2pos；隐藏 Item_Sword/Item_SwordHolder 背部重复武器。
- [x] WeaponAttachmentTests、DirectAttackTests、AssassinationTests 通过；修复截图已归档。
- [x] 最新素材依赖审计：449 项美术资源中 335 项未被当前两个场景直接引用，已保存 used/unused 清单。

## 2026-11-10 人物与 NPC 动作优化

- [x] 弓箭手改为左手持弓，恢复原 Equipped Bow 的局部偏移与 90°旋转，发射点跟随左手；拉弓姿态截图已验证。
- [x] HeroDirectMovement 使用目标状态去重的 CrossFade，修复过渡每帧重启；NPC 移动与攻击互斥，攻击时不再叠加 walk。
- [x] RunAnimationStateTest、DirectAttackTests、AssassinationTests、ArcherEventOnlyTest、BowPoseTests、WeaponAttachmentTests 全部通过。

## 2026-11-11 《影村》实施方案进度

- [x] 方案已加入逐阶段自检、测试诚信红线、工程/场景红线、停止条件与最终完成判定，保存于 `C:\Users\22492\.claude\plans\clever-sparking-teapot.md`。
- [x] P0 基线保护完成：脚本/场景/NavMesh 已备份，编译、武器挂点、移动、攻击、刺杀、弓箭、药水、任务、烟雾、HUD、暂停、死亡与场景切换均实际运行通过。
  - 验收：`docs/superpowers/reports/2026-11-11-P0-baseline-verification.md`
- [x] P1：动作与感知手感、潜行状态、隐藏区域、听觉与可解释警戒已完成并保存。
  - 新增 PlayerStealthState、NoiseEmitter、StealthCoverArea；Ctrl 潜行速度 2.2，10 处高草/灌木使用显式隐藏判定，烟雾复用 SmokeZone。
  - 警戒受距离与潜行/隐藏倍率影响；HUD 增加状态与警戒解释；声音只产生范围限定的 Suspicious，不直接授予实时追击目标。
  - 相机增加探索/潜行/交战距离；RangedAttack 在感知重置后清除过期目标。
  - 实测：普通/潜行固定步长 0.400/0.220；同条件警戒 0.115/0.073；移动、攻击、刺杀、弓箭、感知、药水、任务、烟雾、HUD、背包、暂停、死亡、场景切换和 4 个巡逻点完整路径均通过。
  - 备份：`D:\Unity\backups\RPG_Project_2026-11-11_P1-stealth\`
  - 验收：`docs/superpowers/reports/2026-11-11-P1-stealth-verification.md`
  - 截图：`docs/verification/2026-11-11-p1/`；实时日志：`docs/verification/2026-11-11-p1-live.log`
- [ ] P2：刺杀反馈、尸体发现与有限警报扩散。**用户于 2026-11-11 决定暂停，2026-11-12 再继续**；目前仅在项目外 `C:\Users\22492\.claude\tools\P2*Tests.cs` 准备了三组判别测试与持久化检查，未运行、未修改 P2 生产代码或场景。

### 当前完成节点（2026-11-11 收工）

- **P0、P1 已完成并保存；P2 生产实现未开始。**
- 当前优先事项是等待另一会话完成 P1 独立验收；编辑器由验收会话独占，本会话不再进入 Play 或修改场景/脚本。
- 明日先读取独立验收结论与待处理项；若有 P1 阻断问题先修复，否则再开始 P2。项目外已准备的 P2 测试不是已通过证据。

## 当前限制

- 项目未安装 Input System，Bridge 无法做真实鼠标注入；真人键鼠手感仍待用户验收。
- 既有烘焙后端、地形树 shader 告警仍在；场景切换的 Unknown Missing Script 提示未定位到具体对象，两个场景的 Missing Script 扫描均为 0。
- 工程无 Git，不提交；每个小阶段通过场景保存、文件备份和本文件留档。

## 2026-11-12 剧情化第一章《回村的人》实施进度

- 方向与计划：按用户批准的 `docs/superpowers/specs/2026-11-11-chapter-one-story-design.md` 与 `docs/superpowers/plans/2026-11-11-chapter-one-story.md` 执行；原 P2 不再作为当前工作。
- 本会话直接实施，昨天的开发会话已退出。已实测连接 Tuanjie 2022.3.62t5 与 `D:\Unity\projects\RPG_Project`，基线场景无未保存改动、编译 0 错误。
- [x] Task 1 剧情数据资产完成：`DialogueAsset.cs`、`ChapterDefinition.cs`。测试先因类型不存在失败，实现后 `STORY_ASSET_TESTS PASS clues=3`，编译 0 错误。
- [x] Task 2 黑场字幕、对话框、结局面板完成：`SubtitleOverlay.cs`、`DialoguePanel.cs`、`EndingPanel.cs`。实现前测试按预期失败，实现后 `STORY_UI_TESTS PASS`，验证 timeScale=0 下 UI 可推进、选项回调与结局时间恢复。
- [x] Task 3 剧情状态机完成：`StoryDirector.cs`，并前置实现编译依赖 `StoryTarget.cs`。`STORY_DIRECTOR_TESTS PASS ending=spare heroEnabled=True`，覆盖开场、线索幂等/非法线索、未齐线索禁止对峙、连续对话与结局后的主角控制恢复。队长近距自动对峙与结局实际结算已接线，Task 5 专项复核。
- [x] Task 4 NPC 对话、线索拾取、偷听区完成：`NpcInteractor.cs`、`CluePickup.cs`、`EavesdropZone.cs`。`STORY_INTERACTION_TESTS PASS clues=2`，覆盖距离负例、无选项对话关闭、拾取幂等、偷听 3 秒阈值；家书和偷听支持真实对话内容展示。
- [x] Task 5 剧情目标与 HUD 接入完成：`StoryTarget.cs` 专项通过，队长未解锁时不可被 E 刺杀、剧情走位暂停原 AI、HUD 由 StoryDirector 驱动、4m 外不对峙而近距自动对峙。`AssassinationController.cs`、`GameHUD.cs` 已备份至 `D:\Unity\backups\RPG_Project_2026-11-12_story\`；新增 E 对话/查看家书提示。
- [ ] Task 6：第一章场景与剧情内容接入，进行中；场景已备份为 `RPG游戏.before-story.unity`（MD5 34b36ce3…）。
- [x] Task 6 第一章场景与内容已保存：5 个对话资产 + `chapter_01.asset`，老药师、家书、偷听区、队长井边点接线，旧取卷轴/撤离控制器停用；开场最近巡逻点移出安全距离。三条真实路径分别通过：直接杀 `kill`、听完后杀 `kill_after_truth`、放走 `spare`；杀结局队长真实死亡，放走结局队长存活，结局后主角控制恢复。
  - 修复：剧情启动延迟一帧，避免开场暂停早于角色 `Start` 导致主角 HP 未初始化。
  - 视觉：剧情选项改为深色底；结局按钮可读性修正随后回归。
- [x] Task 7 死亡检查点完成：`StoryCheckpoint.cs`、`StoryDirector.CaptureCheckpoint/ApplyCheckpoint`，`GameFlow.OnHeroDeath` 捕获快照。实现前测试按预期失败，实现后 `STORY_CHECKPOINT_TESTS PASS clues=1 stage=Investigate heroEnabled=True`；重载后不重复开场、家书不复活、timeScale=1。快照仅内存，不做磁盘存档。
- [x] Task 8 全量回归、视觉与持久化验收完成：剧情资产/UI/状态机/交互/剧情目标/三路径/检查点/真实死亡重开均通过；12 项旧玩法回归全部通过；磁盘重载引用与安全区通过；最终编译 0 错误/0 警告。
  - 修复剧情启动顺序、嵌套暂停恢复、剧情禁用静态实例残留、按钮可读性与截图稳定帧。
  - 验收：`docs/superpowers/reports/2026-11-12-story-verification.md`
  - 截图：`docs/verification/2026-11-12-story/`；回归日志：`C:\Users\22492\.claude\tools\story\regression\`
  - 最终状态：编辑态、`timeScale=1`、`RPG游戏` 场景从磁盘重载且无未保存改动；测试快照已清空。
- **《影村》当前已是剧情驱动第一章，不再是取卷轴后撤离的搜打撤关卡。** 下一步为用户真人试玩（真实键鼠手感与剧情阅读体验），后续章节/刺杀反馈/尸体发现另行批准。

## 2026-11-12 第二章剧情实施进度

- [x] Task 1 章节数据、会话状态与第二章初始化选择完成：`ChapterDefinition` 增加下一章、章节标题、开场变体与结局定义；新增 `StorySession`；`StoryDirector` 支持待续章节选择和按上一结局选择开场；新游戏与返回主界面清空会话，重新挑战保留检查点。RED 测试先因类型/成员不存在失败，实现后实际编译 0 错误/0 警告，`CHAPTER2_SESSION_TESTS PASS`。
  - 备份：`D:\Unity\backups\RPG_Project_2026-11-12_chapter2-task1\`（修改前 4 个脚本、完成后 5 个脚本）。
  - 报告：`.superpowers/sdd/chapter2-task-1-report.md`。

## 2026-11-13 用户反馈 bug 修复与场景审计

用户反馈：走路卡帧、武器不在手中；要求全面检查违背常理/物理 bug（如缺碰撞体），并希望在局限场景内做细节增强。

- [x] 阶段 1a 走路卡帧已修复：根因是 `heroController` 状态机的 `HumanoidWalk` 有 `walk==false` 退出过渡，而直接控制从不设置 `walk`；CrossFade 进走路后约 0.75 个周期被打回 Idle，且 `requestedState` 去重使其永不恢复（实测第 394 帧起 206/580 帧不在走路状态）。修复：`HeroDirectMovement.UpdateAnimation` 同步 `anim.SetBool("walk", moving && !running && !hero.isAttack)`。修复后 600 帧连续走路 0 帧掉出走路状态。
- [x] 阶段 1b 武器握持已修复并保存场景：`Hand_R/StealthRapierVisual` 本地旋转由 `(0,0,0)` 改为 `(0,0,-90)`，剑身由手骨 +Y 改为沿手指轴 +X（修复前 dot=0，修复后 dot=1），截图确认自然垂剑。场景 MD5 由 `08c22de5…` 变为 `5e9b2900…`。
  - 备份：`D:\Unity\backups\RPG_Project_2026-11-13_bugfix\`（`HeroDirectMovement.cs`、`RPG游戏.unity`）。
  - 判别测试：`C:\Users\22492\.claude\tools\diag\BugfixTests.cs`（修复前两 FAIL，修复后逐项 PASS）；诊断脚本与截图均在 `C:\Users\22492\.claude\tools\diag\`。
- [x] 阶段 2 碰撞/物理修复已完成并保存：25 个环境实体补 MeshCollider（9 栋房屋/村屋、7 道栅栏、4 个木箱等）；163 棵树的原型改为带胶囊碰撞的变体 `Assets/RPG/Prefabs/SM_Env_Tree_03_Collider.prefab`（原 Synty 预制体未改）；新增 4 面 `WorldBoundary` 隐形墙（±47，高 12）；NavMesh 重烘 316ms。
  - 地形数据复制为本场景专用 `Assets/RPG/RPG_Terrain.asset`，原 `Course Sandbox Terrain.asset` 未改。
  - 判别测试：`CollisionTests.cs` 由 4 项 FAIL 变为 4 项 PASS；`PhysicsTests.cs` 撞房/边界/房屋视线遮挡 PASS；`TreeCollisionTest.cs` 抽样 5/5 棵树 BLOCKED、PASS。
  - 运行时验证：房屋中心与全部建筑中心均不在 NavMesh 上；4 个巡逻点采样可达；长老/井边/剧情点路径 `PathComplete`（家书在箱顶，需先采样到箱边）。
  - 12 项既有玩法回归 **12/12 PASS**（`C:\Users\22492\.claude\tools\diag\regression_2026-11-13.log`）。
  - 修正审计误判：`SM_Bld_House_Base_01` 经截图目检是两层村屋的上层，不是悬浮，位置未动。
  - 场景保存后 MD5：`4332c3c9d5c1a01697e2be87bcfbc902`（阶段 2 完成后）；Terrain 资产 `e2f7f8c0c4e135414db10b1be9bf524a`；本轮前场景基线 `08c22de5…`（备份见上）。
  - 尚未修复的次要观感项（本轮不处理，留待批准）：主角头顶世界空间血条过宽；潜行移动仍播放普通走路动画（脚步频率与 2.2m/s 速度不匹配）；攻击动画是双手剑动作而武器是单手细剑。
- [x] 阶段 3 场景细节增强已实施（克制写实，四类全做），设计与计划见 `docs/superpowers/specs/2026-11-13-rpg-scene-detail-design.md` 与 `docs/superpowers/plans/2026-11-13-rpg-scene-detail.md`。
  - 新增根节点 5 组共 299 个对象：地面标记、植被 110、生活道具 104、灯光 10、边界林带 75。
  - 地形改用本场景专用 `Assets/RPG/RPG_Terrain.asset`（原 Synty 资产已还原为刷路前内容，名称与内容均已核验）。
  - 土路：三条主路 alphamap 刷图（泥土主导），`basemapDistance` 由 1000 调至 25 以显示 splatmap。
  - 氛围：5 盏窗光 + 3 盏灯笼 + 2 处火堆烟，点光总数 10；开启指数雾 density 0.006。
  - 校验：`out/validation.txt` 全 PASS（对象预算、灯光预算、雾、6 条 NavMesh 路径、房屋仍不可行走、禁区间距 1.8m）。
  - 12 项既有回归 **12/12 PASS**；第一章三条剧情路径 **kill / kill_after_truth / spare 全部 PASS**（队长死亡与主角控制符合预期）。
  - 帧时间：839 帧平均 2.905ms、p95 3.44ms、最大 12.05ms（编辑器绝对帧率，无同条件前基线，不宣称退幅）。
  - 场景 MD5 `50d1b39ba1e84465eac5aabbd4495b51`；`RPG_Terrain.asset` MD5 `43b87f944ec14e03c4774384765c875f`；原始 Synty 地形已还原为刷路前权重（出生点 Green=0.84）。
  - 截图归档：`docs/verification/2026-11-13-scene-detail/`；验收报告：`docs/superpowers/reports/2026-11-13-scene-detail-verification.md`。
  - 回滚：删除 5 个 `SceneDetails_*` 根节点，或还原 `D:\Unity\backups\RPG_Project_2026-11-13_scene-detail\`。

## 2026-11-13 鼠标视角 + 全流程试玩审计

用户要求：视角跟随鼠标、弓箭手武器浮空修复，并要求先完整试玩再一次性报告 bug。

- [x] 鼠标锁定视角：`HeroCamera` 改为锁定光标，鼠标 X/Y 转 yaw/pitch（`minPitch=-30`、`maxPitch=70`），滚轮缩放保留；剧情/暂停/对话 `timeScale=0` 自动解锁，恢复后重新锁定；死亡/返回主菜单自动解锁。`ApplyLookDelta(Vector2)` 供测试调用。
  - HUD 操作提示由"中键旋转"改为"鼠标视角"。
- [x] 弓箭手弓浮空已修：`Archer/.../Hand_L/ArcherBowVisual` 本地偏移由 `(0.27,-0.02,0.80)`、旋转 90° 改为 `(-0.22,-0.65,0)`、identity；弓距左手 0.84m → 0.14m，拉弓/近战动画下贴手。
- [x] 66 处柴火/长凳悬空或陷入地面已按地形高度修正；悬空扫描清零。
- [x] 全流程试玩（脚本驱动）：剧情三线索→对峙→放走结局；背后刺杀成功；弓箭手近战/远程（箭周期生成）；潜行/藏草；烟雾/药水；暂停恢复；相机锁定与遮挡。日志与截图在 `C:\Users\22492\.claude\tools\audit\`。
- [x] 回归：12/12 PASS；三剧情路径全 PASS；编译 0/0。
- 报告：`docs/superpowers/reports/2026-11-13-rpg-playthrough-audit.md`。
- 备份：`D:\Unity\backups\RPG_Project_2026-11-13_mousecam_bow\`。
- 场景 MD5 `bd3e352457e7b5f46d64895a84c8b27f`。
- 遗留（报告 P2/P3，未修）：头顶血条过宽、潜行仍播普通走路动画、攻击动画双手剑而武器单手细剑、弓箭手近身不主动拉开射程、草丛夜景剪影、灯笼无连接臂、部分屋顶俯视可看穿、`Order/Button (Legacy)` 越界待核查。
- 限制：脚本驱动试玩不等于真人键鼠，鼠标手感需用户实机确认。

## 2026-11-14 P2 四项修复完成

- [x] 三个世界空间血条统一缩小，主角世界宽度 1.536m → 0.768m。
- [x] `heroController` 新增 `HumanoidSneak`（同走路剪辑、0.55 倍速），`HeroDirectMovement` 潜行移动进入该状态；运行时 stateSpeed=0.55。
- [x] 主角攻击 motion 换成工程已有单手剑 `Sword-Attack-R6`，新 `.anim` 保留 `AttackEnemyEvent`（0.32s）；实测 HP 100→80，伤害未丢失。
- [x] `RangedAttack.TryRetreat`：近身 3m 内沿完整 NavMesh 路径后撤，4.5m 恢复射击，无路径则不穿墙；实测后撤至 4.757m 后射箭，玩家 HP 100→80。
- 验证：四项均先 RED 后 GREEN；既有 12 项回归 **12/12 PASS**，三剧情路径全 PASS。
- 报告：`docs/superpowers/reports/2026-11-14-rpg-p2-fixes-verification.md`；证据：`docs/verification/2026-11-14-p2-fixes/`。
- 备份：`D:\Unity\backups\RPG_Project_2026-11-14_p2-fixes\`；场景 MD5 `987173be98efce82112690cc7c130765`。
- 最终状态：编辑态、场景无未保存改动。真人手感仍待用户实机试玩。

## 2026-11-14 鼠标上下与弓朝向修正

- [x] 鼠标上下反转：`HeroCamera.ApplyLookDelta` 的 pitch 改为 `pitch + delta.y * sensitivity`；运行时鼠标上推 pitch 0→7.5，相机实际移动 0.423m。
- [x] 弓朝向反转：`ArcherBowVisual` 绕本地 Y 轴翻转 180°，localPosition 同步镜像为 `(0.22,-0.65,0)`，握点仍 `(0,0.14,0)`；运行时距手 0.141m，未再次浮空。
- 测试：修复前两项 FAIL，修复后静态/运行时均 PASS；证据 `docs/verification/2026-11-14-orientation-fixes/`。
- 备份：`D:\Unity\backups\RPG_Project_2026-11-14_mouse-bow-orientation\`。
- 场景 MD5 `0b9a2cb2df06352e9534fe9b2daaa2e3`；`HeroCamera.cs` MD5 `4b59f36097e312d7118d55b1464ef8b1`。
- 全量回归 **12/12 PASS**（`C:\Users\22492\.claude\tools\audit\regression_orientation.log`）；最终编辑态、场景无未保存改动。

## 2026-11-14 人物碰撞体补全

- [x] 实测所有人物：Hero 的 CharacterController、队长与弓箭手的 CapsuleCollider 均有效；玩家撞队长停在 0.83m，移动敌人不会穿过玩家。唯一缺口是老药师 `NpcElder` 无碰撞体，玩家可与其重合到距离 0。
- [x] 老药师新增非触发 CapsuleCollider：height=2.4、radius=0.35、center=(0,1.2,0)；实测玩家最近停在 0.78m，所有存活人物均有有效身体碰撞。
- 未改已有角色物理，避免重引入刚体推挤/尸体下坠问题；死亡后禁用碰撞仍保留（避免尸体挡路）。
- 修复前：缺碰撞体 1、NPC 穿行 FAIL；修复后全部 PASS。证据：`docs/verification/2026-11-14-character-colliders/`。
- 备份：`D:\Unity\backups\RPG_Project_2026-11-14_npc-collider\`；场景 MD5 `dad785c435f4f0b4fb1a59d8a0056af6`。
- 全量回归 **12/12 PASS**，三剧情路径全 PASS（`regression_npc_collider.log`、`story_paths_npc_collider.log`）。
- 第二章暂不动工，等待用户给出需求后与第一章衔接。

## 2026-11-14 人物与地图碰撞全面检查

- 实测发现三类问题：① 队长/弓箭手胶囊中心 y=0.8、高 2.45，底部低于脚底约 0.425m，会插入土坡/地面；② 新增装饰只标 Batching/Occludee Static，未标 Navigation Static，NavMesh 自场景细节加入后一直未更新；③ 岩石顶部/斜坡被导航当成可行走面，弓箭手巡逻有 0.07–0.12m 瞬时擦入。
- 已修：两个敌人胶囊统一 height=2.4、radius=0.4、center=(0,1.2,0)；125 个实体装饰标 Navigation Static 并重烘 NavMesh；27 个岩石增加 `NavMeshObstacle`（`carving=true`、`carveOnlyStationary=true`），土坡保留可行走。
- 已验证：所有人物静止时地图穿插 0；木桶/树桩/边界树内部均不再可行走；玩家撞木桶停在 0.500m；20 秒实时巡逻人物与地图穿入 0；六条关键 NavMesh 路径全部完整。
- 备份：`D:\Unity\backups\RPG_Project_2026-11-14_character-map-collision\`（场景与旧 NavMesh）。
- 场景 MD5 `e5532d6b006634a432f4a1caef1aba86`；NavMesh MD5 `065bd3bb5b75d642c5601d1dac0a1a07`。
- 报告：`docs/superpowers/reports/2026-11-14-character-map-collision-verification.md`；证据：`docs/verification/2026-11-14-character-map-collision/`。
- 全量回归 **12/12 PASS**，三剧情路径全 PASS；最终 20 秒巡逻地图穿入 0，编辑态、场景无未保存改动。

### 地图碰撞回归暴露的弓箭手卡死（已修）

- 首轮回归 `P1ArcherEventOnlyTests` FAIL：后撤目的地被 `EnemyController.DoMove` 覆盖，且 NavMesh 最近点可能是当前位置，导致永远到不了后撤退出距离。
- 修复：`RangedAttack.IsRetreating` 时 `DoMove` 不再覆盖目的地；候选点必须比当前位置多拉开 0.25m 且 `SetDestination` 成功，否则退出后撤并恢复射击。
- 实测 1.5s 进入攻击、2.0s 出箭、2.25s HP 100→95。旧测试命中窗口 1.5s 过窄，仅调整为 3s，仍断言恰好一次命中；原测试已备份。
- 最终全量回归 **12/12 PASS**，三剧情路径全 PASS（`regression_map_collision_final.log`、`story_paths_map_collision_final.log`）；真实后撤射击复测 HP 100→80。




- 注意：第二章 Task 2-5 仍未开始，本轮不涉及第二章。

## 2026-11-14 第一章收尾 polish

- [x] 夜景植被：新增两个本项目专用夜间材质，100 个草/灌木/花 Renderer 已覆盖，保留贴图并加入低强度补光；原 Synty 共享材质未改。
- [x] 三盏灯笼均补上 `LanternArm`，连接挂钩与墙面，无碰撞、不影响 NavMesh。
- [x] UI 越界复核：正确投影下没有 Overlay 控件真正越界；离屏角色的世界血条在屏幕外属正常，未改 UI。
- 测试：植被 0/100→100/100，灯笼臂 0/3→3/3；截图与证据 `docs/verification/2026-11-14-chapter1-polish/`。
- 报告：`docs/superpowers/reports/2026-11-14-chapter1-polish-verification.md`。
- 备份：`D:\Unity\backups\RPG_Project_2026-11-14_chapter1-polish\`；场景 MD5 `154103b8e911b95ad1c3aebf30a76d6e`。
- 全量回归 **12/12 PASS**，三剧情路径全 PASS；暗部像素占比 36.3%→13.7%；最终编辑态、场景无未保存改动。

## 2026-11-14 第一章敌人阵容、过渡与主菜单改造

- [x] 删除 `Archer` 场景实例；弓手专项回归退役，脚本/资产暂留。
- [x] 新增 4 名剑兵：东侧 `SwordGuard_East_A/B`、北侧 `SwordGuard_North_A/B`，距出生点 47–60m；每名 3 个巡逻点，均无 `StoryTarget`。
- [x] 北侧原巡逻区被 NavMesh 分区切断，按网格搜索迁移到 `(16,28)` / `(-20,28)`，并剔除 2 个不可达点。
- [x] 剑兵原地转圈根因：`*_PatrolPoints` 被挂在剑兵子物体下，目标随人物移动/转向；已移到独立 `SwordGuardPatrolPoints` 根节点，世界坐标不变。6 秒实测位移 3.09–5.44m，巡逻点静止 4/4。
- [x] 场景过渡 `SceneTransition`：0.7s 淡出、章节标题至少 1s、异步加载、0.8s 淡入；过渡冻结移动；开场字幕延后到淡入结束。
- [x] 主菜单电影化重做：实景背景 + 暗角 + 左侧标题/副标题/标语 + 两个金色按钮；按钮文字对比度已修。
- 验证：roster 静态全 PASS；4/4 剑兵实际巡逻；击杀不改变剧情；20 秒地图穿模 0；菜单 7 项全 PASS；真实按钮点击完成过渡并显示剧情。
- 产物：`docs/superpowers/specs/2026-11-14-chapter1-roster-menu-design.md`、`docs/superpowers/plans/2026-11-14-chapter1-roster-menu.md`、验收报告；证据 `docs/verification/2026-11-14-roster-menu/`。
- 首轮回归两项测试夹具未适配：感知测试随机选中远处剑兵（改为最近目标），死亡流程第二次切换发生在过渡未结束时（等待窗口改 3.2s）；复测通过，生产行为未放宽。
- 最终回归 **11/11 PASS**（弓手专项退役），三剧情路径全 PASS；编辑态、场景无未保存改动。
- 场景 MD5 `0bba043a98a82fccf3f2009b386e31b9`，菜单 MD5 `07e7ec01f7131937a2fae4212618650f`。
- 巡逻根因修复：4 个巡逻点根节点移到 `SwordGuardPatrolPoints`；6 秒实测位移 3.09–5.44m、巡逻点静止 4/4。

## 2026-11-14 全系统审计（寻路/攻击/状态机）

- 角色状态：Idle/Walk/Run/Sneak（状态哈希 -1383648164）/停止回 Idle/攻击 `Sword-Attack-R6`/死亡 `Armed-Death1` 全部通过。
- 攻击：2m 不命中、1m 扣 20、冷却内不重复、冷却后可再攻击、攻击盒最终 0；敌人近战 4 秒扣 15（3×5）。
- **发现并修复攻击滑步**：攻击期间移动 60 帧原本滑行 0.85m；`HeroDirectMovement.Move` 加 `isAttack` 站定门槛，修复后 0.00m，攻击结束后恢复移动。
- Guard 状态机：Patrol→Chase→Search→Patrol 通过；5 个敌人均 onMesh、路径完整、1 秒位移 1.16–1.76m。
- 排查假阳性：潜行按剪辑名误读（实际状态哈希正确）；死亡动画在剧情 `timeScale=0` 下测试导致停帧（正常死亡 `Armed-Death1` 正常）。
- 报告：`docs/superpowers/reports/2026-11-14-full-systems-audit.md`。
- 最终回归 **11/11 PASS**，三剧情路径全 PASS；修复后场景 MD5 `0bba043a98a82fccf3f2009b386e31b9`，`HeroDirectMovement.cs` MD5 `17a0ce1ebb2a831a63eafa25f9703d5e`。
- 已提交并推送：`c43f8e4`（远程 `Shade-Hamlet/main` 已核对一致）；工作区干净。

## 2026-11-14 音量设置

- [x] 新增 `AudioSettings` 主音量服务（`AudioListener.volume` + `PlayerPrefs` `masterVolume`，启动前自动加载）。
- [x] 主菜单新增“设置”按钮，打开统一 `VolumeSettingsPanel`；按钮顺序开始/设置/退出。
- [x] 暂停菜单新增“音量设置”按钮；Esc 优先关闭音量面板再关暂停菜单。
- [x] 面板包含主音量滑条、百分比、关闭；关闭时持久化。
- [x] 顺带清理主菜单残留的旧 `Button (Legacy)`、`StartSubtitle`，补齐 `MenuSubtitle`。
- 验证：主菜单与暂停菜单真实滑条均生效（35%/60%）、持久化、关闭回退、Esc 顺序通过。
- 用户复测反馈修复：滑条原被 `RpgUI.CreateSlider` 设为不可交互且无手柄，已改为可交互并加 30×30 手柄；界面场景原本没有 AudioSource，已添加 `MenuMusic` 循环播放 BGM1；修正“主音量/百分比”文字跑出面板。
- 真实指针拖拽验证：滑到 25% 后音量=0.25、标签 25%；菜单 BGM `isPlaying=True`。
- 最终回归 **11/11 PASS**，三剧情路径全 PASS；`界面.scene` MD5 `d1c38646b9ed2ef48bdee53042cf9fc2`，`VolumeSettingsPanel.cs` MD5 `dab82d24cc89d650db2cde45ec2cc1ed`；已提交推送 `91233e2`。
- 报告：`docs/superpowers/reports/2026-11-14-volume-settings-verification.md`；截图/日志：`docs/verification/2026-11-14-volume-settings/`。
- 最终回归 **11/11 PASS**，三剧情路径全 PASS；场景 MD5 `0bba043a98a82fccf3f2009b386e31b9`、`界面.scene` `0ad68effbe91add61cbd2302ca60ffa7`；未提交。


- 未提交；等待用户确认后再同步发布副本并推送。


- GitHub 已推送：`2c02b91`（操作/动画/碰撞）与 `ffd8d80`（第一章夜景与灯笼）；远程 `Shade-Hamlet` main 已核对为 `ffd8d80a6f5301bf52681d2917b2e5cfb0a38fed`。直连推送上传被重置，改走本机 Clash `127.0.0.1:7897` 代理成功。




