# 《影村》第二章《签名的人》实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在第一章三种结局之后接入同村第二天的第二章，完成三线索调查、沈砚对峙、公开真相/执行私刑双结局，并保持死亡恢复与旧玩法回归。

**Architecture:** 扩展现有 `ChapterDefinition`/`StoryDirector`，用 `StorySession` 保存待进入章节与上章结局；新增 `ChapterTwoContinuity` 管理同场景的两章对象和队长连续性。结局数据化，继续按钮驱动同场景重载，不新增地图或叙事插件。

**Tech Stack:** Tuanjie 1.8.3（2022.3.62t5）、C#、uGUI、`RpgUI`、现有 Bridge 探针；无 Git。

## Global Constraints

- 设计依据：`docs/superpowers/specs/2026-11-12-chapter-two-story-design.md`。不要新增开放世界、配音、运镜、磁盘存档、声望系统或通用节点编辑器。
- **工程无 Git**：所有提交步骤替换为「备份→保存场景→更新 `docs/progress.md`」。备份目录 `D:\Unity\backups\RPG_Project_2026-11-12_chapter2\`。
- 连接与编译：`node C:/Users/22492/.claude/tools/rpg-bridge.mjs manage_editor '{"action":"get_state"}'`；编译用 `request_compile`，期望 `errors:0`。
- Play 探针必须先编辑态运行 `C:\Users\22492\.claude\tools\story\StoryPreparePlay.cs`，再 `manage_editor '{"action":"play"}'`，最后在 play 模式运行探针。遇 `domain reloading`/`assembly reload` 等瞬时错误，等待 3 秒重试；不得把未执行当通过。
- 新测试放 `C:\Users\22492\.claude\tools\story\`，文件名 `Chapter2*Tests.cs`，结果写同目录 `.result.txt`；不要覆盖既有测试。
- 每个异步探针须 `EditorApplication.update += tick`，结束时 `Time.timeScale=1`、`EditorApplication.ExitPlaymode()`，结果落盘；PASS/FAIL 均以文件和日志双重确认。
- 剧情启动保持延迟一帧；暂停嵌套只在首次捕获原始 enabled；禁用剧情物体必须恢复控制并清理 `StoryDirector.Instance`。
- 截图请求后至少等待 0.5 秒稳定帧再推进，截图保存 `docs/verification/2026-11-12-chapter2/`。
- 旧 `MissionTests` 仍废弃。危险确认或权限拒绝事项跳过并记录，不代为批准。
- 每完成一个任务写一次 `docs/progress.md`。

---

### Task 1: 章节数据、会话状态与第二章初始化选择

**Files:**
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\ChapterDefinition.cs`
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\StorySession.cs`
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\StoryDirector.cs`
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\CLick.cs`
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\GameFlow.cs`
- Test: `C:\Users\22492\.claude\tools\story\Chapter2SessionTests.cs`

**Interfaces:**
- Produces: `ChapterDefinition.nextChapter/chapterTitle/openingVariants/endings`、`OpeningVariant(previousEndingId,subtitles)`、`EndingDefinition(id,subtitles,killTarget)`；`StorySession.PendingChapterId/PreviousEndingId/Reset()`。
- Consumes: 现有 `StoryDirector.chapter/Start/InitializeStory/PlayOpening`。

- [ ] **Step 1: 写失败测试**

```csharp
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

try
{
    StorySession.Reset();
    StorySession.PendingChapterId = "chapter_02";
    StorySession.PreviousEndingId = "spare";
    var first = ScriptableObject.CreateInstance<ChapterDefinition>();
    first.chapterId = "chapter_01";
    var second = ScriptableObject.CreateInstance<ChapterDefinition>();
    second.chapterId = "chapter_02";
    second.chapterTitle = "第二章 完";
    second.openingSubtitles = new[] { "默认第二天" };
    second.openingVariants = new[] {
        new ChapterDefinition.OpeningVariant { previousEndingId = "spare", subtitles = new[] { "队长留下了名册。" } }
    };
    second.endings = new[] {
        new ChapterDefinition.EndingDefinition { id = "expose", subtitles = new[] { "真相公开。" }, killTarget = false },
        new ChapterDefinition.EndingDefinition { id = "execute", subtitles = new[] { "私刑执行。" }, killTarget = true }
    };
    first.nextChapter = second;
    if (first.nextChapter.chapterTitle != "第二章 完") throw new Exception("FAIL: 下一章引用");
    if (second.openingVariants[0].previousEndingId != "spare") throw new Exception("FAIL: 开场变体");
    if (!second.endings[1].killTarget) throw new Exception("FAIL: 结局目标结算标记");
    StorySession.Reset();
    if (StorySession.PendingChapterId != null || StorySession.PreviousEndingId != null || StoryCheckpoint.Current != null)
        throw new Exception("FAIL: 新游戏会话未清空");
    File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2SessionTests.result.txt", "PASS data/session reset");
    Debug.Log("CHAPTER2_SESSION_TESTS PASS");
}
catch (Exception ex)
{
    File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2SessionTests.result.txt", "FAIL " + ex.Message);
    Debug.LogException(ex);
}
```

- [ ] **Step 2: 编辑态运行，确认失败**

Run: `node C:/Users/22492/.claude/tools/rpg-bridge.mjs execute_csharp_script '{"script_path":"C:/Users/22492/.claude/tools/story/Chapter2SessionTests.cs","execution_mode":"editor","capture_logs":true}'`
Expected: `CS0246`，`StorySession`/`OpeningVariant` 不存在。

- [ ] **Step 3: 完整替换 `ChapterDefinition.cs`**

```csharp
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Story/Chapter", fileName = "Chapter")]
public class ChapterDefinition : ScriptableObject
{
    [Serializable]
    public class OpeningVariant
    {
        public string previousEndingId;
        [TextArea(2, 4)] public string[] subtitles = new string[0];
    }

    [Serializable]
    public class EndingDefinition
    {
        public string id;
        [TextArea(2, 4)] public string[] subtitles = new string[0];
        public bool killTarget;
    }

    public string chapterId = "chapter_01";
    public string chapterTitle = "第一章 完";
    [TextArea(2, 4)] public string[] openingSubtitles = new string[0];
    public string[] requiredClueIds = { "elder_testimony", "family_letter", "barracks_eavesdrop" };
    public DialogueAsset confrontDialogue;
    public DialogueAsset finalChoiceDialogue;
    [TextArea(2, 4)] public string[] killEndingSubtitles = new string[0];
    [TextArea(2, 4)] public string[] killAfterTruthEndingSubtitles = new string[0];
    [TextArea(2, 4)] public string[] spareEndingSubtitles = new string[0];
    public string targetDisplayName = "卫兵队长";
    public ChapterDefinition nextChapter;
    public OpeningVariant[] openingVariants = new OpeningVariant[0];
    public EndingDefinition[] endings = new EndingDefinition[0];
}
```

