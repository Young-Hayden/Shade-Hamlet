using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 主菜单/开始界面的按钮回调（旧命名保留，避免破坏已接线的 Button 事件）。
/// 开始游戏、返回开始界面、退出游戏。
/// </summary>
public class CLick : MonoBehaviour
{
    // 开始游戏：开始界面 → RPG游戏场景
    /// <summary>开始新游戏：清空会话状态并带过渡进入 RPG游戏。</summary>
    public void ButtonClick()
    {
        Debug.Log("Button Clicked");
        StorySession.Reset();
        SceneTransition.LoadScene("RPG游戏", "第一章 · 回村的人", "调查真相 · 潜行抉择");
    }

    // 退出游戏：RPG游戏场景 → 返回开始界面
    /// <summary>返回主菜单场景。</summary>
    public void BackToStart()
    {
        Debug.Log("Back To Start");
        SceneTransition.LoadScene("界面", "影村", "");
    }

    // 退出游戏：开始界面和菜单共用。
    /// <summary>退出游戏：编辑器内停止 Play，打包后退出应用。</summary>
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
