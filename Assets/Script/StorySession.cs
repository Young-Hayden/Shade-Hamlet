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
