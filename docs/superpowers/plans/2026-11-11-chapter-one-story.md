# 《影村》第一章《回村的人》实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把「取卷轴→撤离」的搜打撤关卡替换为剧情驱动的复仇刺杀第一章：开场字幕、三条线索、队长对峙、杀/放双结局。

**Architecture:** 新增剧情数据资产（`ChapterDefinition`/`DialogueAsset`）+ 场景内唯一 `StoryDirector` 状态机 + 三个 UI（字幕/对话/结局）+ 四个交互组件（NPC 对话、线索拾取、偷听区、剧情目标）。现有潜行/刺杀/弓箭/药水系统保留，只通过小接口对接。

**Tech Stack:** Tuanjie 1.8.3（2022.3.62t5）、C#、uGUI（现有 `RpgUI` 辅助类）、无第三方叙事插件、无 Git。

## Global Constraints

- 设计依据：`docs/superpowers/specs/2026-11-11-chapter-one-story-design.md`（已获用户批准）。不要擅自增加配音、运镜、声望系统、多章节、磁盘存档。
- **工程无 Git**：所有"提交"步骤改为「备份脚本到 `D:\Unity\backups\RPG_Project_2026-11-11_story\` + 保存场景 + 更新 `docs/progress.md`」。不要执行 `git commit`。
- **测试工具链**（已验证可用，工程外的桥）：
  - 编译检查：`node C:/Users/22492/.claude/tools/rpg-bridge.mjs manage_editor '{"action":"get_compilation_summary"}'` → 期望 `"errors":0`
  - 编辑态探针：`node C:/Users/22492/.claude/tools/rpg-bridge.mjs execute_csharp_script '{"script_path":"<绝对路径>.cs","execution_mode":"editor","capture_logs":true}'`
  - Play 探针序列（**必须按此顺序，否则探针桥接编译期间主角会被巡逻卫兵打死**）：
    1. editor 执行：`EditorSceneManager.OpenScene("Assets/Scenes/RPG游戏.unity", OpenSceneMode.Single); Time.timeScale = 0f;`
    2. `node C:/Users/22492/.claude/tools/rpg-bridge.mjs manage_editor '{"action":"play"}'`
    3. play 探针：先等 `hero != null && hero.CurrentHP > 0`，完成初始化与敌人冻结后再 `Time.timeScale = 1f`
  - 若返回 `C# is performing domain reloading`，等待 3 秒后重发同一请求。
  - 探针一律用 `EditorApplication.update += tick` 分阶段执行，结果 `File.WriteAllText` 落盘并 `Debug.Log`，结束时 `EditorApplication.ExitPlaymode()` 且 `Time.timeScale = 1f`。
- **测试脚本放工程外** `C:\Users\22492\.claude\tools\story\`，不要覆盖 `C:\Users\22492\.claude\tools\` 下既有测试（此前覆盖 `ArcherEventOnlyTest.cs` 被权限拒绝，属用户保留事项）。新测试文件名统一 `Story*Tests.cs`。
- **中文文本**：所有剧情台词用中文；写文件用 Write 工具，不要用 bash heredoc（反斜杠/引号会被折叠）。
- **timeScale 红线**：任何打开字幕/对话/结局的路径都必须能恢复 `Time.timeScale = 1f`，包括结局打断对话、场景重载、返回主界面。
- **危险确认一律跳过**：删除资源、覆盖既有测试、批量改动、提交发布等需用户确认的操作，跳过并在 `docs/progress.md` 单列「待用户处理的确认项」。
- 旧 `MissionTests` 验证的是被替换的「取卷轴→撤离」玩法，本章场景接入后**不再要求通过**，在 `docs/progress.md` 标记为废弃，不要为让它通过而恢复旧任务。
- 每完成一个任务保存一次进度（用户硬性要求）。

---

### Task 1: 剧情数据资产类型与资产校验

**Files:**
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\DialogueAsset.cs`
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\ChapterDefinition.cs`
- Test: `C:\Users\22492\.claude\tools\story\StoryAssetTests.cs`

**Interfaces:**
- Produces: `DialogueAsset.id/lines/choices`，`DialogueAsset.Line(speaker,text)`，`DialogueAsset.Choice(choiceId,text,next,actionId)`；`ChapterDefinition.chapterId/openingSubtitles/requiredClueIds/confrontDialogue/finalChoiceDialogue/killEndingSubtitles/killAfterTruthEndingSubtitles/spareEndingSubtitles/targetDisplayName`。后续所有任务依赖这些字段名。

- [ ] **Step 1: 写失败测试** `StoryAssetTests.cs`（编辑态运行）

```csharp
using System;
using UnityEngine;

var dialogue = ScriptableObject.CreateInstance<DialogueAsset>();
dialogue.id = "test_dialogue";
dialogue.lines.Add(new DialogueAsset.Line { speaker = "老药师", text = "你回来了。" });
dialogue.choices.Add(new DialogueAsset.Choice { choiceId = "listen", text = "听他说完", actionId = "learn_truth" });
if (dialogue.lines.Count != 1 || dialogue.choices[0].actionId != "learn_truth")
    throw new Exception("FAIL: DialogueAsset 结构不可用");

var chapter = ScriptableObject.CreateInstance<ChapterDefinition>();
if (chapter.requiredClueIds == null || chapter.requiredClueIds.Length != 3)
    throw new Exception("FAIL: 章节必须默认三条线索");
if (chapter.chapterId != "chapter_01") throw new Exception("FAIL: chapterId 默认值");
Debug.Log("STORY_ASSET_TESTS PASS clues=" + chapter.requiredClueIds.Length);
```

- [ ] **Step 2: 运行确认失败**

Run: `node C:/Users/22492/.claude/tools/rpg-bridge.mjs execute_csharp_script '{"script_path":"C:/Users/22492/.claude/tools/story/StoryAssetTests.cs","execution_mode":"editor","capture_logs":true}'`
Expected: FAIL，`CS0246: The type or namespace name 'DialogueAsset' could not be found`

- [ ] **Step 3: 实现 `DialogueAsset.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Story/Dialogue", fileName = "Dialogue")]
public class DialogueAsset : ScriptableObject
{
    [Serializable]
    public class Line
    {
        public string speaker;
        [TextArea(2, 4)] public string text;
    }

    [Serializable]
    public class Choice
    {
        public string choiceId;
        public string text;
        public DialogueAsset next;   // 为空表示对话结束
        public string actionId;      // 由 StoryDirector 解释，见 Task 3
    }

    public string id;
    public List<Line> lines = new List<Line>();
    public List<Choice> choices = new List<Choice>();
}
```

- [ ] **Step 4: 实现 `ChapterDefinition.cs`**

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Story/Chapter", fileName = "Chapter")]
public class ChapterDefinition : ScriptableObject
{
    public string chapterId = "chapter_01";
    [TextArea(2, 4)] public string[] openingSubtitles = new string[0];
    public string[] requiredClueIds = { "elder_testimony", "family_letter", "barracks_eavesdrop" };
    public DialogueAsset confrontDialogue;
    public DialogueAsset finalChoiceDialogue;
    [TextArea(2, 4)] public string[] killEndingSubtitles = new string[0];
    [TextArea(2, 4)] public string[] killAfterTruthEndingSubtitles = new string[0];
    [TextArea(2, 4)] public string[] spareEndingSubtitles = new string[0];
    public string targetDisplayName = "卫兵队长";
}
```

- [ ] **Step 5: 刷新编译并跑测试**

Run: `node C:/Users/22492/.claude/tools/rpg-bridge.mjs manage_editor '{"action":"request_compile"}'`，再跑 Step 2 的命令
Expected: `STORY_ASSET_TESTS PASS clues=3`；编译摘要 `errors:0`

- [ ] **Step 6: 备份与进度**

把两个新脚本路径记入 `docs/progress.md`「剧情化改造」小节，勾选 Task 1。

---

### Task 2: 黑场字幕、对话框、结局面板

**Files:**
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\SubtitleOverlay.cs`
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\DialoguePanel.cs`
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\EndingPanel.cs`
- Test: `C:\Users\22492\.claude\tools\story\StoryUiTests.cs`

**Interfaces:**
- Consumes: `RpgUI.CreateCanvas(name, parent, interactive)`、`RpgUI.CreateImage(name, parent, sprite, color, anchor, pivot, position, size)`、`RpgUI.CreateText(name, parent, value, fontSize, color, alignment, anchor, pivot, position, size)`、`RpgUI.CreateButton(name, parent, sprite, label, position, size)`（签名见 `Assets/Script/RpgUI.cs`）
- Produces:
  - `SubtitleOverlay.Create(Transform parent)`、`Show(string[] lines, Action onComplete)`、`Advance()`、`IsShowing`
  - `DialoguePanel.Create(Transform parent)`、`Show(DialogueAsset asset, Action<DialogueAsset.Choice> onChoice)`、`Advance()`、`Choose(int index)`、`IsShowing`
  - `EndingPanel.Create(Transform parent)`、`Show(string title)`、`IsShowing`

- [ ] **Step 1: 写失败测试** `StoryUiTests.cs`（Play 模式，timeScale=0 也要能推进，验证 UI 不依赖时间缩放）

```csharp
using System;
using UnityEngine;

