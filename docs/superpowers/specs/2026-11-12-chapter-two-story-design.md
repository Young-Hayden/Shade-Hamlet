# 《影村》第二章《签名的人》设计

日期：2026-11-12
状态：剧情方向、同村第二天、公开真相/执行私刑高潮已获用户批准
前置：第一章《回村的人》已实施并验收；工程无 Git，不提交。

## 1. 目标与范围

在现有村庄与剧情运行时上制作第二章，承接第一章三种结果，让玩家从「向执行者复仇」推进到「追查并公开封门令的签名者」。

**做**：上章结局承接、同村第二天、三条新调查线索、书记官沈砚对峙、公开真相/执行私刑双结局、章节继续按钮、第二章死亡恢复。
**不做**：新地图、开放世界、配音、运镜、磁盘存档、声望系统、通用任务图编辑器、第三套完整章节分支。

## 2. 第一章承接规则

`StoryDirector.EndingId` 的第一章结果作为第二章唯一连续性输入：

| 第一章结果 | 第二章开场与信息 |
|---|---|
| `kill`（直接动手） | 队长已死亡；主角只知道封门令还有第二个签名 |
| `kill_after_truth`（听完后杀） | 队长已死亡；血浸透的名册只剩一个姓氏 |
| `spare`（放走） | 名册完整；队长活着，并可作为非必要证人再对话一次 |

三条路径都指向村公所书记官**沈砚**。信息差异影响开场字幕与可用证人，不改变线索数量、不锁死本章内容。

第二章结束后不复活第一章队长。队长在两条杀结局中保持隐藏；仅在 `spare` 中保留为被动证人。

## 3. 本章剧情

### 3.1 开场

同村第二天，村庄开始搜查。老药师提醒主角：真正需要找的是签下命令的人，而不只是执行命令的人。

### 3.2 三条线索

1. `elder_second_testimony`：与老药师对话，得知村公所旧账记录了火灾当晚的封门调度。
2. `ledger_page`：在村公所取得旧账页，发现书记官沈砚的副签。
3. `archive_eavesdrop`：在档案室外连续偷听 3 秒且未被发现，得知沈砚保留了未经改写的原始封门令。

线索登记幂等；三线索集齐后，沈砚前往村中的公开告示点，形成对峙窗口。

### 3.3 对峙与真相

沈砚承认副签，但声称自己若拒签，火灾会殃及更多村民。他保留原始命令，既能证明自己的责任，也能指向真正的主使。

玩家先选「听他说完」或「立即动手」；立即动手直接进入私刑结局。听完后进入最终抉择：

- `expose`：**公开真相**。把原始命令交给村民，公开签名与封门经过。结局字幕引向幕后主使的反击。
- `execute`：**执行私刑**。沈砚真实死亡，原始命令失去公开机会。结局字幕引向尚未烧尽的第三签名。

本章结局类型为 `expose` / `execute`，不占用第一章的 `kill` / `kill_after_truth` / `spare`。

## 4. 架构与数据

### 4.1 章节与结局资产

扩展现有 `ChapterDefinition`，不重写第一章：

```csharp
public ChapterDefinition nextChapter;
public OpeningVariant[] openingVariants;
public EndingDefinition[] endings;
```

- `OpeningVariant`：`previousEndingId`、`string[] subtitles`。
- `EndingDefinition`：`id`、`string[] subtitles`、`bool killTarget`。
- 第一章现有 `openingSubtitles` 与三组结局数组继续作为兼容回退。
- `chapter_02.nextChapter` 为空，结局面板不显示后续章节按钮。

新增资产：`chapter_02`、`elder_second_day`、`ledger_page`、`archive_whisper`、`shen_confront`、`shen_final_choice`、`captain_witness`。

### 4.2 会话连续性

新增轻量静态运行时数据 `StorySession`：

```csharp
public static string PendingChapterId;
public static string PreviousEndingId;
```

