using System;
using UnityEngine;

public sealed class NoiseEvent
{
    public Vector3 Position;
    public float Radius;
    public float Strength;
    public float Time;
    public Transform Source;
}

public class NoiseEmitter : MonoBehaviour
{
    public static event Action<NoiseEvent> OnNoise;

    public float walkRadius = 2.5f;
    public float runRadius = 6f;
    public float sneakRadius = 0.8f;
    public float walkInterval = 0.45f;
    public float runInterval = 0.28f;
    public float sneakInterval = 0.65f;

    private HeroController hero;
    private HeroDirectMovement movement;
    private PlayerStealthState stealth;
    private float nextFootstep;

    void Awake()
    {
        hero = GetComponent<HeroController>();
        movement = GetComponent<HeroDirectMovement>();
        stealth = GetComponent<PlayerStealthState>();
    }

    void Update()
    {
        if (hero == null || hero.IsDead || !hero.enabled || movement == null || !movement.enabled || !hero.directControl || !movement.IsMoving) return;
        if (Time.time < nextFootstep) return;
        bool sneaking = stealth != null && stealth.IsSneaking;
        bool running = movement.IsRunning && !sneaking;
        float radius = sneaking ? sneakRadius : running ? runRadius : walkRadius;
        float interval = sneaking ? sneakInterval : running ? runInterval : walkInterval;
        float strength = sneaking ? 0.2f : running ? 0.85f : 0.45f;
        EmitNoise(radius, strength, transform.position);
        nextFootstep = Time.time + interval;
    }

    public void EmitNoise(float radius, float strength, Vector3 position)
    {
        if (radius <= 0f || strength <= 0f) return;
        OnNoise?.Invoke(new NoiseEvent
        {
            Position = position,
            Radius = radius,
            Strength = Mathf.Clamp01(strength),
            Time = UnityEngine.Time.time,
            Source = transform
        });
    }
}