var subtitles = SubtitleOverlay.Create(null);
bool subtitleDone = false;
subtitles.Show(new[] { "第一行", "第二行" }, () => subtitleDone = true);
if (!subtitles.IsShowing) throw new Exception("FAIL: 字幕未显示");
subtitles.Advance();
if (subtitleDone) throw new Exception("FAIL: 还有一行就提前结束");
subtitles.Advance();
if (!subtitleDone || subtitles.IsShowing) throw new Exception("FAIL: 字幕未按行结束");

var asset = ScriptableObject.CreateInstance<DialogueAsset>();
asset.id = "ui_test";
asset.lines.Add(new DialogueAsset.Line { speaker = "队长", text = "是你。" });
asset.choices.Add(new DialogueAsset.Choice { choiceId = "kill", text = "动手", actionId = "ending_kill" });
asset.choices.Add(new DialogueAsset.Choice { choiceId = "listen", text = "听他说完", actionId = "learn_truth" });
var panel = DialoguePanel.Create(null);
DialogueAsset.Choice picked = null;
panel.Show(asset, c => picked = c);
panel.Advance();   // 行已读完，进入选项
panel.Choose(1);
if (picked == null || picked.actionId != "learn_truth") throw new Exception("FAIL: 对话选项未回调");
if (panel.IsShowing) throw new Exception("FAIL: 选择后面板未关闭");

var ending = EndingPanel.Create(null);
ending.Show("任务完成");
if (!ending.IsShowing) throw new Exception("FAIL: 结局面板未显示");
Debug.Log("STORY_UI_TESTS PASS");
EditorApplication.ExitPlaymode();
```

文件顶部需 `using UnityEditor;`。**注意**：探针在 timeScale=0 下运行即可，正好证明 UI 不受时间缩放影响。

- [ ] **Step 2: 运行确认失败**

按 Global Constraints 的 Play 序列执行探针。
Expected: FAIL，`CS0246: The type or namespace name 'SubtitleOverlay' could not be found`

- [ ] **Step 3: 实现 `SubtitleOverlay.cs`**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

public class SubtitleOverlay : MonoBehaviour
{
    public bool IsShowing { get; private set; }
    private Text label;
    private string[] lines;
    private int index;
    private Action onComplete;

    public static SubtitleOverlay Create(Transform parent)
    {
        var canvas = RpgUI.CreateCanvas("SubtitleCanvas", parent, false);
        var overlay = canvas.gameObject.AddComponent<SubtitleOverlay>();
        var dim = RpgUI.CreateImage("Dim", canvas.transform, null, new Color(0f, 0f, 0f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.sizeDelta = Vector2.zero;
        overlay.label = RpgUI.CreateText("Line", canvas.transform, "", 34, new Color(0.94f, 0.9f, 0.82f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 400f));
        RpgUI.CreateText("Hint", canvas.transform, "按任意键继续", 20, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(600f, 40f));
        canvas.gameObject.SetActive(false);
        return overlay;
    }

    public void Show(string[] lines, Action onComplete)
    {
        this.lines = lines ?? new string[0];
        this.onComplete = onComplete;
        index = 0;
        if (this.lines.Length == 0) { onComplete?.Invoke(); return; }
        gameObject.SetActive(true);
        IsShowing = true;
        label.text = this.lines[0];
    }

    public void Advance()
    {
        if (!IsShowing) return;
        index++;
        if (index >= lines.Length)
        {
            IsShowing = false;
            gameObject.SetActive(false);
            var done = onComplete;
            onComplete = null;
            done?.Invoke();
            return;
        }
        label.text = lines[index];
    }

    void Update() { if (IsShowing && Input.anyKeyDown) Advance(); }
}
```

- [ ] **Step 4: 实现 `DialoguePanel.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePanel : MonoBehaviour
{
    public bool IsShowing { get; private set; }
    private Text speakerText;
    private Text bodyText;
    private readonly List<Button> choiceButtons = new List<Button>();
    private DialogueAsset asset;
    private Action<DialogueAsset.Choice> onChoice;
    private int lineIndex;
    private bool choicesShown;

    public static DialoguePanel Create(Transform parent)
    {
        var canvas = RpgUI.CreateCanvas("DialogueCanvas", parent, true);
        var panel = canvas.gameObject.AddComponent<DialoguePanel>();
        var bg = RpgUI.CreateImage("Background", canvas.transform, null, new Color(0.05f, 0.05f, 0.08f, 0.92f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1400f, 260f));
        panel.speakerText = RpgUI.CreateText("Speaker", bg.transform, "", 26, new Color(1f, 0.9f, 0.72f), TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(1200f, 40f));
        panel.bodyText = RpgUI.CreateText("Body", bg.transform, "", 26, Color.white, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -70f), new Vector2(1340f, 120f));
        for (int i = 0; i < 2; i++)
        {
            int captured = i;
            var button = RpgUI.CreateButton("Choice" + i, bg.transform, null, "", new Vector2(-350f + i * 350f, -190f), new Vector2(320f, 56f));
            button.onClick.AddListener(() => panel.Choose(captured));
            panel.choiceButtons.Add(button);
        }
        canvas.gameObject.SetActive(false);
        return panel;
    }

    public void Show(DialogueAsset asset, Action<DialogueAsset.Choice> onChoice)
    {
        if (asset == null) { Debug.LogError("DialoguePanel.Show: 空对话资产"); return; }
        this.asset = asset;
        this.onChoice = onChoice;
        lineIndex = 0;
        choicesShown = false;
        gameObject.SetActive(true);
        IsShowing = true;
        foreach (var b in choiceButtons) b.gameObject.SetActive(false);
        RenderLine();
    }

    private void RenderLine()
    {
        if (lineIndex < asset.lines.Count)
        {
            speakerText.text = asset.lines[lineIndex].speaker;
            bodyText.text = asset.lines[lineIndex].text;
            return;
        }
        ShowChoices();
    }

    private void ShowChoices()
    {
        choicesShown = true;
        if (asset.choices.Count == 0) { Close(null); return; }
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            bool used = i < asset.choices.Count;
            choiceButtons[i].gameObject.SetActive(used);
            if (used) choiceButtons[i].GetComponentInChildren<Text>().text = asset.choices[i].text;
        }
    }

    public void Advance()
    {
        if (!IsShowing || choicesShown) return;
        lineIndex++;
        RenderLine();
    }

    public void Choose(int index)
    {
        if (!IsShowing || !choicesShown || index >= asset.choices.Count) return;
        Close(asset.choices[index]);
    }

    private void Close(DialogueAsset.Choice choice)
    {
        IsShowing = false;
        gameObject.SetActive(false);
        var callback = onChoice;
        onChoice = null;
        callback?.Invoke(choice);
    }

    void Update()
    {
        if (!IsShowing) return;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) Advance();
    }
}
```

- [ ] **Step 5: 实现 `EndingPanel.cs`**

```csharp
using UnityEngine;

public class EndingPanel : MonoBehaviour
{
    public bool IsShowing { get; private set; }

    public static EndingPanel Create(Transform parent)
    {
        var canvas = RpgUI.CreateCanvas("StoryEndingCanvas", parent, true);
        var panel = canvas.gameObject.AddComponent<EndingPanel>();
        var bg = RpgUI.CreateImage("Panel", canvas.transform, null, new Color(0.06f, 0.05f, 0.05f, 0.95f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 300f));
        RpgUI.CreateText("Title", bg.transform, "第一章 完", 42, new Color(1f, 0.9f, 0.72f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(500f, 70f));
        RpgUI.CreateButton("RestartButton", bg.transform, null, "重新挑战", new Vector2(-130f, -55f), new Vector2(220f, 64f))
            .onClick.AddListener(() => { if (GameFlow.Instance != null) GameFlow.Instance.RestartGame(); });
        RpgUI.CreateButton("MainMenuButton", bg.transform, null, "返回主界面", new Vector2(130f, -55f), new Vector2(220f, 64f))
            .onClick.AddListener(() => { if (GameFlow.Instance != null) GameFlow.Instance.ReturnToMain(); });
        canvas.gameObject.SetActive(false);
        return panel;
    }

    public void Show(string title)
    {
        gameObject.SetActive(true);
        IsShowing = true;
        Time.timeScale = 1f;   // 结局面板可点击，不被暂停卡住
    }
}
```

- [ ] **Step 6: 编译并跑测试**

Run: `manage_editor request_compile` → 按 Play 序列跑 `StoryUiTests.cs`
Expected: `STORY_UI_TESTS PASS`；编译 `errors:0`

- [ ] **Step 7: 备份与进度**

`docs/progress.md` 勾选 Task 2，记录三个 UI 脚本与"字幕/对话在 timeScale=0 下仍可推进"的实测结论。

---

### Task 3: StoryDirector 剧情状态机

