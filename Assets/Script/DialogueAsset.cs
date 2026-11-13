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
        public DialogueAsset next;
        public string actionId;
    }

    public string id;
    public List<Line> lines = new List<Line>();
    public List<Choice> choices = new List<Choice>();
}