- 结局面板新增「继续第二章」；只有 `chapter.nextChapter != null` 时显示。
- 继续时写入 `PendingChapterId` 与 `PreviousEndingId`，清空死亡快照，重载 `RPG游戏` 场景。
- `StoryDirector` 初始化时按 `PendingChapterId` 选择本章资产；没有待续章节则使用场景默认第一章。
- 返回主界面或开始新游戏时清空 `StorySession` 与 `StoryCheckpoint.Current`，避免跨新游戏污染。

### 4.3 结局与剧情目标

`StoryDirector.ApplyAction` 支持通用 `ending_<id>`，不再为每章新增硬编码分支；第一章 `ending_kill` 仍保留「是否听过真相」的既有选择逻辑。

`ShowEnding` 优先读取 `EndingDefinition`：
- `killTarget=true`：调用 `StoryTarget` 对当前目标执行真实伤害结算。
- `killTarget=false`：目标存活；公开线由字幕说明结果，不新增逮捕 AI。

`StoryTarget` 保留近距自动对峙、剧情走位、动画维护与通用刺杀保护。

## 5. 场景接入

同一天复用 `RPG游戏` 场景，通过 Setup 脚本接入第二章：

- `NpcElder`：保留，替换为第二章对话。
- `ClueLedgerPage`：村公所旧账页，显示对话内容并登记线索。
- `EavesdropArchive`：档案室偷听区，连续 3 秒未被发现后播放偷听内容。
- `NpcClerk`：沈砚，复用现有角色模型，带被动态敌人组件与 `StoryTarget`，仅用于对峙和结局结算。
- `StoryPoint`：本章改为公开告示点附近位置。
- 第一章 `ClueFamilyLetter`、`EavesdropBarracks` 在第二章停用，避免重复登记旧线索。
- 原队长仅在 `spare` 结果下作为 `NpcCaptainWitness` 出现；其余结果隐藏。其旧 `StoryTarget` 不再参与第二章对峙。
- 弓箭手与既有潜行环境保留，巡逻安全区沿用第一章。

Setup 脚本幂等，执行前备份场景；不删除旧剧情对象。

## 6. 失败与边界处理

- 缺少 `OpeningVariant`：使用本章默认 `openingSubtitles`，不阻塞。
- 缺少 `nextChapter`：不显示继续按钮，仍保留重新挑战与返回主界面。
- 非法线索、重复线索、线索未齐对峙、未听完不能进入最终抉择：沿用第一章约束。
- 第二章死亡：快照额外记录 `chapterId`；重载只恢复当前章节，不回退第一章。
- 对话嵌套暂停、`timeScale` 恢复、场景重载、`StoryDirector.Instance` 清理沿用第一章修复。
- 新游戏必须清空上章结局；否则会出现队长错误复活或第二章错误开场。

## 7. 测试与验收

- **承接**：分别从第一章 `kill`、`kill_after_truth`、`spare` 进入第二章，核对章节 id、开场变体、队长显示/隐藏和证人可用性。
- **本章流程**：三线索、沈砚走位、近距自动对峙、立即私刑、听完后公开、听完后私刑。
- **结局世界状态**：`execute` 时沈砚真实死亡；`expose` 时沈砚存活；所有结局后 `timeScale=1`、主角可控制。
- **死亡恢复**：第二章调查中途死亡后仍为 `chapter_02`，线索和阶段恢复，不重复开场。
- **新游戏复位**：返回主界面后再次开始必须进入第一章，无第二章会话残留。
- **回归**：第一章三路径与既有 12 项旧玩法回归继续通过；旧 `MissionTests` 仍废弃。
- **视觉与持久化**：截取第二章开场、老药师对话、沈砚对峙、两种结局；磁盘重载检查章节、NPC、线索和队长连续性。
- 真人键鼠与剧情阅读体验仍需用户试玩。

## 8. 实施分段

1. 会话/章节/通用结局数据与第一章兼容测试。
2. 第二章资产与场景接入、三条承接路径。
3. 第二章双结局、死亡恢复、新游戏复位。
4. 全量回归、视觉、磁盘重载、报告与进度落盘。