- [ ] **Step 4: 新建 `StorySession.cs`**

```csharp
public static class StorySession
{
    public static string PendingChapterId;
    public static string PreviousEndingId;

    public static void Reset()
    {
        PendingChapterId = null;
        PreviousEndingId = null;
        StoryCheckpoint.Current = null;
    }
}
```

- [ ] **Step 5: 接入章节选择与开场变体**

在 `StoryDirector.Start()` 最前面（创建 UI 之前）加入：

```csharp
        SelectPendingChapter();
```

在 `InitializeStory()` 之前加入：

```csharp
    private void SelectPendingChapter()
    {
        string pending = StorySession.PendingChapterId;
        if (string.IsNullOrEmpty(pending) || chapter == null) return;
        if (chapter.chapterId == pending) return;
        if (chapter.nextChapter != null && chapter.nextChapter.chapterId == pending)
            chapter = chapter.nextChapter;
        else
            Debug.LogError("StoryDirector: 场景默认章节无法到达待续章节 " + pending);
    }

    private string[] GetOpeningLines()
    {
        if (chapter == null) return new string[0];
        foreach (var variant in chapter.openingVariants)
            if (variant != null && variant.previousEndingId == StorySession.PreviousEndingId)
                return variant.subtitles;
        return chapter.openingSubtitles;
    }
```

把 `PlayOpening()` 中 `subtitles.Show(...)` 的第一个参数改为：

```csharp
        subtitles.Show(GetOpeningLines(), () =>
```

在 `CLick.ButtonClick()` 的 `LoadScene` 前、`GameFlow.ReturnToMain()` 的 `LoadScene` 前分别加入：

```csharp
        StorySession.Reset();
```

- [ ] **Step 6: 编译并跑测试**

Run: `request_compile`，再执行 Step 2 命令。
Expected: `CHAPTER2_SESSION_TESTS PASS`；编译 `errors:0`。

- [ ] **Step 7: 备份并更新进度**

备份 5 个涉及脚本；`docs/progress.md` 勾选 Task 1。

---

### Task 2: 通用结局、真实目标结算与「继续第二章」

**Files:**
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\StoryDirector.cs`
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\StoryTarget.cs`
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\EndingPanel.cs`
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\StoryCheckpoint.cs`
- Test: `C:\Users\22492\.claude\tools\story\Chapter2EndingTests.cs`

**Interfaces:**
- Produces: `StoryDirector.ContinueToNextChapter(): bool`、`EndingPanel.Show(string title, bool showContinue)`、`EndingPanel.continueButton`、`StoryTarget.ApplyStoryEnding(string endingId, bool killTarget)`。
- Consumes: Task 1 的 `nextChapter/chapterTitle/endings` 与 `StorySession`。

- [ ] **Step 1: 写失败测试**

```csharp
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

try
{
    var chapter = ScriptableObject.CreateInstance<ChapterDefinition>();
    chapter.chapterId = "chapter_02";
    chapter.chapterTitle = "第二章 完";
    chapter.endings = new[] {
        new ChapterDefinition.EndingDefinition { id = "expose", subtitles = new[] { "真相公开。" }, killTarget = false }
    };
    var next = ScriptableObject.CreateInstance<ChapterDefinition>();
    next.chapterId = "chapter_03";
    chapter.nextChapter = next;
    var director = new GameObject("EndingTest").AddComponent<StoryDirector>();
    director.chapter = chapter;
    typeof(StoryDirector).GetProperty("EndingId").SetValue(director, "expose", null);
    if (!director.ContinueToNextChapter()) throw new Exception("FAIL: 有下一章却无法继续");
    if (StorySession.PendingChapterId != "chapter_03") throw new Exception("FAIL: 待续章节错误");
    if (StorySession.PreviousEndingId != "expose") throw new Exception("FAIL: 上章结局未传递");
    var panel = EndingPanel.Create(null);
    panel.Show("第二章 完", true);
    if (!panel.continueButton.gameObject.activeSelf) throw new Exception("FAIL: 继续按钮未显示");
    panel.Show("终章", false);
    if (panel.continueButton.gameObject.activeSelf) throw new Exception("FAIL: 无下一章仍显示继续");
    UnityEngine.Object.DestroyImmediate(panel.gameObject);
    StorySession.Reset();
    File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2EndingTests.result.txt", "PASS generic ending/continue");
    Debug.Log("CHAPTER2_ENDING_TESTS PASS");
}
catch (Exception ex)
{
    StorySession.Reset();
    File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2EndingTests.result.txt", "FAIL " + ex.Message);
    Debug.LogException(ex);
}
```

此测试在**编辑态**运行；`ContinueToNextChapter` 会请求加载场景，这是预期副作用，测试后由 `StoryPreparePlay`/持久化探针重新打开正确场景。

- [ ] **Step 2: 运行确认失败**

Expected: `ContinueToNextChapter`、`Show(string,bool)` 或 `continueButton` 不存在。

- [ ] **Step 3: 通用化 `StoryDirector.ApplyAction`**

保留 `give_clue_elder`，把 `learn_truth`、`ending_kill`、`ending_spare` 分支替换为：

```csharp
            case "learn_truth":
                TruthLearned = true;
                OpenDialogue(chapter != null ? chapter.finalChoiceDialogue : null);
                return;
            default:
                if (actionId != null && actionId.StartsWith("ending_"))
                {
                    string endingId = actionId.Substring("ending_".Length);
                    if (endingId == "kill" && TruthLearned) endingId = "kill_after_truth";
                    ShowEnding(endingId);
                }
                else Debug.LogError("StoryDirector: 未知 actionId " + actionId);
                return;
```

- [ ] **Step 4: 数据化 `ShowEnding` 并接入继续**

完整替换 `ShowEnding`：