**Files:**
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\StoryDirector.cs`
- Test: `C:\Users\22492\.claude\tools\story\StoryDirectorTests.cs`

**Interfaces:**
- Consumes: `ChapterDefinition`、`DialogueAsset`、`SubtitleOverlay`、`DialoguePanel`、`EndingPanel`（Task 1/2）；`StoryTarget.MoveToStoryPoint(Transform)`、`StoryTarget.BeginConfront()`（Task 5 才实现，本任务测试传 `null`）
- Produces（后续任务依赖）：
  - `StoryDirector.Instance`、`Stage`（`StoryStage.Opening/Investigate/ConfrontReady/Confront/Ending`）
  - `RegisterClue(string clueId)`、`ClueCount`、`BeginConfront()`、`OpenDialogue(DialogueAsset)`、`ApplyAction(string actionId)`
  - `GetObjectiveText()`、`IsUiOpen`、`TruthLearned`、`EndingId`（`kill`/`kill_after_truth`/`spare`）
  - `SetGameplayPaused(bool)`、`PlayOpening()`

- [ ] **Step 1: 写失败测试** `StoryDirectorTests.cs`

```csharp
using System;
using UnityEngine;
using UnityEditor;

StoryDirector director = null;
SubtitleOverlay subtitles = null;
int stage = -1;
double stageStart = EditorApplication.timeSinceStartup;
ChapterDefinition chapter = null;
DialogueAsset confront = null;
DialogueAsset finalChoice = null;
EditorApplication.CallbackFunction tick = null;

tick = () =>
{
    try
    {
        double elapsed = EditorApplication.timeSinceStartup - stageStart;
        if (stage == -1)
        {
            chapter = ScriptableObject.CreateInstance<ChapterDefinition>();
            chapter.openingSubtitles = new[] { "三年前，影村大火。" };
            confront = ScriptableObject.CreateInstance<DialogueAsset>();
            confront.id = "captain_confront";
            confront.lines.Add(new DialogueAsset.Line { speaker = "卫兵队长", text = "是你。你长得像你父亲。" });
            confront.choices.Add(new DialogueAsset.Choice { choiceId = "kill", text = "动手", actionId = "ending_kill" });
            confront.choices.Add(new DialogueAsset.Choice { choiceId = "listen", text = "听他说完", actionId = "learn_truth" });
            finalChoice = ScriptableObject.CreateInstance<DialogueAsset>();
            finalChoice.id = "final_choice";
            finalChoice.lines.Add(new DialogueAsset.Line { speaker = "卫兵队长", text = "名册给你。封门令来自上面。" });
            finalChoice.choices.Add(new DialogueAsset.Choice { choiceId = "kill", text = "杀", actionId = "ending_kill" });
            finalChoice.choices.Add(new DialogueAsset.Choice { choiceId = "spare", text = "放", actionId = "ending_spare" });
            chapter.confrontDialogue = confront;
            chapter.finalChoiceDialogue = finalChoice;
            chapter.spareEndingSubtitles = new[] { "他留下名册离去。" };

            var go = new GameObject("StoryTestDirector");
            go.SetActive(false);                       // AddComponent 会立刻跑 Awake，先禁用再赋值
            director = go.AddComponent<StoryDirector>();
            director.chapter = chapter;
            director.target = null;
            director.storyPoint = null;
            go.SetActive(true);
            stage = 0; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 0 && elapsed > 0.3)
        {
            if (director.Stage != StoryStage.Opening) throw new Exception("FAIL: 应为 Opening，实际 " + director.Stage);
            if (Time.timeScale != 0f) throw new Exception("FAIL: 开场字幕应暂停游戏");
            director.BeginConfront();                  // 负例：Opening 阶段不允许对峙
            if (director.Stage != StoryStage.Opening) throw new Exception("FAIL: Opening 阶段被错误推进");
            var overlay = director.GetComponentInChildren<SubtitleOverlay>(true);
            overlay.Advance();                         // 只有一行，直接结束
            if (director.Stage != StoryStage.Investigate) throw new Exception("FAIL: 字幕后应进入 Investigate");
            if (Time.timeScale != 1f) throw new Exception("FAIL: 字幕结束未恢复 timeScale");
            director.BeginConfront();                  // 负例：线索未齐不能对峙
            if (director.Stage == StoryStage.Confront) throw new Exception("FAIL: 线索未齐却能对峙");

            director.RegisterClue("family_letter");
            director.RegisterClue("family_letter");    // 幂等
            if (director.ClueCount != 1) throw new Exception("FAIL: 重复登记线索计数增加");
            director.RegisterClue("not_a_clue");       // 负例：未声明线索应被拒绝
            if (director.ClueCount != 1) throw new Exception("FAIL: 未声明线索被登记");
            director.RegisterClue("elder_testimony");
            director.RegisterClue("barracks_eavesdrop");
            if (director.Stage != StoryStage.ConfrontReady) throw new Exception("FAIL: 线索齐后应为 ConfrontReady");

            director.BeginConfront();
            var panel = director.GetComponentInChildren<DialoguePanel>(true);
            panel.Advance();
            panel.Choose(1);                           // 听他说完
            if (!director.TruthLearned) throw new Exception("FAIL: 听完后 TruthLearned 未置位");
            panel.Advance();
            panel.Choose(1);                           // 放
            if (director.EndingId != "spare") throw new Exception("FAIL: 结局应为 spare，实际 " + director.EndingId);
            overlay.Advance();
            if (Time.timeScale != 1f) throw new Exception("FAIL: 结局后 timeScale 未恢复");
            Debug.Log("STORY_DIRECTOR_TESTS PASS ending=" + director.EndingId);
            EditorApplication.update -= tick;
            UnityEngine.Object.Destroy(director.gameObject);
            EditorApplication.ExitPlaymode();
        }
        else if (elapsed > 30)
        {
            EditorApplication.update -= tick;
            throw new Exception("FAIL: 测试超时 stage=" + stage);
        }
    }
    catch (Exception ex)
    {
        EditorApplication.update -= tick;
        Debug.LogException(ex);
        Time.timeScale = 1f;
        EditorApplication.ExitPlaymode();
    }
};
EditorApplication.update += tick;
```

- [ ] **Step 2: 运行确认失败**

按 Global Constraints 的 Play 序列执行。
Expected: FAIL，`CS0246: The type or namespace name 'StoryDirector' could not be found`

- [ ] **Step 3: 实现 `StoryDirector.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public enum StoryStage { Opening, Investigate, ConfrontReady, Confront, Ending }

public class StoryDirector : MonoBehaviour
{
    public static StoryDirector Instance { get; private set; }

    public ChapterDefinition chapter;
    public StoryTarget target;        // 本章剧情目标（队长），Task 5
    public Transform storyPoint;      // 井边忏悔点

    public StoryStage Stage { get; private set; }
    public bool TruthLearned { get; private set; }
    public string EndingId { get; private set; }
    public int ClueCount { get { return clues.Count; } }
    public bool IsUiOpen
    {
        get
        {
            return (subtitles != null && subtitles.IsShowing)
                || (dialogue != null && dialogue.IsShowing)
                || (ending != null && ending.IsShowing);
        }
    }

    private readonly HashSet<string> clues = new HashSet<string>();
    private readonly Dictionary<MonoBehaviour, bool> pausedStates = new Dictionary<MonoBehaviour, bool>();
    private SubtitleOverlay subtitles;
    private DialoguePanel dialogue;
    private EndingPanel ending;
    private bool openingPlayed;

    void Awake() { Instance = this; Stage = StoryStage.Opening; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        subtitles = SubtitleOverlay.Create(transform);
        dialogue = DialoguePanel.Create(transform);
        ending = EndingPanel.Create(transform);
        PlayOpening();
    }

    public void PlayOpening()
    {
        if (openingPlayed) return;
        openingPlayed = true;
        SetGameplayPaused(true);
        subtitles.Show(chapter != null ? chapter.openingSubtitles : new string[0], () =>
        {
            Stage = StoryStage.Investigate;
            SetGameplayPaused(false);
        });
    }

    public void RegisterClue(string clueId)
    {
        if (chapter == null || string.IsNullOrEmpty(clueId)) return;
        if (Array.IndexOf(chapter.requiredClueIds, clueId) < 0)
        {
            Debug.LogError("StoryDirector: 未声明的线索 " + clueId);
            return;
        }
        if (!clues.Add(clueId)) return;   // 重复登记幂等
        if (clues.Count >= chapter.requiredClueIds.Length && Stage == StoryStage.Investigate)
        {
            Stage = StoryStage.ConfrontReady;
            if (target != null && storyPoint != null) target.MoveToStoryPoint(storyPoint);
        }
    }

    public void BeginConfront()
    {
        if (Stage != StoryStage.ConfrontReady || chapter == null) return;
        Stage = StoryStage.Confront;
        if (target != null) target.BeginConfront();
        OpenDialogue(chapter.confrontDialogue);
    }

    public void OpenDialogue(DialogueAsset asset)
    {
        if (asset == null) { Debug.LogError("StoryDirector: 空对话资产，已跳过"); return; }
        SetGameplayPaused(true);
        dialogue.Show(asset, choice =>
        {
            if (choice == null) { SetGameplayPaused(false); return; }
            HandleChoice(choice);
        });
    }

    private void HandleChoice(DialogueAsset.Choice choice)
    {
        if (!string.IsNullOrEmpty(choice.actionId)) ApplyAction(choice.actionId);
        if (IsUiOpen) return;                       // 动作已打开新对话或结局
        if (choice.next != null) OpenDialogue(choice.next);
        else SetGameplayPaused(false);
    }

