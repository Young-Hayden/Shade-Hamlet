using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class StealthCoverArea : MonoBehaviour
{
    public static readonly List<StealthCoverArea> ActiveAreas = new List<StealthCoverArea>();
    public float detectionMultiplier = 0.25f;
    private SphereCollider triggerArea;

    void Awake()
    {
        triggerArea = GetComponent<SphereCollider>();
        triggerArea.isTrigger = true;
    }

    void OnEnable() { if (!ActiveAreas.Contains(this)) ActiveAreas.Add(this); }
    void OnDisable() { ActiveAreas.Remove(this); }

    public bool Contains(Vector3 worldPoint)
    {
        if (triggerArea == null) triggerArea = GetComponent<SphereCollider>();
        if (triggerArea == null) return false;
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
