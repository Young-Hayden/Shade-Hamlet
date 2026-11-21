using UnityEngine;

/// <summary>
/// 全局音效播放器（单例）。
/// 战斗等系统通过 GameMusic.Instance.PlayMusic(index) 播放一次性音效，
/// 避免重复使用 GameObject.Find 造成空引用。
/// </summary>
public class GameMusic : MonoBehaviour
{
    // 全局唯一实例，战斗脚本直接通过 GameMusic.Instance 播放音效，
    // 不再每次攻击都 GameObject.Find("GameMusic")（找不到还会抛空引用）。
    public static GameMusic Instance { get; private set; }

    /// <summary>音效播放源；未在 Inspector 指定时自动从本对象获取。</summary>
    public AudioSource audioSource;
    /// <summary>可播放的音效剪辑数组，索引由调用方约定。</summary>
    public AudioClip[] sfx;

    void Awake()
    {
        Instance = this;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>按数组索引播放一次音效；索引越界或剪辑为空时静默忽略。</summary>
    public void PlayMusic(int index)
    {
        if (audioSource == null || sfx == null) return;
        if (index < 0 || index >= sfx.Length || sfx[index] == null) return;
        audioSource.PlayOneShot(sfx[index]);
    }
}
