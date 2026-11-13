using UnityEngine;
using UnityEngine.SceneManagement;

public class CLick : MonoBehaviour
{
    // 开始游戏：开始界面 → RPG游戏场景
    public void ButtonClick()
    {
        Debug.Log("Button Clicked");
        StorySession.Reset();
        SceneManager.LoadScene("RPG游戏");
    }

    // 退出游戏：RPG游戏场景 → 返回开始界面
    public void BackToStart()
    {
        Debug.Log("Back To Start");
        SceneManager.LoadScene("界面");
    }

    // 退出游戏：开始界面和菜单共用。
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
