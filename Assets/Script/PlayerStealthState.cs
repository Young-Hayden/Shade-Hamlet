using System.Collections.Generic;
using UnityEngine;

public enum PlayerStealthStatus
{
    Normal,
    Sneaking,
    Seen,
    Hidden,
    InSmoke
}

public class PlayerStealthState : MonoBehaviour
{
    public bool IsSneaking { get; private set; }
    public bool IsHidden { get { return activeCover.Count > 0; } }
    public bool IsInSmoke { get; private set; }
    public PlayerStealthStatus Status { get; private set; }
    public float SneakSpeed = 2.2f;
    public float SneakDetectionMultiplier = 0.65f;
    public float HiddenDetectionMultiplier = 0.25f;

    private readonly HashSet<StealthCoverArea> activeCover = new HashSet<StealthCoverArea>();
    private HeroController hero;
    private HeroDirectMovement movement;
    private bool scriptedSneaking;

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

    public void SetSneaking(bool value)
    {
        scriptedSneaking = value;
        IsSneaking = value && hero != null && !hero.IsDead && (movement == null || movement.enabled);
    }

    public void RegisterCover(StealthCoverArea area)
    {
        if (area != null) activeCover.Add(area);
    }

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
            scriptedSneaking = false;
            IsSneaking = false;
            IsInSmoke = false;
            Status = PlayerStealthStatus.Normal;
            activeCover.Clear();
            return;
        }

        bool canMove = movement != null && movement.enabled;
        IsSneaking = canMove && (Input.GetKey(KeyCode.LeftControl) || scriptedSneaking);

        IsInSmoke = SmokeZone.IsPointInside(transform.position);
        Vector3 bodyCenter = transform.position + Vector3.up * 1.0f;
        for (int i = StealthCoverArea.ActiveAreas.Count - 1; i >= 0; i--)
        {
            var area = StealthCoverArea.ActiveAreas[i];
            if (area == null) { StealthCoverArea.ActiveAreas.RemoveAt(i); continue; }
            if (area.Contains(bodyCenter)) RegisterCover(area);
            else UnregisterCover(area);
        }
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