```csharp
    public void ShowEnding(string endingId)
    {
        EndingId = endingId;
        Stage = StoryStage.Ending;
        string[] lines = new string[0];
        bool killTarget = false;
        ChapterDefinition.EndingDefinition definition = null;
        if (chapter != null)
            foreach (var candidate in chapter.endings)
                if (candidate != null && candidate.id == endingId) { definition = candidate; break; }
        if (definition != null)
        {
            lines = definition.subtitles;
            killTarget = definition.killTarget;
        }
        else if (chapter != null)
        {
            if (endingId == "kill") { lines = chapter.killEndingSubtitles; killTarget = true; }
            else if (endingId == "kill_after_truth") { lines = chapter.killAfterTruthEndingSubtitles; killTarget = true; }
            else lines = chapter.spareEndingSubtitles;
        }
        if (target != null) target.ApplyStoryEnding(endingId, killTarget);
        SetGameplayPaused(true);
        subtitles.Show(lines, () =>
        {
            SetGameplayPaused(false);
            ending.Show(chapter != null ? chapter.chapterTitle : "本章 完", chapter != null && chapter.nextChapter != null);
        });
    }

    public bool ContinueToNextChapter()
    {
        if (chapter == null || chapter.nextChapter == null) return false;
        StorySession.PendingChapterId = chapter.nextChapter.chapterId;
        StorySession.PreviousEndingId = EndingId;
        StoryCheckpoint.Current = null;
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        return true;
    }
```

- [ ] **Step 5: 扩展 `StoryTarget` 结局结算**

保留原 `ApplyStoryEnding(string)` 作为兼容入口，并实现重载：

```csharp
    public void ApplyStoryEnding(string endingId)
    {
        ApplyStoryEnding(endingId, endingId == "kill" || endingId == "kill_after_truth");
    }

    public void ApplyStoryEnding(string endingId, bool killTarget)
    {
        allowKill = killTarget;
        if (killTarget)
        {
            if (hero == null) hero = FindObjectOfType<HeroController>();
            if (controller != null && !controller.isDeath)
                controller.Hurt(controller.MaxHP, hero != null ? hero.transform : null);
            return;
        }
        SetScriptedPassive(true);
        if (endingId != "spare") return;   // 公开真相时沈砚留在现场，不套用队长离开行为
        if (hero == null) hero = FindObjectOfType<HeroController>();
        if (hero != null && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            Vector3 away = transform.position - hero.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = transform.forward;
            agent.isStopped = false;
            agent.SetDestination(transform.position + away.normalized * 8f);
        }
    }
```

- [ ] **Step 6: 完整替换 `EndingPanel.cs`**

```csharp
using UnityEngine;
using UnityEngine.UI;

public class EndingPanel : MonoBehaviour
{
    public bool IsShowing { get; private set; }
    public Button continueButton;
    private Text titleText;

    public static EndingPanel Create(Transform parent)
    {
        var canvas = RpgUI.CreateCanvas("StoryEndingCanvas", parent, true);
        var panel = canvas.gameObject.AddComponent<EndingPanel>();
        var bg = RpgUI.CreateImage("Panel", canvas.transform, null, new Color(0.06f, 0.05f, 0.05f, 0.97f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 330f));
        panel.titleText = RpgUI.CreateText("Title", bg.transform, "本章 完", 42, new Color(1f, 0.9f, 0.72f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 105f), new Vector2(540f, 70f));
        panel.continueButton = RpgUI.CreateButton("ContinueButton", bg.transform, null, "继续下一章", new Vector2(0f, 25f), new Vector2(240f, 60f));
        panel.continueButton.image.color = new Color(0.16f, 0.11f, 0.08f, 0.98f);
        panel.continueButton.onClick.AddListener(() => { if (StoryDirector.Instance != null) StoryDirector.Instance.ContinueToNextChapter(); });
        var restartButton = RpgUI.CreateButton("RestartButton", bg.transform, null, "重新挑战", new Vector2(-135f, -75f), new Vector2(220f, 64f));
        restartButton.image.color = new Color(0.16f, 0.11f, 0.08f, 0.98f);
        restartButton.onClick.AddListener(() => { if (GameFlow.Instance != null) GameFlow.Instance.RestartGame(); });
        var mainMenuButton = RpgUI.CreateButton("MainMenuButton", bg.transform, null, "返回主界面", new Vector2(135f, -75f), new Vector2(220f, 64f));
        mainMenuButton.image.color = new Color(0.16f, 0.11f, 0.08f, 0.98f);
        mainMenuButton.onClick.AddListener(() => { if (GameFlow.Instance != null) GameFlow.Instance.ReturnToMain(); });
        canvas.gameObject.SetActive(false);
        return panel;
    }

    public void Show(string title) { Show(title, false); }

    public void Show(string title, bool showContinue)
    {
        if (titleText != null) titleText.text = title;
        continueButton.gameObject.SetActive(showContinue);
        gameObject.SetActive(true);
        IsShowing = true;
        Time.timeScale = 1f;
    }
}
```

- [ ] **Step 7: 给检查点增加章节 id**

`StoryCheckpoint` 添加 `public string chapterId;`；`CaptureCheckpoint()` 初始化器中增加：

```csharp
            chapterId = chapter != null ? chapter.chapterId : null,
```

`InitializeStory()` 的快照条件改为：

```csharp
        if (StoryCheckpoint.Current != null
            && StoryCheckpoint.Current.chapterId == (chapter != null ? chapter.chapterId : null)) ApplyCheckpoint();
        else { StoryCheckpoint.Current = null; PlayOpening(); }
```

- [ ] **Step 8: 编译、测试与第一章兼容回归**

Expected: `CHAPTER2_ENDING_TESTS PASS`；编译 `errors:0`。重跑 `StoryDirectorTests.cs` 与第一章 `StoryChapterOneTests.cs` 的 spare 路径，结果必须仍 PASS。

- [ ] **Step 9: 备份并更新进度**

备份修改脚本；`docs/progress.md` 勾选 Task 2。

---

### Task 3: 第二章剧情资产与同场景内容路由

**Files:**
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\ChapterTwoContinuity.cs`
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\StoryDirector.cs`
- Create: `C:\Users\22492\.claude\tools\story\SetupChapterTwo.cs`
- Create: `D:\Unity\projects\RPG_Project\Assets\Story\chapter_02.asset` 及六个第二章对话资产
- Modify: `D:\Unity\projects\RPG_Project\Assets\Scenes\RPG游戏.unity`
- Test: `C:\Users\22492\.claude\tools\story\Chapter2ContentTests.cs`

**Interfaces:**
- Produces: `ChapterTwoContinuity.Apply(StoryDirector)`；场景对象 `ChapterTwoContent`、`NpcClerk`、`ClueLedgerPage`、`EavesdropArchive`、`NpcCaptainWitness`（原队长的对话组件）。
- Consumes: `StorySession.PreviousEndingId`、`ChapterDefinition.nextChapter`、第一章现有场景对象。

- [ ] **Step 1: 备份场景并写失败测试**

先把 `RPG游戏.unity` 复制为 `D:\Unity\backups\RPG_Project_2026-11-12_chapter2\RPG游戏.before-chapter2.unity`，核对 MD5 一致。

`Chapter2ContentTests.cs`（编辑态）：

