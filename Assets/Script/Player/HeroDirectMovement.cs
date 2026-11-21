using UnityEngine;

/// <summary>
/// 直接动作模式的位移与动画驱动（WASD + Shift 奔跑 + Ctrl 潜行）。
/// 使用 CharacterController 做碰撞与重力，移动方向相对主摄像机；
/// 动画状态切换在这里统一维护，避免状态机被打回 Idle 造成"站着滑行"。
/// </summary>
public class HeroDirectMovement : MonoBehaviour
{
    /// <summary>主角控制器，未指定时自动获取。</summary>
    public HeroController hero;
    /// <summary>角色碰撞/位移组件，未指定时自动获取。</summary>
    public CharacterController controller;
    /// <summary>走路速度（米/秒）。</summary>
    public float walkSpeed = 4f;
    /// <summary>奔跑速度（米/秒）。</summary>
    public float runSpeed = 6f;
    /// <summary>转身插值速度。</summary>
    public float rotationSpeed = 12f;
    /// <summary>重力加速度，负数向下。</summary>
    public float gravity = -20f;
    /// <summary>当前竖直速度，用于贴地与下落。</summary>
    private float verticalVelocity;
    /// <summary>缺失动画状态是否已报错，避免每帧刷屏。</summary>
    private bool reportedMissingState;
    /// <summary>上一次请求的动画状态，用于去重，避免每帧重启 CrossFade。</summary>
    private string requestedState;
    /// <summary>潜行状态组件引用。</summary>
    private PlayerStealthState stealth;

    /// <summary>本帧是否在移动。</summary>
    public bool IsMoving { get; private set; }
    /// <summary>本帧是否在奔跑。</summary>
    public bool IsRunning { get; private set; }
    /// <summary>本帧是否处于潜行。</summary>
    public bool IsSneaking { get; private set; }

    void Awake()
    {
        if (hero == null) hero = GetComponent<HeroController>();
        if (controller == null) controller = GetComponent<CharacterController>();
        stealth = GetComponent<PlayerStealthState>();
    }

    void OnDisable()
    {
        // 组件被关闭时清空运动标记，防止动画状态残留。
        IsMoving = false;
        IsRunning = false;
        IsSneaking = false;
    }

    /// <summary>读取键盘输入并移动一帧（运行帧使用 Time.deltaTime）。</summary>
    public void Move()
    {
        Move(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
    }

    /// <summary>用给定输入移动一帧。</summary>
    public void Move(Vector2 input, bool running)
    {
        Move(input, running, Time.deltaTime);
    }

    // 显式步长入口让碰撞与移动可用确定性测试验证；正常运行仍使用 Time.deltaTime。
    /// <summary>
    /// 位移核心：
    /// 1) 过渡/死亡/攻击期间站定；
    /// 2) 输入相对相机方向换算为世界方向；
    /// 3) CharacterController 叠加重力移动，并朝移动方向转身；
    /// 4) 同步动画状态。
    /// </summary>
    public void Move(Vector2 input, bool running, float deltaTime)
    {
        if (SceneTransition.BlocksGameplay)
        {
            IsMoving = false;
            IsRunning = false;
            IsSneaking = false;
            return;
        }
        if (hero == null || hero.IsDead || controller == null || !controller.enabled)
        {
            IsMoving = false;
            IsRunning = false;
            IsSneaking = false;
            return;
        }
        // 攻击动画期间站定，避免人物一边挥剑一边滑行。
        if (hero.isAttack)
        {
            IsMoving = false;
            IsRunning = false;
            IsSneaking = false;
            return;
        }
        // 斜向输入限幅，避免对角线比直线更快。
        input = Vector2.ClampMagnitude(input, 1f);
        Camera cam = Camera.main;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();
        Vector3 planar = forward * input.y + right * input.x;
        bool moving = planar.sqrMagnitude > 0.001f;
        if (stealth == null) stealth = GetComponent<PlayerStealthState>();
        bool sneaking = stealth != null && stealth.IsSneaking;
        // 潜行优先级高于奔跑。
        bool actualRunning = running && !sneaking;
        IsMoving = moving;
        IsRunning = actualRunning && moving;
        IsSneaking = sneaking;

        // 贴地时给一个小的向下速度，保证 CharacterController 稳定接地。
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        else verticalVelocity += gravity * deltaTime;
        float speed = sneaking ? (stealth != null ? stealth.SneakSpeed : 2.2f) : actualRunning ? runSpeed : walkSpeed;
        controller.Move((planar * speed + Vector3.up * verticalVelocity) * deltaTime);

        if (moving)
        {
            // 朝移动方向平滑转身。
            Quaternion desired = Quaternion.LookRotation(planar.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, rotationSpeed * deltaTime);
        }
        UpdateAnimation(moving, actualRunning);
    }

    /// <summary>根据移动/奔跑/潜行/攻击状态驱动 Animator（状态去重，仅在变化时 CrossFade）。</summary>
    private void UpdateAnimation(bool moving, bool running)
    {
        if (hero.anim == null) return;
        // 状态机里 HumanoidWalk 带一条 walk==false 的退出过渡，AnyState 也由 walk 进入走路。
        // 直接模式若不维护这个布尔，CrossFade 进走路后会在 exitTime 被状态机打回 Idle，
        // 而下面的状态去重又不再重发 CrossFade，于是人物一直"站着滑行"（玩家看到的卡帧）。
        hero.anim.SetBool("walk", moving && !running && !IsSneaking && !hero.isAttack);
        if (hero.isAttack)
        {
            // 攻击状态由状态机自己控制，移动逻辑不插手。
            requestedState = null;
            return;
        }
        string desired = moving ? (IsSneaking ? "HumanoidSneak" : running ? "HumanoidRun" : "HumanoidWalk") : "HumanoidIdle";
        if (desired == requestedState) return;
        requestedState = desired;
        if (!hero.anim.HasState(0, Animator.StringToHash(desired)))
        {
            // 动画状态缺失属于配置错误：报一次错并停用本组件，避免持续空转。
            if (!reportedMissingState) Debug.LogError("Missing direct movement animator state: " + desired);
            reportedMissingState = true;
            enabled = false;
            return;
        }
        hero.anim.CrossFade(desired, 0.12f, 0);
    }
}
