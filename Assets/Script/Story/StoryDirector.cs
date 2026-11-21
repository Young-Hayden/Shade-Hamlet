using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>第一章剧情阶段。</summary>
public enum StoryStage
{
    /// <summary>开场字幕。</summary>
    Opening,
    /// <summary>调查阶段：收集三条线索。</summary>
    Investigate,
    /// <summary>线索齐备，可前往井边对峙。</summary>
    ConfrontReady,
    /// <summary>与队长对峙/对话。</summary>
    Confront,
    /// <summary>结局演出。</summary>
    Ending
}

/// <summary>
/// 第一章剧情总导演（单例）。
/// 负责章节选择、开场字幕、线索收集、对峙、结局分支、检查点恢复，以及剧情期间的玩法暂停。
/// 剧情数据来自 <see cref="ChapterDefinition"/> 资产；UI 由 SubtitleOverlay / DialoguePanel / EndingPanel 运行时创建。
/// </summary>
public class StoryDirector : MonoBehaviour
{
    /// <summary>全局唯一实例。</summary>
    public static StoryDirector Instance { get; private set; }

    /// <summary>当前章节定义（可在 Inspector 指定；跨章时自动切换到 nextChapter）。</summary>
    public ChapterDefinition chapter;
    /// <summary>剧情目标（卫兵队长），负责走位、对峙与结局表现。</summary>
    public StoryTarget target;
    /// <summary>队长在对峙前的集合点（井边）。</summary>
    public Transform storyPoint;

    /// <summary>当前剧情阶段。</summary>
    public StoryStage Stage { get; private set; }
    /// <summary>是否已经得知真相（影响结局分支）。</summary>
    public bool TruthLearned { get; private set; }
    /// <summary>已结算的结局 ID（kill / kill_after_truth / spare）。</summary>
    public string EndingId { get; private set; }
    /// <summary>已收集线索数量。</summary>
    public int ClueCount { get { return clues.Count; } }

    /// <summary>是否任一剧情 UI（字幕/对话/结局）正在显示。</summary>
    public bool IsUiOpen
    {
        get
        {
            return (subtitles != null && subtitles.IsShowing)
                || (dialogue != null && dialogue.IsShowing)
                || (ending != null && ending.IsShowing);
        }
    }

    /// <summary>剧情是否正在暂停玩法；供暂停菜单等外部系统协调 timeScale。</summary>
    public bool IsGameplayPaused { get { return gameplayPaused; } }

    /// <summary>已收集的线索 ID 集合（自动去重）。</summary>
    private readonly HashSet<string> clues = new HashSet<string>();
    /// <summary>暂停前各玩法组件的启用状态，恢复时还原。</summary>
    private readonly Dictionary<MonoBehaviour, bool> pausedStates = new Dictionary<MonoBehaviour, bool>();
    /// <summary>字幕层。</summary>
    private SubtitleOverlay subtitles;
    /// <summary>对话框。</summary>
    private DialoguePanel dialogue;
    /// <summary>结局面板。</summary>
    private EndingPanel ending;
    /// <summary>开场是否已经播放，避免重载后重复开场。</summary>
    private bool openingPlayed;
    /// <summary>当前是否处于剧情暂停。</summary>
    private bool gameplayPaused;

    void Awake() { Instance = this; Stage = StoryStage.Opening; }
    void OnDisable()
    {
        if (Instance == this)
        {
            // 场景卸载/组件关闭时务必恢复时间缩放，避免编辑器停在 timeScale=0。
            SetGameplayPaused(false);
            Instance = null;
        }
    }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Start()
    {
        SelectPendingChapter();
        subtitles = SubtitleOverlay.Create(transform);
        dialogue = DialoguePanel.Create(transform);
        ending = EndingPanel.Create(transform);
        StartCoroutine(InitializeStory());
    }

    /// <summary>根据 StorySession 指定的待续章节，切换到当前场景可达的章节定义。</summary>
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

    /// <summary>选择开场字幕：若存在匹配上一结局的变体则优先使用，否则用默认开场。</summary>
    private string[] GetOpeningLines()
    {
        if (chapter == null) return new string[0];
        foreach (var variant in chapter.openingVariants)
            if (variant != null && variant.previousEndingId == StorySession.PreviousEndingId)
                return variant.subtitles;
        return chapter.openingSubtitles;
    }

    /// <summary>
    /// 剧情初始化：先等一帧让所有角色的 Start（HP/引用）执行完；
    /// 若从主菜单带过渡进入，等待黑场淡入结束；有检查点则恢复，否则播放开场。
    /// </summary>
    private IEnumerator InitializeStory()
    {
        // 等所有角色 Start 完成 HP/引用初始化；若从主菜单带过渡进入，等黑场淡入结束再开始剧情。
        yield return null;
        if (SceneTransition.BlocksGameplay) yield return SceneTransition.WaitForGameplayReady();
        if (StoryCheckpoint.Current != null) ApplyCheckpoint();
        else PlayOpening();
    }