```csharp
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

try
{
    var chapter = AssetDatabase.LoadAssetAtPath<ChapterDefinition>("Assets/Story/chapter_02.asset");
    if (chapter == null || chapter.chapterId != "chapter_02") throw new Exception("FAIL: chapter_02 资产不存在");
    string[] expected = { "elder_second_testimony", "ledger_page", "archive_eavesdrop" };
    for (int i = 0; i < expected.Length; i++)
        if (chapter.requiredClueIds[i] != expected[i]) throw new Exception("FAIL: 第二章线索 id");
    if (chapter.confrontDialogue == null || chapter.finalChoiceDialogue == null) throw new Exception("FAIL: 沈砚对话缺失");
    if (chapter.openingVariants.Length != 3 || chapter.endings.Length != 2) throw new Exception("FAIL: 承接或结局数量");
    var first = AssetDatabase.LoadAssetAtPath<ChapterDefinition>("Assets/Story/chapter_01.asset");
    if (first.nextChapter != chapter) throw new Exception("FAIL: 第一章 nextChapter 未接线");
    var root = GameObject.Find("Story");
    var router = root.GetComponent<ChapterTwoContinuity>();
    if (router == null || router.chapterTwoRoot == null || router.clerk == null) throw new Exception("FAIL: 第二章路由未接线");
    if (router.chapterTwoRoot.activeSelf) throw new Exception("FAIL: 默认第一章时第二章内容不应预先启用");
    File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2ContentTests.result.txt", "PASS assets/router");
    Debug.Log("CHAPTER2_CONTENT_TESTS PASS");
}
catch (Exception ex)
{
    File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2ContentTests.result.txt", "FAIL " + ex.Message);
    Debug.LogException(ex);
}
```

Run 编辑态探针。Expected: 初次 FAIL，chapter_02 不存在。

- [ ] **Step 2: 新建 `ChapterTwoContinuity.cs`**

```csharp
using UnityEngine;

public class ChapterTwoContinuity : MonoBehaviour
{
    public GameObject chapterTwoRoot;
    public GameObject[] chapterOneClues = new GameObject[0];
    public StoryTarget firstCaptain;
    public StoryTarget clerk;
    public NpcInteractor captainWitness;
    public NpcInteractor elder;
    public DialogueAsset chapterOneElderDialogue;
    public DialogueAsset chapterTwoElderDialogue;

    public void Apply(StoryDirector director)
    {
        bool second = director.chapter != null && director.chapter.chapterId == "chapter_02";
        if (chapterTwoRoot != null) chapterTwoRoot.SetActive(second);
        foreach (var clue in chapterOneClues)
            if (clue != null) clue.SetActive(!second);
        if (elder != null)
            elder.dialogue = second ? chapterTwoElderDialogue : chapterOneElderDialogue;
        director.target = second ? clerk : firstCaptain;
        if (captainWitness != null) captainWitness.enabled = false;
        if (!second || firstCaptain == null) return;

        bool spared = StorySession.PreviousEndingId == "spare";
        firstCaptain.gameObject.SetActive(spared);
        firstCaptain.enabled = false;
        var controller = firstCaptain.GetComponent<EnemyController>();
        if (controller != null) controller.enabled = false;
        var senses = firstCaptain.GetComponent<GuardSenses>();
        if (senses != null) senses.enabled = false;
        if (captainWitness != null) captainWitness.enabled = spared;
    }
}
```

- [ ] **Step 3: 接入 StoryDirector 与通用线索 action**

`StoryDirector` 添加字段：

```csharp
    public ChapterTwoContinuity chapterTwoContinuity;
```

在 `Start()` 的 `SelectPendingChapter();` 后立即加入：

```csharp
        if (chapterTwoContinuity != null) chapterTwoContinuity.Apply(this);
```

`ApplyAction` 的 `default` 分支在「未知 actionId」日志前加入：

```csharp
                else if (actionId != null && actionId.StartsWith("clue:"))
                    RegisterClue(actionId.Substring("clue:".Length));
```

- [ ] **Step 4: 写完整 `SetupChapterTwo.cs`**