    public void ApplyAction(string actionId)
    {
        switch (actionId)
        {
            case "give_clue_elder": RegisterClue("elder_testimony"); break;
            case "learn_truth":
                TruthLearned = true;
                OpenDialogue(chapter != null ? chapter.finalChoiceDialogue : null);
                break;
            case "ending_kill": ShowEnding(TruthLearned ? "kill_after_truth" : "kill"); break;
            case "ending_spare": ShowEnding("spare"); break;
            default: Debug.LogError("StoryDirector: 未知 actionId " + actionId); break;
        }
    }

    public void ShowEnding(string endingId)
    {
        EndingId = endingId;
        Stage = StoryStage.Ending;
        string[] lines = new string[0];
        if (chapter != null)
        {
            if (endingId == "kill") lines = chapter.killEndingSubtitles;
            else if (endingId == "kill_after_truth") lines = chapter.killAfterTruthEndingSubtitles;
            else lines = chapter.spareEndingSubtitles;
        }
        SetGameplayPaused(true);
        subtitles.Show(lines, () =>
        {
            SetGameplayPaused(false);
            ending.Show("第一章 完");
        });
    }

    public string GetObjectiveText()
    {
        int required = chapter != null && chapter.requiredClueIds != null ? chapter.requiredClueIds.Length : 3;
        switch (Stage)
        {
            case StoryStage.Opening: return "影村：回村的人";
            case StoryStage.Investigate: return "目标：调查队长罪行 " + clues.Count + " / " + required;
            case StoryStage.ConfrontReady: return "目标：去井边找" + (chapter != null ? chapter.targetDisplayName : "卫兵队长");
            case StoryStage.Confront: return "抉择：复仇，还是真相";
            default: return "第一章 完";
        }
    }

    public void SetGameplayPaused(bool paused)
    {
        if (paused)
        {
            pausedStates.Clear();
            foreach (var h in FindObjectsOfType<HeroController>()) pausedStates[h] = h.enabled;
            foreach (var e in FindObjectsOfType<EnemyController>()) pausedStates[e] = e.enabled;
            foreach (var g in FindObjectsOfType<GuardSenses>()) pausedStates[g] = g.enabled;
            foreach (var kv in pausedStates) kv.Key.enabled = false;
            Time.timeScale = 0f;
        }
        else
        {
            foreach (var kv in pausedStates) if (kv.Key != null) kv.Key.enabled = kv.Value;
            pausedStates.Clear();
            Time.timeScale = 1f;
        }
    }
}
```

- [ ] **Step 4: 编译并跑测试**

Run: `manage_editor request_compile` → 按 Play 序列跑 `StoryDirectorTests.cs`
Expected: `STORY_DIRECTOR_TESTS PASS ending=spare`；编译 `errors:0`。
若断言失败，检查是否漏了「Opening 阶段不能对峙」「未声明线索被拒绝」两条负例，不要为通过而放宽断言。

- [ ] **Step 5: 备份与进度**

`docs/progress.md` 勾选 Task 3，记录 `StoryDirector` 的状态机与 actionId 列表。

---

### Task 4: NPC 对话、线索拾取、偷听区

**Files:**
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\NpcInteractor.cs`
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\CluePickup.cs`
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\EavesdropZone.cs`
- Test: `C:\Users\22492\.claude\tools\story\StoryInteractionTests.cs`

**Interfaces:**
- Consumes: `StoryDirector.RegisterClue/OpenDialogue/IsUiOpen`（Task 3）
- Produces:
  - `NpcInteractor.dialogue`、`range=2f`、`CanInteract()`、`Interact()`（Task 6 挂老药师）
  - `CluePickup.clueId`、`range=1.6f`、`Interact()`（Task 6 挂家书）
  - `EavesdropZone.clueId`、`radius=4f`、`requiredSeconds=3f`（Task 6 挂兵营外）

- [ ] **Step 1: 写失败测试** `StoryInteractionTests.cs`

```csharp
using System;
using UnityEngine;
using UnityEditor;

StoryDirector director = null;
NpcInteractor npc = null;
CluePickup pickup = null;
EavesdropZone zone = null;
HeroController hero = null;
int stage = -1;
double stageStart = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction tick = null;

Action<Vector3> Teleport = pos =>
{
    var cc = hero.GetComponent<CharacterController>();
    cc.enabled = false; hero.transform.position = pos; cc.enabled = true;
};

tick = () =>
{
    try
    {
        double elapsed = EditorApplication.timeSinceStartup - stageStart;
        if (stage == -1)
        {
            hero = UnityEngine.Object.FindObjectOfType<HeroController>();
            if (hero == null || hero.CurrentHP <= 0f) { if (elapsed > 10) throw new Exception("FAIL: 主角未初始化"); return; }
            foreach (var e in UnityEngine.Object.FindObjectsOfType<EnemyController>()) { e.enabled = false; if (e.agent != null) e.agent.enabled = false; }
            foreach (var g in UnityEngine.Object.FindObjectsOfType<GuardSenses>()) g.enabled = false;
            hero.enabled = false;

            var go = new GameObject("StoryTestDirector");
            go.SetActive(false);
            director = go.AddComponent<StoryDirector>();
            director.chapter = ScriptableObject.CreateInstance<ChapterDefinition>();
            director.chapter.openingSubtitles = new string[0];   // 跳过开场
            go.SetActive(true);

            npc = new GameObject("TestNpc").AddComponent<NpcInteractor>();
            npc.dialogue = ScriptableObject.CreateInstance<DialogueAsset>();
            npc.dialogue.lines.Add(new DialogueAsset.Line { speaker = "老药师", text = "你回来了。" });
            npc.transform.position = hero.transform.position + Vector3.forward * 3f;

            pickup = new GameObject("TestLetter").AddComponent<CluePickup>();
            pickup.clueId = "family_letter";
            pickup.transform.position = hero.transform.position;

            zone = new GameObject("TestZone").AddComponent<EavesdropZone>();
            zone.clueId = "barracks_eavesdrop";
            zone.radius = 4f;
            zone.transform.position = hero.transform.position;
            Time.timeScale = 1f;
            stage = 0; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 0 && elapsed > 0.3)
        {
            if (npc.CanInteract()) throw new Exception("FAIL: 3m 外不应可对话");
            Teleport(npc.transform.position + Vector3.forward * 1f);
            stage = 1; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 1 && elapsed > 0.3)
        {
            if (!npc.CanInteract()) throw new Exception("FAIL: 1m 内应可对话");
            npc.Interact();
            if (!director.IsUiOpen) throw new Exception("FAIL: 对话未打开");
            var panel = director.GetComponentInChildren<DialoguePanel>(true);
            panel.Advance();      // 无选项对话直接结束
            if (director.IsUiOpen) throw new Exception("FAIL: 无选项对话未自动关闭");
            if (Time.timeScale != 1f) throw new Exception("FAIL: 对话结束未恢复时间");

            pickup.Interact();
            if (director.ClueCount != 1) throw new Exception("FAIL: 家书未登记");
            if (pickup.gameObject.activeSelf) throw new Exception("FAIL: 拾取后家书未隐藏");
            pickup.Interact();
            if (director.ClueCount != 1) throw new Exception("FAIL: 重复拾取计数增加");
            stage = 2; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 2 && elapsed > 1.0)
        {
            if (director.ClueCount != 1) throw new Exception("FAIL: 偷听未满 3 秒就登记");
            stage = 3; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 3 && elapsed > 2.5)
        {
            if (director.ClueCount != 2) throw new Exception("FAIL: 偷听满 3 秒未登记线索");
            Debug.Log("STORY_INTERACTION_TESTS PASS clues=" + director.ClueCount);
            EditorApplication.update -= tick;
            UnityEngine.Object.Destroy(director.gameObject);
            EditorApplication.ExitPlaymode();
        }
        else if (elapsed > 40)
        {
            EditorApplication.update -= tick;
            throw new Exception("FAIL: 超时 stage=" + stage);
        }
    }
    catch (Exception ex)
    {
        EditorApplication.update -= tick;
        Debug.LogException(ex);
        Time.timeScale = 1f;
        EditorApplication.ExitPlaymode();
    }
};
EditorApplication.update += tick;
```

- [ ] **Step 2: 运行确认失败**

Expected: FAIL，`CS0246: The type or namespace name 'NpcInteractor' could not be found`

- [ ] **Step 3: 实现 `NpcInteractor.cs`**

```csharp
using UnityEngine;

public class NpcInteractor : MonoBehaviour
{
    public DialogueAsset dialogue;
    public float range = 2f;
    private HeroController player;

    public bool CanInteract()
    {
        if (player == null) player = FindObjectOfType<HeroController>();
        if (player == null || player.IsDead || dialogue == null) return false;
        if (StoryDirector.Instance != null && StoryDirector.Instance.IsUiOpen) return false;
        return Vector3.Distance(player.transform.position, transform.position) <= range;
    }

    public void Interact()
    {
        if (!CanInteract() || StoryDirector.Instance == null) return;
        StoryDirector.Instance.OpenDialogue(dialogue);
    }

    void Update() { if (Input.GetKeyDown(KeyCode.E)) Interact(); }
}
```

- [ ] **Step 4: 实现 `CluePickup.cs`**

