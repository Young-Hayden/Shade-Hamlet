/// <summary>
/// 跨场景的剧情会话状态（纯静态，不挂场景）。
/// 主菜单选择「下一章」时写入 PendingChapterId；章节开始时记录上一结局，供开场变体选择。
/// </summary>
public static class StorySession
{
    /// <summary>待进入的章节 ID；为空表示按场景默认章节开始。</summary>
    public static string PendingChapterId;
    /// <summary>上一章结算的结局 ID，用于选择开场变体。</summary>
    public static string PreviousEndingId;

    /// <summary>清空会话状态（返回主界面或开始新游戏时调用）。</summary>
    public static void Reset()
    {
        PendingChapterId = null;
        PreviousEndingId = null;
        StoryCheckpoint.Current = null;
    }
}
