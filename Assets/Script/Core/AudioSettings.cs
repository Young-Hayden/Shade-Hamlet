using UnityEngine;

/// <summary>
/// 静态音频设置：统一维护主音量，并写入 PlayerPrefs 持久化。
/// 游戏启动前自动执行 Initialize，把上次保存的音量应用到 AudioListener。
/// </summary>
public static class AudioSettings
{
    /// <summary>PlayerPrefs 中保存主音量的键名。</summary>
    private const string MasterVolumeKey = "masterVolume";

    /// <summary>当前主音量，范围 0～1。</summary>
    public static float MasterVolume { get; private set; } = 1f;

    /// <summary>在首个场景加载前读取存档音量并应用，保证菜单与游戏的音量一致。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        AudioListener.volume = MasterVolume;
    }

    /// <summary>设置主音量：立即应用并写入 PlayerPrefs。</summary>
    public static void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        AudioListener.volume = MasterVolume;
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
    }

    /// <summary>把 PlayerPrefs 刷写到磁盘；音量面板关闭时调用。</summary>
    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
