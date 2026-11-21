using System.Collections.Generic;
using UnityEngine;

/// <summary>玩家潜行显示状态，供 HUD 展示。</summary>
public enum PlayerStealthStatus
{
    /// <summary>普通状态。</summary>
    Normal,
    /// <summary>按住 Ctrl 潜行。</summary>
    Sneaking,
    /// <summary>已被卫兵发现（追击中）。</summary>
    Seen,
    /// <summary>处于高草/灌木等隐藏区域。</summary>
    Hidden,
    /// <summary>处于烟雾中。</summary>
    InSmoke
}

/// <summary>
/// 玩家潜行状态汇总：Ctrl 潜行、隐藏区域、烟雾区域，以及被追击状态。
/// 对外提供 DetectionMultiplier，供 GuardSenses 调整发现速度。
/// </summary>
public class PlayerStealthState : MonoBehaviour
{
    /// <summary>当前是否处于潜行。</summary>
    public bool IsSneaking { get; private set; }
    /// <summary>是否位于至少一个隐藏区域内。</summary>
    public bool IsHidden { get { return activeCover.Count > 0; } }
    /// <summary>是否位于烟雾内。</summary>
    public bool IsInSmoke { get; private set; }
    /// <summary>综合显示状态。</summary>
    public PlayerStealthStatus Status { get; private set; }
    /// <summary>潜行移动速度（米/秒）。</summary>
    public float SneakSpeed = 2.2f;
    /// <summary>潜行时被发现的倍率（越小越难被发现）。</summary>
    public float SneakDetectionMultiplier = 0.65f;
    /// <summary>藏在草丛/灌木中时被发现的倍率。</summary>
    public float HiddenDetectionMultiplier = 0.25f;

    /// <summary>当前所处的隐藏区域集合。</summary>
    private readonly HashSet<StealthCoverArea> activeCover = new HashSet<StealthCoverArea>();
    /// <summary>主角控制器。</summary>
    private HeroController hero;
    /// <summary>直接移动组件（判断是否允许潜行）。</summary>
    private HeroDirectMovement movement;
    /// <summary>脚本/测试设置的潜行标记，与键盘输入取或。</summary>
    private bool scriptedSneaking;

    /// <summary>被发现的倍率：隐藏优先，其次潜行，否则正常。</summary>
    public float DetectionMultiplier
    {
        get
        {
            if (IsHidden) return HiddenDetectionMultiplier;
            if (IsSneaking) return SneakDetectionMultiplier;
            return 1f;
        }
    }

    void Awake()
    {
        hero = GetComponent<HeroController>();
        movement = GetComponent<HeroDirectMovement>();
        IsInSmoke = false;
        Status = PlayerStealthStatus.Normal;
    }

    /// <summary>由脚本/测试设置潜行状态；主角死亡或移动组件关闭时无效。</summary>
    public void SetSneaking(bool value)
    {
        scriptedSneaking = value;
        IsSneaking = value && hero != null && !hero.IsDead && (movement == null || movement.enabled);
    }

    /// <summary>进入隐藏区域。</summary>
    public void RegisterCover(StealthCoverArea area)
    {
        if (area != null) activeCover.Add(area);
    }

    /// <summary>离开隐藏区域。</summary>
    public void UnregisterCover(StealthCoverArea area)
    {
        if (area != null) activeCover.Remove(area);
    }

    void Update()
    {
        if (hero == null) hero = GetComponent<HeroController>();
        if (movement == null) movement = GetComponent<HeroDirectMovement>();
        if (hero == null || hero.IsDead)
        {
            // 死亡时清空所有潜行标记。
            scriptedSneaking = false;
            IsSneaking = false;
            IsInSmoke = false;
            Status = PlayerStealthStatus.Normal;
            activeCover.Clear();
            return;
        }

        bool canMove = movement != null && movement.enabled;
        // Ctrl 按住即潜行；直接移动组件关闭时不潜行。
        IsSneaking = canMove && (Input.GetKey(KeyCode.LeftControl) || scriptedSneaking);

        // 每帧刷新烟雾与隐藏区域；区域对象被销毁时顺带清理列表。
        IsInSmoke = SmokeZone.IsPointInside(transform.position);
        Vector3 bodyCenter = transform.position + Vector3.up * 1.0f;
        for (int i = StealthCoverArea.ActiveAreas.Count - 1; i >= 0; i--)
        {
            var area = StealthCoverArea.ActiveAreas[i];
            if (area == null) { StealthCoverArea.ActiveAreas.RemoveAt(i); continue; }
            if (area.Contains(bodyCenter)) RegisterCover(area);
            else UnregisterCover(area);
        }
        // 被追击状态优先级最高，用于 HUD 提示。
        bool seen = false;
        foreach (var guard in FindObjectsOfType<GuardSenses>())
        {
            if (guard != null && guard.State == GuardState.Chase) { seen = true; break; }
        }
        if (seen) Status = PlayerStealthStatus.Seen;
        else if (IsInSmoke) Status = PlayerStealthStatus.InSmoke;
        else if (IsHidden) Status = PlayerStealthStatus.Hidden;
        else if (IsSneaking) Status = PlayerStealthStatus.Sneaking;
        else Status = PlayerStealthStatus.Normal;
    }

    void OnDisable()
    {
        activeCover.Clear();
        IsSneaking = false;
        IsInSmoke = false;
        Status = PlayerStealthStatus.Normal;
    }
}
