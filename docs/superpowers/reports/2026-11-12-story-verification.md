# 《影村》第一章《回村的人》实施与验收报告

日期：2026-11-12
实施：本会话直接执行，未使用昨天的开发会话。
依据：`docs/superpowers/specs/2026-11-11-chapter-one-story-design.md`、`docs/superpowers/plans/2026-11-11-chapter-one-story.md`

## 交付结论

**第一章已从「取卷轴→撤离」改为剧情驱动的复仇刺杀流程，并保存到真实场景。**
开场黑场字幕 → 老药师对话 → 家书取证 → 兵营偷听 → 队长井边忏悔 → 近距对峙 → 杀/放抉择 → 结局与下一章钩子均可用。

玩家不再以搜集物资并撤离结算为目标；潜行、刺杀和卫兵感知用于调查与接近剧情目标。

## 实际新增内容

- 剧情资产：`chapter_01`、`elder_intro`、`family_letter`、`barracks_whisper`、`captain_confront`、`final_choice`，位于 `Assets/Story/`。
- 剧情运行时：`StoryDirector`、`StoryCheckpoint`。
- UI：`SubtitleOverlay`、`DialoguePanel`、`EndingPanel`。
- 交互：`NpcInteractor`、`CluePickup`、`EavesdropZone`、`StoryTarget`。
- 场景：`Story`、`StoryPoint`、`NpcElder`、`ClueFamilyLetter`、`EavesdropBarracks`；近战 `Enemy` 作为卫兵队长。
- HUD：目标文本由剧情状态机驱动；增加「E 对话」「E 查看家书」提示。
- 旧任务：场景中的 `IntelPickup`、`EscapePoint`、`MissionController` 均停用，对象保留供回滚；旧 `MissionTests` 随该玩法废弃。
- 开场安全区：最近巡逻点移离出生点；磁盘重载后最近点距离实测 11.71m（含高度差）。

## 剧情路径实测

| 路径 | 实测结果 |
|---|---|
| 直接动手 | `kill`：队长真实死亡，进入无名册杀结局 |
| 听完陈述后杀 | `kill_after_truth`：获得真相，队长真实死亡，进入名册被血浸透结局 |
| 听完陈述后放走 | `spare`：队长存活，进入名册指向幕后主使结局 |

三条路径均验证：三条线索按序登记、队长实际走到井边、玩家近距自动触发对峙、结局后 `timeScale=1` 且主角控制恢复。

## 功能与负例测试

| 测试 | 结果 |
|---|---|
| 剧情资产结构 | `STORY_ASSET_TESTS PASS clues=3` |
| 字幕/对话/结局 UI | `STORY_UI_TESTS PASS`；timeScale=0 下可推进 |
| 剧情状态机 | 线索幂等、非法线索拒绝、Opening/线索未齐禁止对峙、连续对话嵌套暂停恢复均通过 |
| NPC/拾取/偷听 | 距离负例、无选项对话关闭、拾取幂等、偷听连续 3 秒阈值通过 |
| 剧情目标 | 队长未解锁不可 E 刺杀；剧情走位暂停旧 AI；4m 外不对峙、近距自动对峙；HUD 文本正确 |
| 内存检查点 | 重载后线索=1、阶段正确、家书不复活、不重复开场 |
| 真实死亡重开 | 致命伤害→死亡面板→重新挑战；主角 100 血且可控、线索恢复、timeScale=1 |

## 旧玩法回归

以下 12 项均在独立 Play 中通过，日志保存在 `C:\Users\22492\.claude\tools\story\regression\`：

1. P1StealthTests
2. P1GuardSensesTests
3. P1ArcherEventOnlyTests
4. DirectMovementTests
5. DirectAttackTests
6. AssassinationTests
7. PotionRuntimeTests
8. SmokeTests
9. FullHudTests
10. PauseMenuTests
11. DeathFlowTests
12. SceneSwitchProbe（返回后 Hero/Enemy 均存在）

旧刺杀探针在「剧情禁用」测试模式下临时放开剧情目标限制；正式剧情中的队长刺杀保护由独立测试验证，二者未混淆。

## 实施中发现并修复的问题

1. **剧情启动早于角色初始化**：开场暂停可能使主角 HP 尚未由 `Start` 赋值；改为延迟一帧启动剧情。
2. **连续对话嵌套暂停**：第二次暂停会覆盖原始启用状态；改为只在首次暂停捕获，所有出口恢复。
3. **剧情禁用后的静态实例残留**：导致 HUD 误认为剧情 UI 仍打开、屏蔽刺杀提示；`OnDisable` 现在恢复游戏并清空 `Instance`。
4. **按钮白底浅字**：剧情选项和结局按钮改为深色底，截图复核可读。
5. **截图错位**：截图请求后同帧推进导致拍到下一阶段；改为稳定帧后再推进并重拍。

## 视觉与持久化

截图位于 `docs/verification/2026-11-12-story/`：
- `story-opening.png`
- `story-dialogue.png`
- `story-confront.png`
- `story-ending-subtitle.png`
- `story-ending-panel.png`

已逐张读图确认内容对应、文字清晰。磁盘重载场景后：章节与对话引用、NPC、家书、偷听区、队长 `StoryTarget`、井边点、旧任务禁用状态均保留，场景无未保存修改。最终编译 **0 错误 / 0 警告**。

## 备份与回滚

- 脚本备份：`D:\Unity\backups\RPG_Project_2026-11-12_story\`
- 场景备份：`RPG游戏.before-story.unity`
- 备份 MD5：`34b36ce3c1f14123af1536d0ff88c5b3`
- 工程无 Git，未提交。

## 保留事项与未覆盖范围

- 真实键鼠手感仍需用户亲自试玩；探针调用交互 API，不等于完整真人键鼠验收。
- 潜行被发现时间仍沿用 P1 实测平衡（6m 正常 0.875s、潜行 1.345s、隐藏 3.493s），本次未调整数值。
- 场景切换仍有既有 Progressive GPU、地形树 Soft Occlusion、Unknown Missing Script 提示；本报告不宣称已清零。
- 无配音、无运镜、无磁盘存档、无多章节，均按已批准范围保留到后续。
- 本轮没有新增需要用户确认的危险操作，也没有覆盖既有 `ArcherEventOnlyTest.cs`。
