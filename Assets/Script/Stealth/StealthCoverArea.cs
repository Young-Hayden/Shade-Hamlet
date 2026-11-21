using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 隐藏区域（高草/灌木）：球体触发器，玩家进入后注册到 PlayerStealthState，
/// 从而降低被发现速度。区域本身不启用碰撞，不影响移动与 NavMesh。
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class StealthCoverArea : MonoBehaviour
{
    /// <summary>场景中所有启用的隐藏区域，供玩家每帧做包含检测。</summary>
    public static readonly List<StealthCoverArea> ActiveAreas = new List<StealthCoverArea>();
    /// <summary>该区域对应的被发现倍率（仅作记录，实际倍率由 PlayerStealthState 统一提供）。</summary>
    public float detectionMultiplier = 0.25f;
    /// <summary>球体触发范围。</summary>
    private SphereCollider triggerArea;

    void Awake()
    {
        triggerArea = GetComponent<SphereCollider>();
        triggerArea.isTrigger = true;
    }

    void OnEnable() { if (!ActiveAreas.Contains(this)) ActiveAreas.Add(this); }
    void OnDisable() { ActiveAreas.Remove(this); }

    /// <summary>判断世界坐标是否位于本区域内（考虑物体缩放）。</summary>
    public bool Contains(Vector3 worldPoint)
    {
        if (triggerArea == null) triggerArea = GetComponent<SphereCollider>();
        if (triggerArea == null) return false;
        // 取三轴缩放的最大值，避免非等比缩放导致判定偏小。
        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
        float radius = triggerArea.radius * Mathf.Max(0.01f, scale);
        return (worldPoint - transform.position).sqrMagnitude <= radius * radius;
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<HeroController>();
        if (player == null) return;
        var state = player.GetComponent<PlayerStealthState>();
        if (state != null) state.RegisterCover(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInParent<HeroController>();
        if (player == null) return;
        var state = player.GetComponent<PlayerStealthState>();
        if (state != null) state.UnregisterCover(this);
    }
}