```csharp
using UnityEngine;

public class CluePickup : MonoBehaviour
{
    public string clueId = "family_letter";
    public float range = 1.6f;
    private HeroController player;

    public bool CanInteract()
    {
        if (player == null) player = FindObjectOfType<HeroController>();
        if (player == null || player.IsDead || StoryDirector.Instance == null) return false;
        if (StoryDirector.Instance.IsUiOpen) return false;
        return Vector3.Distance(player.transform.position, transform.position) <= range;
    }

    public void Interact()
    {
        if (!CanInteract()) return;
        StoryDirector.Instance.RegisterClue(clueId);
        gameObject.SetActive(false);
    }

    void Update() { if (Input.GetKeyDown(KeyCode.E)) Interact(); }
}
```

- [ ] **Step 5: 实现 `EavesdropZone.cs`**

```csharp
using UnityEngine;

public class EavesdropZone : MonoBehaviour
{
    public string clueId = "barracks_eavesdrop";
    public float radius = 4f;
    public float requiredSeconds = 3f;
    private HeroController player;
    private float timer;
    private bool registered;

    void Update()
    {
        if (registered || StoryDirector.Instance == null) return;
        if (StoryDirector.Instance.IsUiOpen) return;
        if (player == null) player = FindObjectOfType<HeroController>();
        if (player == null || player.IsDead) return;

        bool inside = Vector3.Distance(player.transform.position, transform.position) <= radius;
        bool chased = false;
        foreach (var guard in FindObjectsOfType<GuardSenses>())
            if (guard.State == GuardState.Chase) { chased = true; break; }

        if (inside && !chased)
        {
            timer += Time.deltaTime;
            if (timer >= requiredSeconds)
            {
                registered = true;
                StoryDirector.Instance.RegisterClue(clueId);
            }
        }
        else timer = 0f;   // 离开或被发现都要重新累计
    }
}
```

- [ ] **Step 6: 编译并跑测试**

Expected: `STORY_INTERACTION_TESTS PASS clues=2`；编译 `errors:0`

- [ ] **Step 7: 备份与进度**

`docs/progress.md` 勾选 Task 4，记录「E 交互在剧情 UI 打开时被屏蔽」「偷听需连续 3 秒未被发现」。

---

### Task 5: 剧情目标 StoryTarget、队长刺杀屏蔽、HUD 目标接入

**Files:**
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\StoryTarget.cs`
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\AssassinationController.cs`（`Update` 开头、`IsEligible` 末尾）
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\GameHUD.cs:175-176`（目标文本优先级）
- Test: `C:\Users\22492\.claude\tools\story\StoryTargetTests.cs`

**Interfaces:**
- Consumes: `StoryDirector.GetObjectiveText/IsUiOpen`（Task 3）
- Produces: `StoryTarget.allowKill`、`SetScriptedPassive(bool)`、`MoveToStoryPoint(Transform)`、`BeginConfront()`（Task 3 调用后两者）

- [ ] **Step 1: 写失败测试** `StoryTargetTests.cs`

```csharp
using System;
using UnityEngine;
using UnityEditor;

HeroController hero = null;
EnemyController captain = null;
StoryTarget target = null;
AssassinationController assassin = null;
int stage = -1;
double stageStart = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction tick = null;

Action<Vector3> Teleport = pos =>
{
    var cc = hero.GetComponent<CharacterController>();
    cc.enabled = false; hero.transform.position = pos; cc.enabled = true;
};
Action PlaceBehind = () =>
{
    Vector3 pos = captain.transform.position - captain.transform.forward * 1.2f;
    Teleport(pos);
    Vector3 face = captain.transform.position - pos; face.y = 0f;
    hero.transform.rotation = Quaternion.LookRotation(face.normalized);
};

tick = () =>
{
    try
    {
        double elapsed = EditorApplication.timeSinceStartup - stageStart;
        if (stage == -1)
        {
            hero = UnityEngine.Object.FindObjectOfType<HeroController>();
            if (hero == null || hero.CurrentHP <= 0f) { if (elapsed > 10) throw new Exception("FAIL: 主角未初始化"); return; }
            assassin = hero.GetComponent<AssassinationController>();
            foreach (var e in UnityEngine.Object.FindObjectsOfType<EnemyController>())
            {
                if (e.GetComponent<RangedAttack>() == null) captain = e;
                e.enabled = false; if (e.agent != null) e.agent.enabled = false;
            }
            foreach (var g in UnityEngine.Object.FindObjectsOfType<GuardSenses>()) g.enabled = false;  // 保持 Patrol
            hero.enabled = false;
            if (captain == null) throw new Exception("FAIL: 找不到近战卫兵");
            target = captain.gameObject.AddComponent<StoryTarget>();
            target.allowKill = false;
            PlaceBehind();
            stage = 0; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 0 && elapsed > 0.3)
        {
            EnemyController picked;
            if (assassin.CanAssassinate(out picked)) throw new Exception("FAIL: 剧情目标未解锁时仍可被刺杀");
            target.allowKill = true;
            if (!assassin.CanAssassinate(out picked)) throw new Exception("FAIL: allowKill=true 后仍不能刺杀");
            if (picked != captain) throw new Exception("FAIL: 刺杀目标不是队长");

            var point = new GameObject("TestStoryPoint").transform;
            point.position = captain.transform.position + Vector3.forward * 5f;
            captain.agent.enabled = true;
            target.MoveToStoryPoint(point);
            if (captain.enabled) throw new Exception("FAIL: 剧情走位时 EnemyController 应暂停");
            if (!captain.agent.isActiveAndEnabled || captain.agent.destination != point.position)
                throw new Exception("FAIL: 剧情走位目的地未设置");
            target.BeginConfront();
            if (captain.GetComponent<GuardSenses>().enabled) throw new Exception("FAIL: 对峙时感知应暂停");

            var hud = UnityEngine.Object.FindObjectOfType<GameHUD>();
            var go = new GameObject("StoryTestDirector");
            go.SetActive(false);
            var director = go.AddComponent<StoryDirector>();
            director.chapter = ScriptableObject.CreateInstance<ChapterDefinition>();
            director.chapter.openingSubtitles = new string[0];
            go.SetActive(true);
            stage = 1; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 1 && elapsed > 0.5)
        {
            var hud = UnityEngine.Object.FindObjectOfType<GameHUD>();
            string expected = StoryDirector.Instance.GetObjectiveText();
            if (hud.objectiveText.text != expected)
                throw new Exception("FAIL: HUD 目标文本未由 StoryDirector 驱动，实际 " + hud.objectiveText.text + " 期望 " + expected);
            Debug.Log("STORY_TARGET_TESTS PASS objective=" + expected);
            EditorApplication.update -= tick;
            UnityEngine.Object.Destroy(StoryDirector.Instance.gameObject);
            EditorApplication.ExitPlaymode();
        }
        else if (elapsed > 40)
        {
            EditorApplication.update -= tick;
            throw new Exception("FAIL: 超时 stage=" + stage);
        }
    }
    catch (Exception ex)
    {
        EditorApplication.update -= tick;
        Debug.LogException(ex);
        Time.timeScale = 1f;
        EditorApplication.ExitPlaymode();
    }
};
EditorApplication.update += tick;
```

- [ ] **Step 2: 运行确认失败**

Expected: FAIL，`CS0246: The type or namespace name 'StoryTarget' could not be found`

- [ ] **Step 3: 实现 `StoryTarget.cs`**

```csharp
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyController))]
public class StoryTarget : MonoBehaviour
{
    public bool allowKill;   // 最终抉择允许动手前，通用 E 刺杀对本目标无效
    private EnemyController controller;
    private GuardSenses senses;
    private NavMeshAgent agent;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        senses = GetComponent<GuardSenses>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void SetScriptedPassive(bool value)
    {
        if (senses != null) senses.enabled = !value;
        if (controller != null) controller.enabled = !value;
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = value;
    }

    public void MoveToStoryPoint(Transform point)
    {
        if (point == null) return;
        SetScriptedPassive(true);   // 剧情走位期间不被巡逻/警戒覆盖目的地
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(point.position);
        }
    }

    public void BeginConfront()
    {
        SetScriptedPassive(true);
        var hero = FindObjectOfType<HeroController>();
        if (hero == null) return;
        Vector3 dir = hero.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    void Update()
    {
        // EnemyController 被剧情暂停时，行走动画由本组件维护，避免滑行
        if (controller == null || controller.enabled || agent == null || !agent.enabled) return;
        if (controller.animator != null)
            controller.animator.SetBool("walk", agent.velocity.magnitude > 0.05f);
    }
}
```

- [ ] **Step 4: 修改 `AssassinationController.cs`**

`Update()` 开头（`if (hero == null) return;` 之后）插入：

```csharp
        if (StoryDirector.Instance != null && StoryDirector.Instance.IsUiOpen) return;
```

`IsEligible` 末尾两行（原 `var senses = ...; return senses != null && senses.State == GuardState.Patrol;`）替换为：

```csharp
        var senses = enemy.GetComponent<GuardSenses>();
        if (senses == null || senses.State != GuardState.Patrol) return false;
        var storyTarget = enemy.GetComponent<StoryTarget>();
        return storyTarget == null || storyTarget.allowKill;
