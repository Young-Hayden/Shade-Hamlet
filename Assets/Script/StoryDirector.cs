using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StoryStage { Opening, Investigate, ConfrontReady, Confront, Ending }

public class StoryDirector : MonoBehaviour
{
    public static StoryDirector Instance { get; private set; }

    public ChapterDefinition chapter;
    public StoryTarget target;
    public Transform storyPoint;

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
    private bool gameplayPaused;

    void Awake() { Instance = this; Stage = StoryStage.Opening; }
    void OnDisable()
    {
        if (Instance == this)
        {
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

    private IEnumerator InitializeStory()
    {
        // 等所有角色 Start 完成 HP/引用初始化；若从主菜单带过渡进入，等黑场淡入结束再开始剧情。
        yield return null;
        if (SceneTransition.BlocksGameplay) yield return SceneTransition.WaitForGameplayReady();
        if (StoryCheckpoint.Current != null) ApplyCheckpoint();
        else PlayOpening();
    }

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

    public void RegisterClue(string clueId)
    {
        if (chapter == null || string.IsNullOrEmpty(clueId)) return;
        if (Array.IndexOf(chapter.requiredClueIds, clueId) < 0)
        {
            Debug.LogError("StoryDirector: 未声明的线索 " + clueId);
            return;
        }
        if (!clues.Add(clueId)) return;
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
        if (IsUiOpen) return;
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
        openingPlayed = true;
        Stage = cp.stage == StoryStage.Ending ? StoryStage.Investigate : cp.stage;
        if (Stage == StoryStage.Confront) Stage = StoryStage.ConfrontReady;
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

    public void SetGameplayPaused(bool paused)
    {
        if (paused)
        {
            if (gameplayPaused) { Time.timeScale = 0f; return; }
            gameplayPaused = true;
            pausedStates.Clear();
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
