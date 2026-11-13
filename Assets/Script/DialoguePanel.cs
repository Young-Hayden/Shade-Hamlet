using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePanel : MonoBehaviour
{
    public bool IsShowing { get; private set; }
    private Text speakerText;
    private Text bodyText;
    private readonly List<Button> choiceButtons = new List<Button>();
    private DialogueAsset asset;
    private Action<DialogueAsset.Choice> onChoice;
    private int lineIndex;
    private bool choicesShown;

    public static DialoguePanel Create(Transform parent)
    {
        var canvas = RpgUI.CreateCanvas("DialogueCanvas", parent, true);
        var panel = canvas.gameObject.AddComponent<DialoguePanel>();
        var bg = RpgUI.CreateImage("Background", canvas.transform, null, new Color(0.05f, 0.05f, 0.08f, 0.92f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1400f, 260f));
        panel.speakerText = RpgUI.CreateText("Speaker", bg.transform, "", 26, new Color(1f, 0.9f, 0.72f), TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(1200f, 40f));
        panel.bodyText = RpgUI.CreateText("Body", bg.transform, "", 26, Color.white, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -70f), new Vector2(1340f, 120f));
        for (int i = 0; i < 2; i++)
        {
            int captured = i;
            var button = RpgUI.CreateButton("Choice" + i, bg.transform, null, "", new Vector2(-175f + i * 350f, -95f), new Vector2(320f, 56f));
            button.image.color = new Color(0.16f, 0.11f, 0.08f, 0.98f);
            button.onClick.AddListener(() => panel.Choose(captured));
            panel.choiceButtons.Add(button);
        }
        canvas.gameObject.SetActive(false);
        return panel;
    }

    public void Show(DialogueAsset asset, Action<DialogueAsset.Choice> onChoice)
    {
        if (asset == null) { Debug.LogError("DialoguePanel.Show: 空对话资产"); return; }
        this.asset = asset;
        this.onChoice = onChoice;
        lineIndex = 0;
        choicesShown = false;
        gameObject.SetActive(true);
        IsShowing = true;
        foreach (var b in choiceButtons) b.gameObject.SetActive(false);
        RenderLine();
    }

    private void RenderLine()
    {
        if (lineIndex < asset.lines.Count)
        {
            speakerText.text = asset.lines[lineIndex].speaker;
            bodyText.text = asset.lines[lineIndex].text;
            return;
        }
        ShowChoices();
    }

    private void ShowChoices()
    {
        choicesShown = true;
        if (asset.choices.Count == 0) { Close(null); return; }
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            bool used = i < asset.choices.Count;
            choiceButtons[i].gameObject.SetActive(used);
            if (used) choiceButtons[i].GetComponentInChildren<Text>().text = asset.choices[i].text;
        }
    }

    public void Advance()
    {
        if (!IsShowing || choicesShown) return;
        lineIndex++;
        RenderLine();
    }

    public void Choose(int index)
    {
        if (!IsShowing || !choicesShown || index < 0 || index >= asset.choices.Count) return;
        Close(asset.choices[index]);
    }

    private void Close(DialogueAsset.Choice choice)
    {
        IsShowing = false;
        gameObject.SetActive(false);
        var callback = onChoice;
        onChoice = null;
        callback?.Invoke(choice);
    }

    void Update()
    {
        if (!IsShowing) return;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) Advance();
    }
}
