# 《影村》剧情化改造：第一章《回村的人》设计

日期：2026-11-11
状态：故事方向与实现路线已获用户确认（复仇与刺杀 / 一章完整切片 / 对话框+黑场字幕 / 杀·放双结局）
工程：`D:\Unity\projects\RPG_Project`（无 Git，按惯例以文件备份与场景保存留档，不提交）

## 1. 目标与非目标

把当前「取卷轴 → 刺杀/避开卫兵 → 撤离结算」的搜打撤式关卡，改造成**剧情驱动**的复仇刺杀第一章。
潜行、刺杀、弓箭手、烟雾、药水等已有系统保留，作为服务剧情的手段。

**做**：一章完整切片（开场、3 条线索、对峙、杀/放双结局、下一章钩子）、轻量剧情运行时、对话框与黑场字幕、本章检查点。
**不做**：配音、镜头演出、声望/善恶值、多章节、开放世界、NPC 日常作息、新美术素材、装备成长。

## 2. 剧情内容（已定稿方向）

主角是三年前影村大火的幸存者，回村找当年带队封门的**卫兵队长**复仇。

NPC：村口老药师（证人）、年轻卫兵（偷听对象）、卫兵队长（本章目标）。

节拍：
1. 黑场字幕：火灾与回村。
2. 村口老药师对话（教学 E 对话）：队长夜里独自巡逻，藏着不敢寄出的信。
3. 调查三条线索：老药师证词（对话）／废墟家书（拾取）／兵营外偷听年轻卫兵与同伴交谈（潜行接近且未被发现）。
   偷听给出反转：封门令来自"上面"，队长只是执行者。
4. 线索齐 → 队长深夜独自到井边忏悔，形成刺杀窗口。
5. 对峙：靠近触发对话而非秒杀，他认出主角 → 选「动手」或「听他说完」。
   **选「动手」：立即进入结局·杀（无名册），真相随他死去。**
6. 选「听他说完」：他交出幕后主使名册 → 最终抉择杀 / 放。
7. 结局·杀（听完后杀）：名册被血浸透只剩一个名字，村庄搜捕加紧。
8. 结局·放：他留下名册离去，名册指向幕后主使。
   钩子：直接动手=幕后名字随他死去；听完后杀=名册残存的一个名字；放走=名册指向幕后主使。

即结局只有杀 / 放两类，但**杀有两版字幕**：直接动手（无名册）与听完后杀（名册被血浸透）。

## 3. 架构

新增一层剧情运行时，与现有玩法脚本通过小接口通信，不改动伤害/感知公式。

```
ChapterDefinition (ScriptableObject, 章节数据)
DialogueAsset     (ScriptableObject, 对话数据)
        ↓ 读取
StoryDirector (场景内唯一，剧情状态机 + 线索登记 + 检查点)
        ↓ 调用
DialoguePanel / SubtitleOverlay / EndingPanel (UI)
        ↓ 目标文本
GameHUD
        ↑ 交互
NpcInteractor / CluePickup / EavesdropZone / StoryTarget
```

### 3.1 数据资产

`DialogueAsset`
- `id`：唯一字符串，如 `elder_intro`、`captain_confront`。
- `lines[]`：`(speaker, text)`，顺序播放。
- `choices[]`：`(choiceId, text, nextDialogueId, actionId)`，可为空（纯线性对话）。
  `actionId` 是剧情动作标识，如 `give_clue_elder`、`open_final_choice`、`ending_kill`、`ending_spare`，由 StoryDirector 解释。

`ChapterDefinition`
- `chapterId`：`chapter_01`。
- `openingSubtitles[]`：开场黑场字幕行。
- `requiredClueIds[]`：`elder_testimony`、`family_letter`、`barracks_eavesdrop`。
- `confrontDialogueId`、`finalChoiceDialogueId`。
- `killEndingSubtitles[]`（直接动手，无名册）、`killAfterTruthEndingSubtitles[]`（听完后杀，名册被血浸透）、`spareEndingSubtitles[]`。
- 目标名、NPC 显示名等文本字段。

资产放 `Assets/Story/`。`StoryDirector` 通过场景内引用持有本章 `ChapterDefinition`，对话资产由章节引用，避免运行时按路径查找。

### 3.2 StoryDirector

状态：`Opening → Investigate → ConfrontReady → Confront → Ending`。
职责：
- 推进状态、登记线索 `RegisterClue(string id)`（重复登记幂等）、判断 `requiredClueIds` 是否集齐。
- 打开/关闭对话与字幕，期间暂停游戏（见 §5）。
- 向 `GameHUD` 提供当前目标文本与线索计数 `n/3`。
- 解释对话 `actionId`，触发队长走位、最终抉择与结局。
- 保存/恢复本章检查点（见 §6）。
- 资产缺失或引用断裂时 `Debug.LogError` 并跳过该拍，**绝不让玩家卡在无法推进的状态**；无法推进时回退到可继续的探索状态并提示。