```csharp
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

const string Dir = "Assets/Story";
DialogueAsset.Line L(string speaker, string text) => new DialogueAsset.Line { speaker = speaker, text = text };
DialogueAsset.Choice C(string id, string text, string action) => new DialogueAsset.Choice { choiceId = id, text = text, actionId = action };
DialogueAsset Make(string name, DialogueAsset.Line[] lines, DialogueAsset.Choice[] choices)
{
    string path = Dir + "/" + name + ".asset";
    var asset = AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);
    if (asset == null) { asset = ScriptableObject.CreateInstance<DialogueAsset>(); AssetDatabase.CreateAsset(asset, path); }
    asset.id = name;
    asset.lines.Clear(); asset.lines.AddRange(lines);
    asset.choices.Clear(); asset.choices.AddRange(choices);
    EditorUtility.SetDirty(asset);
    return asset;
}

var elder2 = Make("elder_second_day", new[] {
    L("老药师", "村里已经开始搜人。你昨夜见过的队长，只是执行封门的人。"),
    L("老药师", "去村公所找旧账。封门调度有副签，那才是真正能追查的名字。")
}, new[] { C("accept", "我会找到签名的人。", "clue:elder_second_testimony") });
var letter2 = Make("ledger_page", new[] {
    L("旧账页", "火灾当晚，封门调度由书记官沈砚副签。墨迹比队长的名字更深。"),
    L("主角", "第二个签名，沈砚。")
}, new DialogueAsset.Choice[0]);
var whisper2 = Make("archive_whisper", new[] {
    L("档案室卫兵", "沈书记官还不肯烧掉原始封门令。"),
    L("年轻卫兵", "他说那是唯一能证明命令从哪里来、又要谁负责的东西。"),
    L("主角", "原始命令还在。我要让它见光。")
}, new DialogueAsset.Choice[0]);
var confront2 = Make("shen_confront", new[] {
    L("书记官沈砚", "你拿到旧账了。不错，副签是我。"),
    L("书记官沈砚", "如果我不签，那晚火会烧掉半座村。你可以恨我，但先想清楚真相值不值得活下来。")
}, new[] { C("execute", "立即动手", "ending_execute"), C("listen", "听他说完", "learn_truth") });
var final2 = Make("shen_final_choice", new[] {
    L("书记官沈砚", "原始封门令在这里。签名、指印、传令时辰，都在。"),
    L("书记官沈砚", "杀我很容易。让全村看见这张纸，比杀我难得多。")
}, new[] { C("expose", "公开真相", "ending_expose"), C("execute", "执行私刑", "ending_execute") });
var witness = Make("captain_witness", new[] {
    L("卫兵队长", "名册你留着。沈砚的副签不是终点，原始命令才能指到下令的人。"),
    L("卫兵队长", "这一次，别只让死人知道真相。")
}, new DialogueAsset.Choice[0]);

var first = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(Dir + "/chapter_01.asset");
var second = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(Dir + "/chapter_02.asset");
if (second == null) { second = ScriptableObject.CreateInstance<ChapterDefinition>(); AssetDatabase.CreateAsset(second, Dir + "/chapter_02.asset"); }
second.chapterId = "chapter_02";
second.chapterTitle = "第二章 完";
second.targetDisplayName = "书记官沈砚";
second.requiredClueIds = new[] { "elder_second_testimony", "ledger_page", "archive_eavesdrop" };
second.confrontDialogue = confront2;
second.finalChoiceDialogue = final2;
second.openingSubtitles = new[] { "第二天，影村开始搜查。", "封门令上还有第二个签名。" };
second.openingVariants = new[] {
    new ChapterDefinition.OpeningVariant { previousEndingId = "kill", subtitles = new[] { "队长死了。没人再告诉你该找谁。", "但你记得：封门令还有第二个签名。" } },
    new ChapterDefinition.OpeningVariant { previousEndingId = "kill_after_truth", subtitles = new[] { "血浸透名册，只剩一个姓氏。", "第二天，你沿着残名走向村公所。" } },
    new ChapterDefinition.OpeningVariant { previousEndingId = "spare", subtitles = new[] { "队长留下的名册指向书记官沈砚。", "第二天，他愿意再为你做一次证。" } }
};
second.endings = new[] {
    new ChapterDefinition.EndingDefinition { id = "expose", subtitles = new[] { "原始封门令被贴上告示墙。全村终于看见了签名。", "当夜，村外出现陌生骑手。幕后主使知道真相已经见光。" }, killTarget = false },
    new ChapterDefinition.EndingDefinition { id = "execute", subtitles = new[] { "沈砚倒下后，原始命令再也没能贴上告示墙。", "灰烬里还剩第三个签名的一角。你的复仇仍未结束。" }, killTarget = true }
};
first.nextChapter = second;
EditorUtility.SetDirty(first); EditorUtility.SetDirty(second);
AssetDatabase.SaveAssets();

GameObject Find(string name)
{
    foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        if (go.name == name && go.scene == SceneManager.GetActiveScene()) return go;
    return null;
}
var story = Find("Story");
var director = story.GetComponent<StoryDirector>();
var firstCaptain = director.target;
var captainController = firstCaptain.GetComponent<EnemyController>();

var root = Find("ChapterTwoContent");
if (root == null) root = new GameObject("ChapterTwoContent");
root.SetActive(true);   // 接线完成后统一关闭；编辑态可见
var clerk = Find("NpcClerk");
if (clerk == null)
{
    clerk = Object.Instantiate(captainController.gameObject, root.transform);
    clerk.name = "NpcClerk";
}
Vector3 clerkPos = new Vector3(-3f, 2f, -20f);
UnityEngine.AI.NavMeshHit navHit;
if (UnityEngine.AI.NavMesh.SamplePosition(clerkPos, out navHit, 8f, UnityEngine.AI.NavMesh.AllAreas)) clerkPos = navHit.position;
clerk.transform.position = clerkPos;
var clerkController = clerk.GetComponent<EnemyController>();
clerkController.enabled = false;
var clerkSenses = clerk.GetComponent<GuardSenses>();
clerkSenses.enabled = false;
var clerkTarget = clerk.GetComponent<StoryTarget>();
clerkTarget.allowKill = false;
clerkTarget.confrontRange = 2f;

var ledger = Find("ClueLedgerPage");
if (ledger == null) { ledger = new GameObject("ClueLedgerPage"); ledger.transform.SetParent(root.transform); }
ledger.transform.position = Find("IntelCrate").transform.position;
var ledgerClue = ledger.GetComponent<CluePickup>();
if (ledgerClue == null) ledgerClue = ledger.AddComponent<CluePickup>();
ledgerClue.clueId = "ledger_page";
ledgerClue.content = letter2;

var archive = Find("EavesdropArchive");
if (archive == null) { archive = new GameObject("EavesdropArchive"); archive.transform.SetParent(root.transform); }
archive.transform.position = Find("Archer").transform.position;
var archiveZone = archive.GetComponent<EavesdropZone>();
if (archiveZone == null) archiveZone = archive.AddComponent<EavesdropZone>();
archiveZone.clueId = "archive_eavesdrop";
archiveZone.radius = 4f;
archiveZone.requiredSeconds = 3f;
archiveZone.content = whisper2;

var captainWitness = firstCaptain.GetComponent<NpcInteractor>();
if (captainWitness == null) captainWitness = firstCaptain.gameObject.AddComponent<NpcInteractor>();
captainWitness.dialogue = witness;
captainWitness.range = 2f;
captainWitness.enabled = false;

var router = story.GetComponent<ChapterTwoContinuity>();
if (router == null) router = story.AddComponent<ChapterTwoContinuity>();
router.chapterTwoRoot = root;
router.chapterOneClues = new[] { Find("ClueFamilyLetter"), Find("EavesdropBarracks") };
router.firstCaptain = firstCaptain;
router.clerk = clerkTarget;
router.captainWitness = captainWitness;
router.elder = Find("NpcElder").GetComponent<NpcInteractor>();
router.chapterOneElderDialogue = AssetDatabase.LoadAssetAtPath<DialogueAsset>(Dir + "/elder_intro.asset");
router.chapterTwoElderDialogue = elder2;
director.chapterTwoContinuity = router;
director.chapter = first;
director.target = firstCaptain;

root.SetActive(false);
AssetDatabase.SaveAssets();
EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
Debug.Log("SETUP_CHAPTER_TWO PASS chapter=chapter_02 clerk=" + clerk.transform.position);
```

- [ ] **Step 5: 编译、Setup、运行内容测试**

Run: `request_compile` → 编辑态 `SetupChapterTwo.cs` → `Chapter2ContentTests.cs`。
Expected: `SETUP_CHAPTER_TWO PASS`、`CHAPTER2_CONTENT_TESTS PASS`；编译 `errors:0`。

- [ ] **Step 6: 保存进度**

`docs/progress.md` 勾选 Task 3，记录场景备份 MD5 与新增资产。

---

### Task 4: 三种承接、第二章完整流程与死亡/新游戏恢复

**Files:**
- Test: `C:\Users\22492\.claude\tools\story\Chapter2FlowTests.cs`
- Test: `C:\Users\22492\.claude\tools\story\Chapter2ContinuityTests.cs`

**Interfaces:**
- Consumes: 全部生产接口；本任务只修测试发现的真实缺陷，不新增玩法。

