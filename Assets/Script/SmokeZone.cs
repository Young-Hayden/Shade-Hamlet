using System.Collections.Generic;
using UnityEngine;

public class SmokeZone : MonoBehaviour
{
    public float radius = 3f;
    private static readonly List<SmokeZone> activeZones = new List<SmokeZone>();

    void OnEnable() { if (!activeZones.Contains(this)) activeZones.Add(this); }
    void OnDisable() { activeZones.Remove(this); }

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

    private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared < 0.0001f) return Vector3.Distance(point, start);
        float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
        return Vector3.Distance(point, start + segment * t);
    }
}
