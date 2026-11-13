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