```

- [ ] **Step 5: 修改 `GameHUD.cs` 目标文本**

把 `objectiveText.text` 的两行赋值（`MissionController.Instance` 优先的那段）改为：

```csharp
        if (StoryDirector.Instance != null) objectiveText.text = StoryDirector.Instance.GetObjectiveText();
        else if (MissionController.Instance != null) objectiveText.text = MissionController.Instance.GetObjectiveText();
        else objectiveText.text = kills >= target ? "目标完成：敌人已全部击败" : "目标：击败敌人 " + kills + " / " + target;
```

- [ ] **Step 6: 编译并跑测试**

Expected: `STORY_TARGET_TESTS PASS objective=目标：调查队长罪行 0 / 3`；编译 `errors:0`

- [ ] **Step 7: 备份与进度**

备份 `AssassinationController.cs`、`GameHUD.cs` 到 `D:\Unity\backups\RPG_Project_2026-11-11_story\`，`docs/progress.md` 勾选 Task 5。

---

### Task 6: 第一章场景与剧情内容接入

**Files:**
- Create: `D:\Unity\projects\RPG_Project\Assets\Story\chapter_01.asset`（由 Setup 脚本生成）
- Create: `D:\Unity\projects\RPG_Project\Assets\Story\elder_intro.asset`、`captain_confront.asset`、`final_choice.asset`
- Create: `C:\Users\22492\.claude\tools\story\SetupChapterOne.cs`（编辑态场景接入脚本，可重复执行，幂等）
- Modify: `D:\Unity\projects\RPG_Project\Assets\Scenes\RPG游戏.unity`（由 Setup 脚本修改并保存）
- Test: `C:\Users\22492\.claude\tools\story\StoryChapterOneTests.cs`

**Interfaces:**
- Consumes: 前面全部任务。场景内对象名约定：`Story`（含 `StoryDirector`）、`StoryPoint`（井边）、`NpcElder`、`ClueFamilyLetter`、`EavesdropBarracks`、队长=近战 `Enemy`（挂 `StoryTarget`）、年轻卫兵=`Archer`。

- [ ] **Step 1: 备份场景**

把 `RPG游戏.unity` 复制到 `D:\Unity\backups\RPG_Project_2026-11-11_story\RPG游戏.before-story.unity`（用文件复制工具，不用交互式 `cp`）。

- [ ] **Step 2: 写失败测试** `StoryChapterOneTests.cs`

探针分阶段跑真实章节内容，`PATH` 常量决定走哪条分支，需跑三次：`"kill"`（直接动手）、`"kill_after_truth"`（听完后杀）、`"spare"`（听完后放）。

```csharp
using System;
using UnityEngine;
using UnityEditor;

const string PATH = "spare";   // 三次运行分别改为 kill / kill_after_truth / spare

StoryDirector director = null;
HeroController hero = null;
EnemyController captain = null;
int stage = -1;
double stageStart = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction tick = null;

Action<Vector3> Teleport = pos =>
{
    var cc = hero.GetComponent<CharacterController>();
    cc.enabled = false; hero.transform.position = pos; cc.enabled = true;
};
SubtitleOverlay Subtitles => director.GetComponentInChildren<SubtitleOverlay>(true);
DialoguePanel Panel => director.GetComponentInChildren<DialoguePanel>(true);
Action SkipSubtitles = () => { int guard = 0; while (Subtitles.IsShowing && guard++ < 12) Subtitles.Advance(); };

tick = () =>
{
    try
    {
        double elapsed = EditorApplication.timeSinceStartup - stageStart;
        if (stage == -1)
        {
            hero = UnityEngine.Object.FindObjectOfType<HeroController>();
            director = StoryDirector.Instance;
            if (hero == null || director == null || hero.CurrentHP <= 0f) { if (elapsed > 10) throw new Exception("FAIL: 章节未初始化"); return; }
            captain = null;
            foreach (var e in UnityEngine.Object.FindObjectsOfType<EnemyController>())
                if (e.GetComponent<StoryTarget>() != null) captain = e;
            if (captain == null) throw new Exception("FAIL: 场景里没有剧情目标队长");
            SkipSubtitles();     // 跳过开场字幕
            stage = 0; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 0 && elapsed > 0.3)
        {
            if (director.Stage != StoryStage.Investigate) throw new Exception("FAIL: 开幕后应进入调查阶段");
            var elder = GameObject.Find("NpcElder").GetComponent<NpcInteractor>();
            Teleport(elder.transform.position + Vector3.forward);
            stage = 1; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 1 && elapsed > 0.3)
        {
            var elder = GameObject.Find("NpcElder").GetComponent<NpcInteractor>();
            if (!elder.CanInteract()) throw new Exception("FAIL: 老药师不可对话");
            elder.Interact();
            for (int i = 0; i < elder.dialogue.lines.Count; i++) Panel.Advance();   // 逐行读完才出现选项
            Panel.Choose(0);                        // 「我记得那晚。」→ 登记老药师证词
            if (director.ClueCount != 1) throw new Exception("FAIL: 老药师证词未登记");
            Teleport(GameObject.Find("ClueFamilyLetter").transform.position);
            stage = 2; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 2 && elapsed > 0.3)
        {
            var letter = GameObject.Find("ClueFamilyLetter").GetComponent<CluePickup>();
            letter.Interact();
            if (director.ClueCount != 2) throw new Exception("FAIL: 家书未登记");
            Teleport(GameObject.Find("EavesdropBarracks").transform.position);
            stage = 3; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 3 && elapsed > 4.0)
        {
            if (director.ClueCount != 3) throw new Exception("FAIL: 偷听未登记第三条线索，ClueCount=" + director.ClueCount);
            if (director.Stage != StoryStage.ConfrontReady) throw new Exception("FAIL: 未进入对峙就绪");
            Teleport(captain.transform.position - captain.transform.forward * 1.5f);
            stage = 4; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 4 && elapsed > 0.5)
        {
            director.BeginConfront();
            for (int i = 0; i < director.chapter.confrontDialogue.lines.Count; i++) Panel.Advance();
            if (PATH == "kill") Panel.Choose(0);
            else
            {
                Panel.Choose(1);            // 听他说完
                for (int i = 0; i < director.chapter.finalChoiceDialogue.lines.Count; i++) Panel.Advance();
                Panel.Choose(PATH == "spare" ? 1 : 0);
            }
            string expected = PATH == "kill" ? "kill" : PATH == "spare" ? "spare" : "kill_after_truth";
            if (director.EndingId != expected)
                throw new Exception("FAIL: 结局应为 " + expected + "，实际 " + director.EndingId);
            if (PATH != "kill" && !director.TruthLearned) throw new Exception("FAIL: 听完未记录真相");
            SkipSubtitles();
            if (Time.timeScale != 1f) throw new Exception("FAIL: 结局后 timeScale 未恢复");
            Debug.Log("STORY_CHAPTER_ONE_TESTS PASS path=" + PATH + " ending=" + director.EndingId);
            EditorApplication.update -= tick;
            EditorApplication.ExitPlaymode();
        }
        else if (elapsed > 90)
        {
            EditorApplication.update -= tick;
            throw new Exception("FAIL: 超时 stage=" + stage + " clue=" + director.ClueCount);
        }
    }
    catch (Exception ex)
    {
        EditorApplication.update -= tick;
        Debug.LogException(ex);
        Time.timeScale = 1f;
        EditorApplication.ExitPlaymode();
    }
};
EditorApplication.update += tick;
```

- [ ] **Step 3: 运行确认失败**

Expected: FAIL，`场景里没有剧情目标队长` 或 `StoryDirector.Instance == null`

- [ ] **Step 4: 实现 `SetupChapterOne.cs`（编辑态执行，创建资产与场景对象，幂等：已存在则复用）**

```csharp
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

const string StoryDir = "Assets/Story";
if (!Directory.Exists(StoryDir)) Directory.CreateDirectory(StoryDir);

DialogueAsset Elder = null, Confront = null, Final = null;
DialogueAsset MakeDialogue(string name, string id, DialogueAsset.Line[] lines, DialogueAsset.Choice[] choices)
{
    string path = StoryDir + "/" + name + ".asset";
    var asset = AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);
    if (asset == null)
    {
        asset = ScriptableObject.CreateInstance<DialogueAsset>();
        AssetDatabase.CreateAsset(asset, path);
    }
    asset.id = id;
    asset.lines.Clear(); asset.lines.AddRange(lines);
    asset.choices.Clear(); asset.choices.AddRange(choices);
    EditorUtility.SetDirty(asset);
    return asset;
}
DialogueAsset.Line L(string speaker, string text) => new DialogueAsset.Line { speaker = speaker, text = text };
DialogueAsset.Choice C(string id, string text, string action) => new DialogueAsset.Choice { choiceId = id, text = text, actionId = action };

Elder = MakeDialogue("elder_intro", "elder_intro",
    new[] {
        L("老药师", "是你？三年前那场火，我把你从后门拖出来的。"),
        L("老药师", "带队封门的是卫兵队长。他每晚巡逻，怀里一直揣着一封不敢寄出的信。"),
        L("老药师", "兵营那边常有人议论那晚的事。别被发现，听清楚了再动手。")
    },
    new[] { C("accept", "我记得那晚。", "give_clue_elder") });

