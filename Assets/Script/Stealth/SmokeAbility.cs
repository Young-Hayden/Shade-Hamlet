using UnityEngine;

/// <summary>
/// 烟雾能力（G 键）：在主角位置生成烟雾区域，持续 duration 秒，带冷却。
/// 烟雾区域同时提供视觉遮挡（GuardSenses）与玩家「处于烟雾」状态。
/// </summary>
[RequireComponent(typeof(HeroController))]
public class SmokeAbility : MonoBehaviour
{
    /// <summary>烟雾特效预制体。</summary>
    public GameObject smokePrefab;
    /// <summary>烟雾持续时间（秒）。</summary>
    public float duration = 4f;
    /// <summary>冷却时间（秒）。</summary>
    public float cooldown = 12f;
    /// <summary>烟雾半径。</summary>
    public float radius = 3f;
    /// <summary>下一次可用的时间点。</summary>
    private float readyAt;
    /// <summary>主角控制器。</summary>
    private HeroController hero;

    /// <summary>剩余冷却时间；0 表示可用。</summary>
    public float RemainingCooldown { get { return Mathf.Max(0f, readyAt - Time.time); } }

    void Awake() { hero = GetComponent<HeroController>(); }
    void Update()
    {
        if (GameplayInputGate.Blocked) return;
        if (Input.GetKeyDown(KeyCode.G)) TryActivate();
    }

    /// <summary>尝试释放烟雾：死亡、预制体缺失或冷却中返回 false。</summary>
    public bool TryActivate()
    {
        // 剧情/暂停/死亡/过渡期间不允许释放。
        if (GameplayInputGate.Blocked) return false;
        if (hero == null) hero = GetComponent<HeroController>();
        if (hero.IsDead || smokePrefab == null || Time.time < readyAt) return false;
        readyAt = Time.time + cooldown;
        var smoke = Instantiate(smokePrefab, transform.position, Quaternion.identity);
        // 特效预制体若未带 SmokeZone，运行时补一个，保证逻辑遮挡生效。
        var zone = smoke.GetComponent<SmokeZone>();
        if (zone == null) zone = smoke.AddComponent<SmokeZone>();
        zone.radius = radius;
        Destroy(smoke, duration);
        return true;
    }
}