### 3.3 交互组件

- `NpcInteractor`：挂在 NPC 上，`range=2m`，E 触发指定 `DialogueAsset`；对话结束回调 StoryDirector。
- `CluePickup`：废墟家书，E 拾取，`RegisterClue` 后隐藏自身。沿用现有 `IntelPickup` 的交互形式，但不再驱动旧任务。
- `EavesdropZone`：兵营外偷听区，玩家在区内且未处于 Chase/Seen 状态累计 3 秒 → 登记线索；被打断则重新累计。
- `StoryTarget`：标记本章剧情目标（队长）。提供 `SetScriptedPassive(bool)`（忏悔阶段不主动警戒）、`MoveToStoryPoint(Transform)`（前往井边）、`BeginConfront()`（停 AI 并进入对峙）。

## 4. 对现有脚本的改动

| 脚本 | 改动 |
|---|---|
| `MissionController` | 本章不再作为结束条件来源；目标文本改由 StoryDirector 提供。`TryExtract` 撤离胜利流程在本章停用。 |
| `IntelCrate` / `MissionEscapePortal` | 场景中停用（家书另用 `CluePickup`），保留对象以便回滚。 |
| `AssassinationController` | `IsEligible` 增加一条：目标带 `StoryTarget` 且未进入最终抉择的"允许动手"状态时返回 false，防止通用 E 刺杀跳过剧情秒杀队长。普通卫兵刺杀不变。 |
| `GuardSenses` / `EnemyController` | 不改感知与伤害逻辑；队长由 `StoryTarget` 在剧情需要时暂停 AI。 |
| `GameHUD` | 目标文本与线索计数改由 StoryDirector 驱动，其余 HUD 不动。 |
| `HeroController` / `HeroDirectMovement` | 不改移动与攻击；对话与字幕期间由 StoryDirector 统一禁用输入。 |

上一验收的中等项"出生点无安全区"在本章编排中一并处理：村口老药师一带设为安全区，最近巡逻路径不覆盖开场对话位置。

## 5. UI 与暂停规则

- `DialoguePanel`：底部面板，说话者名 + 正文 + 最多 2 个选项按钮 + 继续提示（E/左键）。用 `RpgUI` 与 Fantasy RPG UI 现有底图。
- `SubtitleOverlay`：全屏黑底 + 居中文字 + 按任意键继续，支持多行顺序播放。
- `EndingPanel`：结局字幕 + 重新挑战 / 返回主界面，复用现有按钮样式。
- **暂停规则**：对话与字幕期间 `Time.timeScale=0`、禁用 `HeroController` 输入与敌人 AI；结束时**所有出口都恢复 `Time.timeScale=1`**（含对话被结局打断、场景重载、返回主界面），这是既有踩过的坑。

## 6. 检查点与死亡

- 死亡 → 重开本章场景，但**线索与剧情阶段恢复到死亡前快照**：快照记录 `stage`、已登记线索、已完成对话 id。
- 场景重载后 `StoryDirector` 依快照重新应用：已完成对话的 NPC 不重复给线索、已拾取家书保持隐藏、队长按阶段就位。
- 快照仅存活于本次运行（静态运行时数据），不做磁盘存档；磁盘存档属于后续阶段，不在本章范围。

## 7. 测试方案

沿用已验证的验收方法：editor 先磁盘重载场景 + `Time.timeScale=0` → `manage_editor play` → 探针等初始化完成再恢复时间，避免探针编译期间主角被巡逻卫兵打死。

- **编辑态资产校验**：对话 `nextDialogueId` 闭合、章节引用的对话与线索 id 存在、`requiredClueIds` 与场景内线索组件一一对应。
- **Play 逐拍探针**：开场字幕 → 老药师对话 → 三线索登记 → 队长就位 → 对峙 → 三条路径各跑一遍：直接动手（杀·无名册）、听完后杀、听完后放。
- **负例**：线索未齐不能触发对峙；未「听他说完」不会获得名册；重复登记同一线索计数不增加；对话中玩家不能移动且 `timeScale=0`；结局后 `timeScale=1`。
- **检查点**：调查中途死亡重开后，线索计数与阶段与快照一致。
- **回归**：潜行、普通卫兵刺杀、弓箭、烟雾、药水、HUD、暂停、场景往返保持通过；队长不会被通用 E 刺杀秒杀。

## 8. 实施分段（供后续计划展开）

- S1：剧情运行时与 UI（资产类型、StoryDirector、对话框、黑场字幕、目标 HUD 接入）。
- S2：第一章内容接入（NPC、三条线索、偷听区、队长走位与对峙、双结局）。
- S3：检查点、开场安全区、回归与独立验收。

每段结束保存场景、备份脚本并更新 `docs/progress.md`；涉及危险或需用户确认的操作跳过并单列。
