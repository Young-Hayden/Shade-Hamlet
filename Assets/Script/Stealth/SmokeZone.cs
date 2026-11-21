using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 烟雾区域：注册到全局列表，向感知系统提供视线遮挡与「点在烟雾内」查询。
/// 用线段到圆心的距离判断视线是否穿过烟雾，不依赖物理碰撞。
/// </summary>
public class SmokeZone : MonoBehaviour
{
    /// <summary>烟雾半径。</summary>
    public float radius = 3f;
    /// <summary>当前场景中启用的烟雾区域。</summary>
    private static readonly List<SmokeZone> activeZones = new List<SmokeZone>();

    void OnEnable() { if (!activeZones.Contains(this)) activeZones.Add(this); }
    void OnDisable() { activeZones.Remove(this); }

    /// <summary>判断视线段（眼睛→胸口）是否被任一烟雾区域遮挡。</summary>
    public static bool BlocksSight(Vector3 eye, Vector3 chest)
    {
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            var zone = activeZones[i];
            if (zone == null) { activeZones.RemoveAt(i); continue; }
            if (DistanceToSegment(zone.transform.position, eye, chest) <= zone.radius) return true;
        }
        return false;
    }

    /// <summary>判断点是否位于任一烟雾区域内。</summary>
    public static bool IsPointInside(Vector3 point)
    {
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            var zone = activeZones[i];
            if (zone == null) { activeZones.RemoveAt(i); continue; }
            if ((zone.transform.position - point).sqrMagnitude <= zone.radius * zone.radius) return true;
        }
        return false;
    }

    /// <summary>点到线段的最近距离；线段退化时退化为点到点距离。</summary>
    private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared < 0.0001f) return Vector3.Distance(point, start);
        float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
        return Vector3.Distance(point, start + segment * t);
    }
}