Confront = MakeDialogue("captain_confront", "captain_confront",
    new[] {
        L("卫兵队长", "站住。……是你。你长得像你父亲。"),
        L("卫兵队长", "我每晚都来井边。那晚之后，我没睡过一个整觉。")
    },
    new[] { C("kill", "动手", "ending_kill"), C("listen", "听他说完", "learn_truth") });

Final = MakeDialogue("final_choice", "final_choice",
    new[] {
        L("卫兵队长", "封门令是上面签的，我只是执行。名册在这里，签名都在。"),
        L("卫兵队长", "你可以现在杀了我，也可以拿着它去找真正下令的人。")
    },
    new[] { C("kill", "杀", "ending_kill"), C("spare", "放", "ending_spare") });

string chapterPath = StoryDir + "/chapter_01.asset";
var chapter = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(chapterPath);
if (chapter == null)
{
    chapter = ScriptableObject.CreateInstance<ChapterDefinition>();
    AssetDatabase.CreateAsset(chapter, chapterPath);
}
chapter.chapterId = "chapter_01";
chapter.openingSubtitles = new[] { "三年前，影村一场大火，烧掉了你的全部。", "今天，你回村了。" };
chapter.confrontDialogue = Confront;
chapter.finalChoiceDialogue = Final;
chapter.killEndingSubtitles = new[] { "刀落下去的时候，你没有问出那个名字。", "复仇完成了，真相却随他一起死去。" };
chapter.killAfterTruthEndingSubtitles = new[] { "名册被血浸透，只剩一个名字还能辨认。", "村里开始挨家搜捕。你带着那半个名字离开。" };
chapter.spareEndingSubtitles = new[] { "他留下名册，朝村口走去，没有回头。", "名册指向真正下令的人。你的仇，还没报完。" };
EditorUtility.SetDirty(chapter);
AssetDatabase.SaveAssets();

// ---- 场景对象 ----
GameObject Ensure(string name, Transform parent = null)
{
    var go = GameObject.Find(name);
    if (go == null) { go = new GameObject(name); if (parent != null) go.transform.SetParent(parent, false); }
    return go;
}

var story = Ensure("Story");
var director = story.GetComponent<StoryDirector>() ?? story.AddComponent<StoryDirector>();
director.chapter = chapter;

var hero = Object.FindObjectOfType<HeroController>();
EnemyController captain = null;
foreach (var e in Object.FindObjectsOfType<EnemyController>())
    if (e.GetComponent<RangedAttack>() == null) captain = e;
var storyTarget = captain.GetComponent<StoryTarget>() ?? captain.gameObject.AddComponent<StoryTarget>();
storyTarget.allowKill = false;
director.target = storyTarget;

// 井边忏悔点：沿用原撤离点位置（原撤离交互同时停用）
var portal = GameObject.Find("MissionEscapePortal");
var point = Ensure("StoryPoint", story.transform);
if (portal != null) point.transform.position = portal.transform.position;
else point.transform.position = captain.transform.position + Vector3.forward * 6f;
director.storyPoint = point.transform;

// 老药师：出生点旁 3m，复用队长模型，只有对话组件
var elder = Ensure("NpcElder");
elder.transform.position = hero.transform.position + hero.transform.forward * 3f;
if (elder.GetComponentInChildren<SkinnedMeshRenderer>() == null)
{
    var model = captain.GetComponentInChildren<SkinnedMeshRenderer>();
    if (model != null) Object.Instantiate(model.gameObject, elder.transform, false);
}
var npc = elder.GetComponent<NpcInteractor>() ?? elder.AddComponent<NpcInteractor>();
npc.dialogue = Elder;
npc.range = 2f;

// 家书：原卷轴木箱位置
var crate = GameObject.Find("IntelCrate");
var letter = Ensure("ClueFamilyLetter");
if (crate != null) letter.transform.position = crate.transform.position;
var clue = letter.GetComponent<CluePickup>() ?? letter.AddComponent<CluePickup>();
clue.clueId = "family_letter";
letter.SetActive(true);

// 偷听区：弓箭手（年轻卫兵）巡逻区旁
var archer = GameObject.Find("Archer");
var zone = Ensure("EavesdropBarracks");
if (archer != null) zone.transform.position = archer.transform.position;
var eavesdrop = zone.GetComponent<EavesdropZone>() ?? zone.AddComponent<EavesdropZone>();
eavesdrop.clueId = "barracks_eavesdrop";
eavesdrop.radius = 4f;
eavesdrop.requiredSeconds = 3f;

// 停用旧「取卷轴→撤离」玩法，对象保留以便回滚
foreach (var intel in Object.FindObjectsOfType<IntelPickup>(true)) intel.enabled = false;
foreach (var escape in Object.FindObjectsOfType<EscapePoint>(true)) escape.enabled = false;
foreach (var mission in Object.FindObjectsOfType<MissionController>(true)) mission.enabled = false;