- [ ] **Step 1: 写承接测试**

```csharp
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

const string PREVIOUS = "spare";
int stage = -1;
double start = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    try
    {
        if (stage == -1)
        {
            StorySession.PendingChapterId = "chapter_02";
            StorySession.PreviousEndingId = PREVIOUS;
            StoryCheckpoint.Current = null;
            SceneManager.LoadScene("RPG游戏");
            stage = 0; start = EditorApplication.timeSinceStartup;
        }
        else if (stage == 0 && EditorApplication.timeSinceStartup - start > 1.0)
        {
            var director = StoryDirector.Instance;
            if (director == null || director.GetComponentInChildren<SubtitleOverlay>(true) == null) return;
            if (director.chapter == null || director.chapter.chapterId != "chapter_02") throw new Exception("FAIL: 未进入第二章");
            var captain = director.chapterTwoContinuity.firstCaptain;
            bool expectedAlive = PREVIOUS == "spare";
            if (captain.gameObject.activeSelf != expectedAlive) throw new Exception("FAIL: 队长连续性错误");
            if (director.chapterTwoContinuity.captainWitness.enabled != expectedAlive) throw new Exception("FAIL: 证人可用性错误");
            if (director.target != director.chapterTwoContinuity.clerk) throw new Exception("FAIL: 本章目标不是沈砚");
            File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2ContinuityTests-" + PREVIOUS + ".result.txt", "PASS " + PREVIOUS);
            Debug.Log("CHAPTER2_CONTINUITY_TESTS PASS " + PREVIOUS);
            EditorApplication.update -= tick;
            Time.timeScale = 1f;
            EditorApplication.ExitPlaymode();
        }
    }
    catch (Exception ex)
    {
        EditorApplication.update -= tick;
        File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2ContinuityTests-" + PREVIOUS + ".result.txt", "FAIL " + ex.Message);
        Debug.LogException(ex);
        Time.timeScale = 1f;
        EditorApplication.ExitPlaymode();
    }
};
EditorApplication.update += tick;
```

分别把 `PREVIOUS` 改为 `kill`、`kill_after_truth`、`spare` 跑三次。三次都必须 PASS。

- [ ] **Step 2: 写第二章流程测试**

```csharp
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

const string PATH = "expose";
StoryDirector director = null;
HeroController hero = null;
EnemyController clerk = null;
int stage = -2;
double start = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction tick = null;
Action<int> Next = n => { stage = n; start = EditorApplication.timeSinceStartup; };
SubtitleOverlay Subtitles() => director.GetComponentInChildren<SubtitleOverlay>(true);
DialoguePanel Panel() => director.GetComponentInChildren<DialoguePanel>(true);
Action<Vector3> Teleport = pos => { var cc = hero.GetComponent<CharacterController>(); cc.enabled = false; hero.transform.position = pos; cc.enabled = true; };
Action SkipSubtitles = () => { int guard = 0; while (Subtitles().IsShowing && guard++ < 12) Subtitles().Advance(); };
Action CloseContent = () => { for (int i = 0; i < 8 && Panel().IsShowing; i++) Panel().Advance(); };

tick = () =>
{
    try
    {
        double elapsed = EditorApplication.timeSinceStartup - start;
        if (stage == -2)
        {
            StorySession.PendingChapterId = "chapter_02";
            StorySession.PreviousEndingId = "spare";
            StoryCheckpoint.Current = null;
            SceneManager.LoadScene("RPG游戏");
            Next(-1);
        }
        else if (stage == -1)
        {
            director = StoryDirector.Instance;
            hero = UnityEngine.Object.FindObjectOfType<HeroController>();
            if (director == null || hero == null || hero.CurrentHP <= 0f || Subtitles() == null || !director.IsUiOpen) return;
            if (director.chapter.chapterId != "chapter_02") throw new Exception("FAIL: 章节错误");
            clerk = director.target.GetComponent<EnemyController>();
            SkipSubtitles();
            var archer = GameObject.Find("Archer").GetComponent<EnemyController>();
            archer.enabled = false; archer.agent.enabled = false; archer.GetComponent<GuardSenses>().enabled = false;
            Next(0);
        }
        else if (stage == 0 && elapsed > 0.3)
        {
            var elder = GameObject.Find("NpcElder").GetComponent<NpcInteractor>();
            Teleport(elder.transform.position + Vector3.forward);
            Next(1);
        }
        else if (stage == 1 && elapsed > 0.3)
        {
            var elder = GameObject.Find("NpcElder").GetComponent<NpcInteractor>();
            elder.Interact();
            for (int i = 0; i < elder.dialogue.lines.Count; i++) Panel().Advance();
            Panel().Choose(0);
            if (director.ClueCount != 1) throw new Exception("FAIL: 老药师线索");
            Teleport(GameObject.Find("ClueLedgerPage").transform.position);
            Next(2);
        }
        else if (stage == 2 && elapsed > 0.3)
        {
            GameObject.Find("ClueLedgerPage").GetComponent<CluePickup>().Interact();
            CloseContent();
            if (director.ClueCount != 2) throw new Exception("FAIL: 旧账页线索");
            Teleport(GameObject.Find("EavesdropArchive").transform.position);
            Next(3);
        }
        else if (stage == 3 && elapsed > 4.0)
        {
            CloseContent();
            if (director.ClueCount != 3 || director.Stage != StoryStage.ConfrontReady) throw new Exception("FAIL: 偷听线索或对峙就绪");
            Next(4);
        }
        else if (stage == 4)
        {
            if (Vector3.Distance(clerk.transform.position, director.storyPoint.position) > 1.5f) return;
            Teleport(clerk.transform.position - clerk.transform.forward * 1.2f);
            Next(5);
        }
        else if (stage == 5 && elapsed > 0.5)
        {
            if (!Panel().IsShowing) throw new Exception("FAIL: 沈砚近距对峙未触发");
            for (int i = 0; i < director.chapter.confrontDialogue.lines.Count; i++) Panel().Advance();
            Panel().Choose(1);
            for (int i = 0; i < director.chapter.finalChoiceDialogue.lines.Count; i++) Panel().Advance();
            Panel().Choose(PATH == "expose" ? 0 : 1);
            if (director.EndingId != PATH) throw new Exception("FAIL: 结局 " + director.EndingId);
            if (clerk.isDeath != (PATH == "execute")) throw new Exception("FAIL: 沈砚结局世界状态");
            SkipSubtitles();
            if (Time.timeScale != 1f || !hero.enabled) throw new Exception("FAIL: 结局后控制未恢复");
            File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2FlowTests-" + PATH + ".result.txt", "PASS " + PATH);
            Debug.Log("CHAPTER2_FLOW_TESTS PASS " + PATH);
            EditorApplication.update -= tick;
            Time.timeScale = 1f;
            EditorApplication.ExitPlaymode();
        }
        else if (elapsed > 90) throw new Exception("FAIL: 超时 stage=" + stage);
    }
    catch (Exception ex)
    {
        EditorApplication.update -= tick;
        File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2FlowTests-" + PATH + ".result.txt", "FAIL " + ex.Message);
        Debug.LogException(ex);
        Time.timeScale = 1f;
        EditorApplication.ExitPlaymode();
    }
};
EditorApplication.update += tick;
```

