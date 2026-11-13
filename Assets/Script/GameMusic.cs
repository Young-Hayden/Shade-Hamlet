using UnityEngine;

public class GameMusic : MonoBehaviour
{
    // 全局唯一实例，战斗脚本直接通过GameMusic.Instance播放音效，
    // 不再每次攻击都GameObject.Find("GameMusic")（找不到还会抛空引用）。
    public static GameMusic Instance { get; private set; }

    // 音效源组件
    public AudioSource audioSource;
    // 音效数组
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

    // 创建音效播放函数
    public void PlayMusic(int index)
    {
        if (audioSource == null || sfx == null) return;
        if (index < 0 || index >= sfx.Length || sfx[index] == null) return;
        audioSource.PlayOneShot(sfx[index]);
    }
}
