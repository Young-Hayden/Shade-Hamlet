using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 世界空间血条控制：让血条始终面向主摄像机，并更新 Slider 数值。
/// 同时保留主菜单/旧入口使用的退出与进入游戏按钮方法。
/// </summary>
public class HPScript : MonoBehaviour
{
    // 定义一个slider类型变量
    /// <summary>血条 Slider；值为 0～1。</summary>
    public Slider hpSlider;

    // 缓存主摄像机，避免每帧Camera.main做全场景标签查找。
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    // 放在LateUpdate：等相机跟随主角移动完再对齐，血条不会慢一拍。
    void LateUpdate()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 让画布与血条一直对着摄像机（World Space Canvas正面为+Z）。
        Vector3 dir = mainCamera.transform.position - transform.position;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    /// <summary>刷新血条显示；最大血量无效时忽略。</summary>
    public void SetHP(float hpNow, float hpMax)
    {
        if (hpSlider == null || hpMax <= 0f) return;
        // 夹紧到0~1，血量不会出现负数血条。
        hpSlider.value = Mathf.Clamp01(hpNow / hpMax);
    }

    // 创建一个退出游戏函数
    /// <summary>退出游戏：编辑器内停止 Play，打包后退出应用。</summary>
    public void ExitGame()
    {
#if UNITY_EDITOR
        // 编辑器专用
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 玩家专用
        Application.Quit();
#endif
    }

    // 创建一个进入"RPG游戏"场景的函数
    /// <summary>旧入口：带过渡进入 RPG游戏 场景。</summary>
    public void EnterRPGGame()
    {
        SceneTransition.LoadScene("RPG游戏", "第一章 · 回村的人", "调查真相 · 潜行抉择");
    }
}
