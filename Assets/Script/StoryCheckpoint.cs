public class StoryCheckpoint
{
    public static StoryCheckpoint Current;
    public StoryStage stage;
    public string[] clues = new string[0];
    public bool truthLearned;
    public string endingId;
}