// 开场安全区：把距出生点 12m 内的巡逻点沿远离方向推到 12m；若推移后破坏 NavMesh 通路则回退该点
if (captain.targetList != null)
{
    var agent = captain.agent;
    foreach (var patrol in captain.targetList)
    {
        if (patrol == null) continue;
        Vector3 flat = patrol.position - hero.transform.position; flat.y = 0f;
        if (flat.magnitude >= 12f || flat.magnitude <= 0.01f) continue;
        Vector3 original = patrol.position;
        patrol.position = hero.transform.position + flat.normalized * 12f + Vector3.up * (original.y - hero.transform.position.y);
        UnityEngine.AI.NavMeshHit navHit;
        if (!UnityEngine.AI.NavMesh.SamplePosition(patrol.position, out navHit, 3f, UnityEngine.AI.NavMesh.AllAreas))
        {
            patrol.position = original;   // 找不到可行走点，保持原样
            Debug.LogWarning("SETUP_CHAPTER_ONE: 巡逻点无法推移到可行走位置，已回退 " + patrol.name);
            continue;
        }
        patrol.position = navHit.position;
    }
    // 通路验证：任一巡逻点路径不完整就整体回退，不留下会卡死的巡逻路线
    if (agent != null && agent.isOnNavMesh)
    {
        foreach (var patrol in captain.targetList)
        {
            if (patrol == null) continue;
            var path = new UnityEngine.AI.NavMeshPath();
            UnityEngine.AI.NavMesh.CalculatePath(captain.transform.position, patrol.position, UnityEngine.AI.NavMesh.AllAreas, path);
            if (path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
                Debug.LogWarning("SETUP_CHAPTER_ONE: 巡逻点通路不完整，需人工复核 " + patrol.name + " status=" + path.status);
        }
    }
}

AssetDatabase.SaveAssets();
EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
Debug.Log("SETUP_CHAPTER_ONE PASS chapter=" + chapter.chapterId + " captain=" + captain.name + " elder=" + elder.transform.position);
```

- [ ] **Step 5: 执行 Setup 并保存场景**

Run: `node C:/Users/22492/.claude/tools/rpg-bridge.mjs execute_csharp_script '{"script_path":"C:/Users/22492/.claude/tools/story/SetupChapterOne.cs","execution_mode":"editor","capture_logs":true}'`
Expected: `SETUP_CHAPTER_ONE PASS chapter=chapter_01 ...`；编译 `errors:0`

- [ ] **Step 6: 三条路径各跑一遍**

把 `StoryChapterOneTests.cs` 的 `PATH` 依次改为 `kill`、`kill_after_truth`、`spare`，每次按 Play 序列运行。
Expected: 三次分别输出 `STORY_CHAPTER_ONE_TESTS PASS path=kill ending=kill`、`... path=kill_after_truth ending=kill_after_truth`、`... path=spare ending=spare`

- [ ] **Step 7: 截图与进度**

抓取开场字幕、老药师对话、队长对峙、两种结局画面，存 `docs/verification/2026-11-11-story/`；`docs/progress.md` 勾选 Task 6，并注明旧 `MissionTests` 随旧任务停用而废弃。

---

### Task 7: 死亡检查点与进度恢复

**Files:**
- Create: `D:\Unity\projects\RPG_Project\Assets\Script\StoryCheckpoint.cs`
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\StoryDirector.cs`（`Start` 分支 + 两个新方法）
- Modify: `D:\Unity\projects\RPG_Project\Assets\Script\GameFlow.cs:20`（`OnHeroDeath` 开头捕获快照）
- Test: `C:\Users\22492\.claude\tools\story\StoryCheckpointTests.cs`

**Interfaces:**
- Consumes: `StoryDirector` 的 `clues`、`Stage`、`TruthLearned`、`EndingId`、`target`、`storyPoint`
- Produces: `StoryCheckpoint.Current`（静态，跨场景重载存活）、`StoryDirector.CaptureCheckpoint()`

- [ ] **Step 1: 写失败测试** `StoryCheckpointTests.cs`

```csharp
using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

int stage = -1;
double stageStart = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction tick = null;

tick = () =>
{
    try
    {
        double elapsed = EditorApplication.timeSinceStartup - stageStart;
        if (stage == -1)
        {
            var director = StoryDirector.Instance;
            var hero = UnityEngine.Object.FindObjectOfType<HeroController>();
            if (director == null || hero == null || hero.CurrentHP <= 0f) { if (elapsed > 10) throw new Exception("FAIL: 未初始化"); return; }
            var subtitles = director.GetComponentInChildren<SubtitleOverlay>(true);
            int guard = 0;
            while (subtitles.IsShowing && guard++ < 12) subtitles.Advance();
            director.RegisterClue("family_letter");
            if (director.ClueCount != 1) throw new Exception("FAIL: 线索登记失败");
            director.CaptureCheckpoint();
            if (StoryCheckpoint.Current == null) throw new Exception("FAIL: 快照未生成");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);   // 等价于死亡重开本章
            stage = 0; stageStart = EditorApplication.timeSinceStartup;
        }
        else if (stage == 0 && elapsed > 1.0)
        {
            var director = StoryDirector.Instance;
            if (director == null) { if (elapsed > 10) throw new Exception("FAIL: 重载后无 StoryDirector"); return; }
            if (director.ClueCount != 1) throw new Exception("FAIL: 重载后线索丢失，ClueCount=" + director.ClueCount);
            if (director.Stage != StoryStage.Investigate) throw new Exception("FAIL: 重载后阶段错误 " + director.Stage);
            var letter = GameObject.Find("ClueFamilyLetter");
            if (letter != null && letter.activeSelf) throw new Exception("FAIL: 已拾取家书在重载后复活");
            var subtitles = director.GetComponentInChildren<SubtitleOverlay>(true);
            if (subtitles.IsShowing) throw new Exception("FAIL: 重开后重复播放开场字幕");
            if (Time.timeScale != 1f) throw new Exception("FAIL: 重开后 timeScale 未恢复");
            Debug.Log("STORY_CHECKPOINT_TESTS PASS clues=" + director.ClueCount + " stage=" + director.Stage);
            EditorApplication.update -= tick;
            EditorApplication.ExitPlaymode();
        }
        else if (elapsed > 40)
        {
            EditorApplication.update -= tick;
            throw new Exception("FAIL: 超时 stage=" + stage);
        }
    }
    catch (Exception ex)
    {
        EditorApplication.update -= tick;
        Debug.LogException(ex);
        Time.timeScale = 1f;
        EditorApplication.ExitPlaymode();
    }
};
EditorApplication.update += tick;
```

- [ ] **Step 2: 运行确认失败**

Expected: FAIL，`CS0246: The type or namespace name 'StoryCheckpoint' could not be found`（或 `CaptureCheckpoint` 不存在）

- [ ] **Step 3: 实现 `StoryCheckpoint.cs`**

```csharp
public class StoryCheckpoint
{
    public static StoryCheckpoint Current;   // 仅本次运行内存快照，不做磁盘存档
    public StoryStage stage;
    public string[] clues = new string[0];
    public bool truthLearned;
    public string endingId;
}
```

- [ ] **Step 4: 修改 `StoryDirector.cs`**

`Start()` 末尾的 `PlayOpening();` 改为：

```csharp
        if (StoryCheckpoint.Current != null) ApplyCheckpoint();
        else PlayOpening();
```

在 `SetGameplayPaused` 之后新增两个方法：

```csharp
    public void CaptureCheckpoint()
    {
        StoryCheckpoint.Current = new StoryCheckpoint
        {
            stage = Stage,
            clues = new List<string>(clues).ToArray(),
            truthLearned = TruthLearned,
            endingId = EndingId
        };
    }

    private void ApplyCheckpoint()
    {
        var cp = StoryCheckpoint.Current;
        openingPlayed = true;                       // 重开后不重播开场
        Stage = cp.stage == StoryStage.Ending ? StoryStage.Investigate : cp.stage;
        if (Stage == StoryStage.Confront) Stage = StoryStage.ConfrontReady;   // 对峙允许重新触发
        clues.Clear();
        foreach (var id in cp.clues) clues.Add(id);
        TruthLearned = cp.truthLearned;
        EndingId = null;

        foreach (var pickup in FindObjectsOfType<CluePickup>(true))
            if (clues.Contains(pickup.clueId)) pickup.gameObject.SetActive(false);

        if (Stage == StoryStage.ConfrontReady && target != null && storyPoint != null)
            target.MoveToStoryPoint(storyPoint);
        SetGameplayPaused(false);
    }
```

- [ ] **Step 5: 修改 `GameFlow.cs`**

`OnHeroDeath()` 开头（`if (deathPanel != null) return;` 之后）插入：

```csharp
        if (StoryDirector.Instance != null) StoryDirector.Instance.CaptureCheckpoint();
```

- [ ] **Step 6: 编译并跑测试**

Expected: `STORY_CHECKPOINT_TESTS PASS clues=1 stage=Investigate`；编译 `errors:0`

- [ ] **Step 7: 备份与进度**

`docs/progress.md` 勾选 Task 7，注明「快照只在内存，不做磁盘存档」。

---

### Task 8: 全量回归、独立验收与文档

**Files:**
- Test: 既有探针 `C:\Users\22492\.claude\tools\P1StealthTests.cs`、`P1GuardSensesTests.cs`、`P1ArcherEventOnlyTests.cs`、`DirectMovementTests.cs`、`DirectAttackTests.cs`、`AssassinationTests.cs`、`PotionRuntimeTests.cs`、`SmokeTests.cs`、`FullHudTests.cs`、`PauseMenuTests.cs`、`DeathFlowTests.cs`、`SceneSwitchProbe.cs`
- Create: `C:\Users\22492\.claude\tools\story\StoryRegressionGuard.cs`
- Create: `D:\Unity\projects\RPG_Project\docs\superpowers\reports\2026-11-11-story-verification.md`

- [ ] **Step 1: 写回归前置脚本 `StoryRegressionGuard.cs`**

旧玩法探针不知道剧情字幕的存在，直接在剧情场景跑会被开场暂停卡住。**只在 Play 内禁用剧情物体，不保存场景**：

```csharp
using UnityEngine;
using UnityEditor;

var story = GameObject.Find("Story");
if (story != null) story.SetActive(false);
Time.timeScale = 1f;
Debug.Log("STORY_REGRESSION_GUARD storyDisabled=" + (story != null));
```

用法：按 Play 序列进入 Play 后，先以 play 模式执行本脚本，再执行对应的旧探针。

- [ ] **Step 2: 逐个重跑旧探针**

每个文件重复「Play → `StoryRegressionGuard.cs` → 旧探针」，记录结果。期望全部通过，**除了**：
- `MissionTests.cs`：验证已被替换的旧任务，标记废弃，不重跑。
- `DeathFlowTests.cs`：死亡时现在会额外捕获剧情快照，属预期行为；若它断言面板数量，仍应通过。

任何失败先判断是「剧情接入引入的回归」还是「旧探针依赖旧任务对象」，前者必须修，后者在报告中说明，**不得为通过而恢复旧任务或放宽断言**。

- [ ] **Step 3: 磁盘重载持久化检查**

编辑态执行 `EditorSceneManager.OpenScene("Assets/Scenes/RPG游戏.unity", OpenSceneMode.Single)` 后确认：`Story` 物体与 `StoryDirector.chapter` 引用在、`NpcElder.dialogue` 引用在、队长 `StoryTarget` 在且 `allowKill=false`、`StoryPoint` 位置正确、旧 `IntelPickup/EscapePoint/MissionController` 均为禁用、被移动的巡逻点位置已保存。

- [ ] **Step 4: 控制台与截图**

最终停止 Play 并重载场景后，控制台不得有新增编译错误或运行时异常；既有 Progressive GPU 烘焙、地形树 Soft Occlusion、场景切换 Unknown Missing Script 提示按既有事实记录，不宣称清零。截图存 `docs/verification/2026-11-11-story/`。

- [ ] **Step 5: 写验收报告并更新进度**

`docs/superpowers/reports/2026-11-11-story-verification.md` 内容：三条剧情路径实测结果、负例、检查点恢复、回归清单与结果、截图清单、未覆盖项（真实键鼠手感仍需用户试玩）。`docs/progress.md` 勾选 Task 8，并写明「《影村》已从搜打撤关卡转为剧情驱动第一章」。

---

## 自审记录

- **Spec 覆盖**：剧情资产→Task 1；字幕/对话/结局 UI 与暂停规则→Task 2/3；状态机与 actionId→Task 3；NPC/拾取/偷听→Task 4；`StoryTarget` 与刺杀屏蔽、HUD 目标→Task 5；旧任务停用、开场安全区、剧情文本→Task 6；检查点→Task 7；回归与验收→Task 8。Spec 中的「不做」项（配音、运镜、声望、多章节、磁盘存档）未出现在任何任务中。
- **类型一致性**：`StoryDirector` 调用的 `StoryTarget.MoveToStoryPoint/BeginConfront` 在 Task 5 定义；`DialogueAsset.Choice.next/actionId` 在 Task 1 定义并在 Task 3 消费；`SubtitleOverlay/DialoguePanel/EndingPanel` 的 `Create/Show/Advance/Choose/IsShowing` 在 Task 2 定义，与 Task 3/6/7 探针用法一致；`StoryCheckpoint.Current` 在 Task 7 定义并被 `GameFlow` 使用。
- **占位符**：无 TBD/TODO；所有代码步骤均含完整代码与期望输出。
- **已知取舍**：老药师复用队长模型、无 NPC 名牌与配音（素材简陋按用户要求不计缺陷）；队长走位用 NavMeshAgent 实际寻路，剧情期间由 `StoryTarget` 接管行走动画避免滑行。