把 `PATH` 分别设为 `expose`、`execute` 运行，均须 PASS。另增加一次直接私刑：在对峙选项直接 `Choose(0)`，必须得到 `execute` 且沈砚死亡。

- [ ] **Step 3: 写完整死亡恢复与新游戏复位测试**

文件 `C:\Users\22492\.claude\tools\story\Chapter2RecoveryTests.cs`：

```csharp
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

int stage = -3;
double start = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction tick = null;
Action<int> Next = n => { stage = n; start = EditorApplication.timeSinceStartup; };

tick = () =>
{
    try
    {
        double elapsed = EditorApplication.timeSinceStartup - start;
        var director = StoryDirector.Instance;
        var hero = UnityEngine.Object.FindObjectOfType<HeroController>();
        if (stage == -3)
        {
            StorySession.PendingChapterId = "chapter_02";
            StorySession.PreviousEndingId = "spare";
            StoryCheckpoint.Current = null;
            SceneManager.LoadScene("RPG游戏");
            Next(-2);
        }
        else if (stage == -2)
        {
            if (director == null || hero == null || hero.CurrentHP <= 0f
                || director.GetComponentInChildren<SubtitleOverlay>(true) == null || !director.IsUiOpen) return;
            if (director.chapter.chapterId != "chapter_02") throw new Exception("FAIL: 未进入第二章");
            var subtitles = director.GetComponentInChildren<SubtitleOverlay>(true);
            int guard = 0;
            while (subtitles.IsShowing && guard++ < 12) subtitles.Advance();
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyController>())
            { enemy.enabled = false; if (enemy.agent != null) enemy.agent.enabled = false; }
            foreach (var senses in UnityEngine.Object.FindObjectsOfType<GuardSenses>()) senses.enabled = false;
            director.RegisterClue("ledger_page");
            hero.Hurt(hero.MaxHP, null);
            if (!hero.IsDead || !GameFlow.Instance.IsDeathPanelShown) throw new Exception("FAIL: 死亡流程未触发");
            if (StoryCheckpoint.Current == null || StoryCheckpoint.Current.chapterId != "chapter_02")
                throw new Exception("FAIL: 快照章节错误");
            GameFlow.Instance.RestartGame();
            Next(-1);
        }
        else if (stage == -1)
        {
            if (director == null || hero == null || hero.CurrentHP <= 0f
                || director.GetComponentInChildren<SubtitleOverlay>(true) == null) return;
            if (director.chapter.chapterId != "chapter_02") throw new Exception("FAIL: 死亡后回退章节");
            if (director.ClueCount != 1 || director.Stage != StoryStage.Investigate) throw new Exception("FAIL: 第二章线索未恢复");
            if (hero.CurrentHP != 100f || hero.IsDead || !hero.enabled || Time.timeScale != 1f)
                throw new Exception("FAIL: 第二章死亡重开角色状态");
            if (director.GetComponentInChildren<SubtitleOverlay>(true).IsShowing) throw new Exception("FAIL: 重开重复开场");
            StorySession.Reset();   // 与 CLick.ButtonClick 新游戏入口使用同一调用
            SceneManager.LoadScene("RPG游戏");
            Next(0);
        }
        else if (stage == 0)
        {
            if (director == null || director.GetComponentInChildren<SubtitleOverlay>(true) == null) return;
            if (director.chapter.chapterId != "chapter_01") throw new Exception("FAIL: 新游戏未回第一章");
            if (StorySession.PreviousEndingId != null || StoryCheckpoint.Current != null) throw new Exception("FAIL: 会话残留");
            if (!director.chapterTwoContinuity.firstCaptain.gameObject.activeSelf) throw new Exception("FAIL: 新游戏队长未恢复");
            if (director.target != director.chapterTwoContinuity.firstCaptain) throw new Exception("FAIL: 新游戏目标未恢复");
            File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2RecoveryTests.result.txt", "PASS chapter2 death + new game reset");
            Debug.Log("CHAPTER2_RECOVERY_TESTS PASS");
            EditorApplication.update -= tick;
            Time.timeScale = 1f;
            EditorApplication.ExitPlaymode();
        }
        if (elapsed > 90) throw new Exception("FAIL: 超时 stage=" + stage);
    }
    catch (Exception ex)
    {
        EditorApplication.update -= tick;
        File.WriteAllText("C:/Users/22492/.claude/tools/story/Chapter2RecoveryTests.result.txt", "FAIL " + ex.Message);
        Debug.LogException(ex);
        Time.timeScale = 1f;
        EditorApplication.ExitPlaymode();
    }
};
EditorApplication.update += tick;
```

Expected: `CHAPTER2_RECOVERY_TESTS PASS`，覆盖第二章实际死亡恢复和新游戏复位。

- [ ] **Step 4: 更新进度**

记录三种承接、两种结局、直接私刑、死亡恢复、新游戏复位的实际结果，勾选 Task 4。

---

### Task 5: 全量回归、视觉、持久化与交付

**Files:**
- Test: `C:\Users\22492\.claude\tools\story\Chapter2VisualCapture.cs`
- Create: `D:\Unity\projects\RPG_Project\docs\superpowers\reports\2026-11-12-chapter-two-verification.md`
- Modify: `D:\Unity\projects\RPG_Project\docs\progress.md`

- [ ] **Step 1: 第一章兼容回归**

重跑 `StoryAssetTests`、`StoryUiTests`、`StoryDirectorTests`、`StoryInteractionTests`、`StoryTargetTests`、第一章三路径、真实死亡恢复。全部必须 PASS；单元探针若遇场景正式导演，先停用正式 `Story` 物体再创建测试导演，禁止双导演。

- [ ] **Step 2: 旧玩法回归**

运行：

```bash
python -I "C:/Users/22492/.claude/tools/story/run_story_regression.py"
```

Expected: 12 项均 PASS。`SceneSwitchProbe` 以日志中的 `SCENE SWITCH RESULT` 且 `hero=True enemy=True` 为准。旧 `MissionTests` 不恢复。

- [ ] **Step 3: 写并运行完整视觉探针**

