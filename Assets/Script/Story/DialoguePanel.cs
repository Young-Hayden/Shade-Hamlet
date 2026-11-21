using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 剧情对话框：底部面板显示说话人与台词，逐行推进，台词读完后显示最多两个选项。
/// 选项回调交给 StoryDirector；面板在 timeScale=0 下仍可通过输入推进。
/// </summary>
public class DialoguePanel : MonoBehaviour
{
    /// <summary>对话是否正在显示。</summary>
    public bool IsShowing { get; private set; }
    /// <summary>说话人文本。</summary>
    private Text speakerText;
    /// <summary>台词正文。</summary>
    private Text bodyText;
    /// <summary>两个选项按钮（资产最多支持 2 个选项）。</summary>
    private readonly List<Button> choiceButtons = new List<Button>();
    /// <summary>当前播放的对话资产。</summary>
    private DialogueAsset asset;
    /// <summary>关闭时的回调（携带所选选项，null 表示没有选项）。</summary>
    private Action<DialogueAsset.Choice> onChoice;
    /// <summary>当前台词索引。</summary>
    private int lineIndex;
    /// <summary>台词是否已读完、正在等待选择。</summary>
    private bool choicesShown;

    /// <summary>运行时创建对话框 Canvas 与控件，并挂载本组件。</summary>
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
            // 捕获循环变量，避免闭包共享同一个索引。
            int captured = i;
            var button = RpgUI.CreateButton("Choice" + i, bg.transform, null, "", new Vector2(-175f + i * 350f, -95f), new Vector2(320f, 56f));
            // 深色底以配合剧情氛围，保证文字可读。
            button.image.color = new Color(0.16f, 0.11f, 0.08f, 0.98f);
            button.onClick.AddListener(() => panel.Choose(captured));
            panel.choiceButtons.Add(button);
        }
        canvas.gameObject.SetActive(false);
        return panel;
    }

    /// <summary>开始显示一段对话。</summary>
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

    /// <summary>渲染当前台词；台词越界则进入选项阶段。</summary>
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

    /// <summary>显示选项；资产没有选项时直接关闭对话。</summary>
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

    /// <summary>推进到下一行台词；正在等待选择时不响应。</summary>
    public void Advance()
    {
        if (!IsShowing || choicesShown) return;
        lineIndex++;
        RenderLine();
    }

    /// <summary>选择第 index 个选项并关闭对话。</summary>
    public void Choose(int index)
    {
        if (!IsShowing || !choicesShown || index < 0 || index >= asset.choices.Count) return;
        Close(asset.choices[index]);
    }

    /// <summary>关闭面板并触发一次回调（回调清空，避免重复触发）。</summary>
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
        // 暂停菜单打开时不推进对话，避免一次按键同时触发两个 UI。
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) return;
        // E 或鼠标左键推进台词；选择阶段由按钮响应。
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) Advance();
    }
}
