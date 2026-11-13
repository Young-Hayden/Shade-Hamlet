using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HPScript : MonoBehaviour
{
    // 定义一个slider类型变量
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

    public void SetHP(float hpNow, float hpMax)
    {
        if (hpSlider == null || hpMax <= 0f) return;
        // 夹紧到0~1，血量不会出现负数血条。
        hpSlider.value = Mathf.Clamp01(hpNow / hpMax);
    }

    // 创建一个退出游戏函数
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
    public void EnterRPGGame()
    {
        // Application.LoadLevel已弃用，改用SceneManager。
        SceneManager.LoadScene("RPG游戏");
    }
}
