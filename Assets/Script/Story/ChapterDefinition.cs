using System;
using UnityEngine;

/// <summary>
/// 章节数据资产（ScriptableObject）。
/// 存放开场字幕、必需线索、对峙/抉择对话、三种结局字幕，以及跨章衔接与开场变体。
/// </summary>
[CreateAssetMenu(menuName = "Story/Chapter", fileName = "Chapter")]
public class ChapterDefinition : ScriptableObject
{
    /// <summary>按上一章结局选择的开场字幕变体。</summary>
    [Serializable]
    public class OpeningVariant
    {
        /// <summary>匹配的上一章结局 ID。</summary>
        public string previousEndingId;
        /// <summary>该变体使用的开场字幕。</summary>
        [TextArea(2, 4)] public string[] subtitles = new string[0];
    }

    /// <summary>章节结局定义（预留的通用结局结构）。</summary>
    [Serializable]
    public class EndingDefinition
    {
        /// <summary>结局 ID。</summary>
        public string id;
        /// <summary>结局字幕。</summary>
        [TextArea(2, 4)] public string[] subtitles = new string[0];
        /// <summary>是否在结局中击杀目标。</summary>
        public bool killTarget;
    }

    /// <summary>章节唯一 ID。</summary>
    public string chapterId = "chapter_01";
    /// <summary>章节标题（结局面板使用）。</summary>
    public string chapterTitle = "第一章 完";
    /// <summary>默认开场字幕。</summary>
    [TextArea(2, 4)] public string[] openingSubtitles = new string[0];
    /// <summary>调查阶段必须集齐的线索 ID。</summary>
    public string[] requiredClueIds = { "elder_testimony", "family_letter", "barracks_eavesdrop" };
    /// <summary>对峙对话。</summary>
    public DialogueAsset confrontDialogue;
    /// <summary>得知真相后的最终抉择对话。</summary>
    public DialogueAsset finalChoiceDialogue;
    /// <summary>直接击杀结局字幕。</summary>
    [TextArea(2, 4)] public string[] killEndingSubtitles = new string[0];
    /// <summary>得知真相后击杀结局字幕。</summary>
    [TextArea(2, 4)] public string[] killAfterTruthEndingSubtitles = new string[0];
    /// <summary>放走结局字幕。</summary>
    [TextArea(2, 4)] public string[] spareEndingSubtitles = new string[0];
    /// <summary>目标在 HUD 中的显示名。</summary>
    public string targetDisplayName = "卫兵队长";
    /// <summary>下一章定义（跨章衔接）。</summary>
    public ChapterDefinition nextChapter;
    /// <summary>开场变体列表。</summary>
    public OpeningVariant[] openingVariants = new OpeningVariant[0];
    /// <summary>通用结局定义列表（预留）。</summary>
    public EndingDefinition[] endings = new EndingDefinition[0];
}
