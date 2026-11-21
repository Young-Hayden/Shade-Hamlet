using System;
using UnityEngine;

/// <summary>一次噪音事件：位置、传播半径、强度与来源。</summary>
public sealed class NoiseEvent
{
    /// <summary>噪音发生的位置。</summary>
    public Vector3 Position;
    /// <summary>可被听到的半径。</summary>
    public float Radius;
    /// <summary>强度 0～1，影响警觉度。</summary>
    public float Strength;
    /// <summary>发生时间。</summary>
    public float Time;
    /// <summary>噪音来源变换。</summary>
    public Transform Source;
}

/// <summary>
/// 玩家脚步/攻击噪音发射器。
/// 按走路/奔跑/潜行以不同间隔与半径发出 <see cref="NoiseEvent"/>，卫兵通过静态事件订阅。
/// </summary>
public class NoiseEmitter : MonoBehaviour
{
    /// <summary>全局噪音事件；卫兵在 OnEnable/OnDisable 中订阅与退订。</summary>
    public static event Action<NoiseEvent> OnNoise;

    /// <summary>走路噪音半径。</summary>
    public float walkRadius = 2.5f;
    /// <summary>奔跑噪音半径。</summary>
    public float runRadius = 6f;
    /// <summary>潜行噪音半径。</summary>
    public float sneakRadius = 0.8f;
    /// <summary>走路脚步间隔。</summary>
    public float walkInterval = 0.45f;
    /// <summary>奔跑脚步间隔。</summary>
    public float runInterval = 0.28f;
    /// <summary>潜行脚步间隔。</summary>
    public float sneakInterval = 0.65f;

    /// <summary>主角控制器。</summary>
    private HeroController hero;
    /// <summary>直接移动组件（读取移动/奔跑状态）。</summary>
    private HeroDirectMovement movement;
    /// <summary>潜行状态组件。</summary>
    private PlayerStealthState stealth;
    /// <summary>下一次允许发出脚步噪音的时间。</summary>
    private float nextFootstep;

    void Awake()
    {
        hero = GetComponent<HeroController>();
        movement = GetComponent<HeroDirectMovement>();
        stealth = GetComponent<PlayerStealthState>();
    }

    void Update()
    {
        // 仅在直接移动模式下、角色存活且正在移动时产生脚步噪音。
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

    /// <summary>发出一次噪音事件；非法半径/强度直接忽略。</summary>
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
