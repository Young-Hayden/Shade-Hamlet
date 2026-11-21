/// <summary>
/// 剧情检查点快照：主角死亡时由 StoryDirector 捕获，重新挑战时恢复。
/// 仅保存在内存中，不写磁盘存档。
/// </summary>
public class StoryCheckpoint
{
    /// <summary>当前检查点；null 表示没有可恢复的进度。</summary>
    public static StoryCheckpoint Current;
    /// <summary>捕获时的剧情阶段。</summary>
    public StoryStage stage;
    /// <summary>已收集的线索 ID。</summary>
    public string[] clues = new string[0];
    /// <summary>是否已知真相。</summary>
    public bool truthLearned;
    /// <summary>结局 ID（死亡时通常为空）。</summary>
    public string endingId;
}