文件 `C:\Users\22492\.claude\tools\story\Chapter2VisualCapture.cs`，`PATH` 分别运行 `expose` 和 `execute`：

```csharp
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

const string PATH = "expose";
StoryDirector director = null;
HeroController hero = null;
EnemyController clerk = null;
int stage = -2;
double start = EditorApplication.timeSinceStartup;
string outDir = "D:/Unity/projects/RPG_Project/docs/verification/2026-11-12-chapter2";
EditorApplication.CallbackFunction tick = null;
Action<int> Next = n => { stage = n; start = EditorApplication.timeSinceStartup; };
SubtitleOverlay Subtitles() => director.GetComponentInChildren<SubtitleOverlay>(true);
DialoguePanel Panel() => director.GetComponentInChildren<DialoguePanel>(true);
Action Skip = () => { int guard = 0; while (Subtitles().IsShowing && guard++ < 12) Subtitles().Advance(); };
Action<Vector3> Teleport = pos => { var cc = hero.GetComponent<CharacterController>(); cc.enabled = false; hero.transform.position = pos; cc.enabled = true; };
Action<string> Capture = name => ScreenCapture.CaptureScreenshot(outDir + "/" + name + "-" + PATH + ".png");

tick = () =>
{
    try
    {
        double elapsed = EditorApplication.timeSinceStartup - start;
        if (stage == -2)
        {
            StorySession.PendingChapterId = "chapter_02";
            StorySession.PreviousEndingId = "spare";
            StoryCheckpoint.Current = null;
            Directory.CreateDirectory(outDir);
            SceneManager.LoadScene("RPG游戏");
            Next(-1);
        }
        else if (stage == -1)
        {
            director = StoryDirector.Instance;
            hero = UnityEngine.Object.FindObjectOfType<HeroController>();
            if (director == null || hero == null || hero.CurrentHP <= 0f || Subtitles() == null || !director.IsUiOpen) return;
            clerk = director.target.GetComponent<EnemyController>();
            Next(0);
        }
        else if (stage == 0 && elapsed > 0.5) { Capture("opening"); Next(1); }
        else if (stage == 1 && elapsed > 0.5)
        {
            Skip();
            var elder = GameObject.Find("NpcElder").GetComponent<NpcInteractor>();
            Teleport(elder.transform.position + Vector3.forward);
            Next(2);
        }
        else if (stage == 2 && elapsed > 0.4)
        {
            GameObject.Find("NpcElder").GetComponent<NpcInteractor>().Interact();
            Next(3);
        }
        else if (stage == 3 && elapsed > 0.5) { Capture("dialogue"); Next(4); }
        else if (stage == 4 && elapsed > 0.5)
        {
            for (int i = 0; i < director.chapter.openingVariants.Length + 2 && Panel().IsShowing; i++) Panel().Advance();
            if (Panel().IsShowing) Panel().Choose(0);
            director.RegisterClue("ledger_page");
            director.RegisterClue("archive_eavesdrop");
            Next(5);
        }
        else if (stage == 5)
        {
            if (Vector3.Distance(clerk.transform.position, director.storyPoint.position) > 1.5f) return;
            Teleport(clerk.transform.position - clerk.transform.forward * 1.5f);
            Next(6);
        }
        else if (stage == 6 && elapsed > 0.7)
        {
            if (!Panel().IsShowing) throw new Exception("FAIL: 对峙未显示");
            Capture("confront");
            Next(7);
        }
        else if (stage == 7 && elapsed > 0.5)
        {
            for (int i = 0; i < director.chapter.confrontDialogue.lines.Count; i++) Panel().Advance();
            Panel().Choose(1);
            for (int i = 0; i < director.chapter.finalChoiceDialogue.lines.Count; i++) Panel().Advance();
            Panel().Choose(PATH == "expose" ? 0 : 1);
            Next(8);
        }
        else if (stage == 8 && elapsed > 0.5)
        {
            if (!Subtitles().IsShowing) throw new Exception("FAIL: 结局字幕未显示");
            Capture("ending-subtitle");
            Next(9);
        }
        else if (stage == 9 && elapsed > 0.5) { Skip(); Next(10); }
        else if (stage == 10 && elapsed > 0.5) { Capture("ending-panel"); Next(11); }
        else if (stage == 11 && elapsed > 0.5)
        {
            File.WriteAllText(outDir + "/visual-" + PATH + ".txt", "PASS stable frames");
            EditorApplication.update -= tick;
            Time.timeScale = 1f;
            EditorApplication.ExitPlaymode();
        }
        if (elapsed > 90) throw new Exception("FAIL: 视觉超时 stage=" + stage);
    }
    catch (Exception ex)
    {
        EditorApplication.update -= tick;
        File.WriteAllText(outDir + "/visual-" + PATH + ".txt", "FAIL " + ex.Message);
        Debug.LogException(ex);
        Time.timeScale = 1f;
        EditorApplication.ExitPlaymode();
    }
};
EditorApplication.update += tick;
```

Expected: 两条路径均生成开场、对话、对峙、结局字幕、结局面板截图并写 `PASS stable frames`。逐张读图，确认无错位、按钮可读、中文完整。

- [ ] **Step 4: 持久化与控制台**

从磁盘重载 `RPG游戏`，检查 `chapter_01.nextChapter=chapter_02`、`ChapterTwoContinuity` 引用、第二章默认内容关闭、第一章默认对象完整、场景无未保存改动。清空负例测试日志后执行 `request_compile`，期望 0 错误/0 警告。既有烘焙/树 shader/Unknown Missing Script 提示不宣称清零。

- [ ] **Step 5: 最终恢复与文档**

恢复编辑态、`Time.timeScale=1`、`StorySession.Reset()`、`StoryCheckpoint.Current=null`，从磁盘重载场景。报告记录所有实测结果、日志路径、截图、备份 MD5、未覆盖的真人键鼠验收。`docs/progress.md` 勾选 Task 5，并更新下一章候选为待批准事项。

---

## 自审记录

- **Spec 覆盖**：会话与承接→Task 1；通用结局与继续按钮→Task 2；同场景对象、队长连续性、全部剧情文本→Task 3；三承接/双结局/死亡/新游戏→Task 4；回归/视觉/持久化→Task 5。
- **类型一致性**：`OpeningVariant`、`EndingDefinition`、`StorySession`、`ChapterTwoContinuity.Apply`、`ContinueToNextChapter`、`ApplyStoryEnding(string,bool)` 均先定义再使用；第一章旧方法保留兼容入口。
- **占位符**：无 TBD/TODO；代码步骤均给出实际代码、命令与预期输出。
- **范围**：没有新增地图、配音、运镜、磁盘存档、声望或通用节点编辑器。