    /// <summary>播放开场字幕；暂停玩法，字幕结束后进入调查阶段。</summary>
    public void PlayOpening()
    {
        if (openingPlayed) return;
        openingPlayed = true;
        SetGameplayPaused(true);
        subtitles.Show(GetOpeningLines(), () =>
        {
            Stage = StoryStage.Investigate;
            SetGameplayPaused(false);
        });
    }

    /// <summary>登记一条线索；未在章节中声明的线索视为配置错误并忽略。</summary>
    public void RegisterClue(string clueId)
    {
        if (chapter == null || string.IsNullOrEmpty(clueId)) return;
        if (Array.IndexOf(chapter.requiredClueIds, clueId) < 0)
        {
            Debug.LogError("StoryDirector: 未声明的线索 " + clueId);
            return;
        }
        if (!clues.Add(clueId)) return;
        // 线索集齐且仍处于调查阶段时，解锁对峙并让队长走向井边。
        if (clues.Count >= chapter.requiredClueIds.Length && Stage == StoryStage.Investigate)
        {
            Stage = StoryStage.ConfrontReady;
            if (target != null && storyPoint != null) target.MoveToStoryPoint(storyPoint);
        }
    }

    /// <summary>开始对峙：仅在 ConfrontReady 状态有效。</summary>
    public void BeginConfront()
    {
        if (Stage != StoryStage.ConfrontReady || chapter == null) return;
        Stage = StoryStage.Confront;
        if (target != null) target.BeginConfront();
        OpenDialogue(chapter.confrontDialogue);
    }

    /// <summary>打开一段对话；对话期间暂停玩法，选项回调交给 HandleChoice。</summary>
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

    /// <summary>处理对话选项：执行动作、进入下一段对话，或结束对话并恢复玩法。</summary>
    private void HandleChoice(DialogueAsset.Choice choice)
    {
        if (!string.IsNullOrEmpty(choice.actionId)) ApplyAction(choice.actionId);
        if (IsUiOpen) return;
        if (choice.next != null) OpenDialogue(choice.next);
        else SetGameplayPaused(false);
    }

    /// <summary>执行章节动作：给线索、得知真相、结算结局。</summary>
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

    /// <summary>结算结局：让目标执行对应表现，播放结局字幕，最后显示结局面板。</summary>
    public void ShowEnding(string endingId)
    {
        EndingId = endingId;
        Stage = StoryStage.Ending;
        if (target != null) target.ApplyStoryEnding(endingId);
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

    /// <summary>返回 HUD 当前应显示的目标文本。</summary>
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

    /// <summary>把当前剧情进度保存到内存检查点（主角死亡重开时使用，不写磁盘）。</summary>
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

    /// <summary>恢复检查点：重建线索集合、隐藏已拾取线索、必要时让队长回到井边。</summary>
    private void ApplyCheckpoint()
    {
        var cp = StoryCheckpoint.Current;
        // 有检查点说明开场已经看过，不再重复播放。
        openingPlayed = true;
        Stage = cp.stage == StoryStage.Ending ? StoryStage.Investigate : cp.stage;
        // 对峙中途死亡时退回到「可对峙」，避免直接卡在对话态。
        if (Stage == StoryStage.Confront) Stage = StoryStage.ConfrontReady;
        clues.Clear();
        foreach (var id in cp.clues) clues.Add(id);
        TruthLearned = cp.truthLearned;
        EndingId = null;

        // 已经拾取过的线索物件不再出现在场景里。
        foreach (var pickup in FindObjectsOfType<CluePickup>(true))
            if (clues.Contains(pickup.clueId)) pickup.gameObject.SetActive(false);

        if (Stage == StoryStage.ConfrontReady && target != null && storyPoint != null)
            target.MoveToStoryPoint(storyPoint);
        SetGameplayPaused(false);
    }

    /// <summary>
    /// 剧情暂停/恢复：暂停时禁用主角、敌人与感知组件并置 timeScale=0；
    /// 恢复时还原各组件原启用状态并置 timeScale=1。重复调用是安全的。
    /// </summary>
    public void SetGameplayPaused(bool paused)
    {
        if (paused)
        {
            if (gameplayPaused) { Time.timeScale = 0f; return; }
            gameplayPaused = true;
            pausedStates.Clear();
            // 记录并关闭玩法组件，避免暂停期间仍响应输入/移动。
            foreach (var h in FindObjectsOfType<HeroController>()) pausedStates[h] = h.enabled;
            foreach (var e in FindObjectsOfType<EnemyController>()) pausedStates[e] = e.enabled;
            foreach (var g in FindObjectsOfType<GuardSenses>()) pausedStates[g] = g.enabled;
            foreach (var kv in pausedStates) kv.Key.enabled = false;
            Time.timeScale = 0f;
        }
        else
        {
            gameplayPaused = false;
            foreach (var kv in pausedStates) if (kv.Key != null) kv.Key.enabled = kv.Value;
            pausedStates.Clear();
            Time.timeScale = 1f;
        }
    }
}
