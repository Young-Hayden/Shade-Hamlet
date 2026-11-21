using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话数据资产（ScriptableObject）：若干台词 + 若干选项。
/// 选项可指向下一段对话，或携带 actionId 由 StoryDirector 执行剧情动作。
/// </summary>
[CreateAssetMenu(menuName = "Story/Dialogue", fileName = "Dialogue")]
public class DialogueAsset : ScriptableObject
{
    /// <summary>一条台词。</summary>
    [Serializable]
    public class Line
    {
        /// <summary>说话人显示名。</summary>
        public string speaker;
        /// <summary>台词内容。</summary>
        [TextArea(2, 4)] public string text;
    }

    /// <summary>一个对话选项。</summary>
    [Serializable]
    public class Choice
    {
        /// <summary>选项 ID（用于测试与引用）。</summary>
        public string choiceId;
        /// <summary>按钮文本。</summary>
        public string text;
        /// <summary>选择后进入的下一段对话；为空则结束对话。</summary>
        public DialogueAsset next;
        /// <summary>选择后触发的剧情动作 ID（由 StoryDirector.ApplyAction 解释）。</summary>
        public string actionId;
    }

    /// <summary>对话唯一 ID。</summary>
    public string id;
    /// <summary>台词序列。</summary>
    public List<Line> lines = new List<Line>();
    /// <summary>选项列表；为空时对话只有「继续」。</summary>
    public List<Choice> choices = new List<Choice>();
}
