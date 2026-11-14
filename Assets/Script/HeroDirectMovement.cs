using UnityEngine;

public class HeroDirectMovement : MonoBehaviour
{
    public HeroController hero;
    public CharacterController controller;
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public float rotationSpeed = 12f;
    public float gravity = -20f;
    private float verticalVelocity;
    private bool reportedMissingState;
    private string requestedState;
    private PlayerStealthState stealth;

    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsSneaking { get; private set; }

    void Awake()
    {
        if (hero == null) hero = GetComponent<HeroController>();
        if (controller == null) controller = GetComponent<CharacterController>();
        stealth = GetComponent<PlayerStealthState>();
    }

    void OnDisable()
    {
        IsMoving = false;
        IsRunning = false;
        IsSneaking = false;
    }

    public void Move()
    {
        Move(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
    }

    public void Move(Vector2 input, bool running)
    {
        Move(input, running, Time.deltaTime);
    }

    // 显式步长入口让碰撞与移动可用确定性测试验证；正常运行仍使用 Time.deltaTime。
    public void Move(Vector2 input, bool running, float deltaTime)
    {
        if (hero == null || hero.IsDead || controller == null || !controller.enabled)
        {
            IsMoving = false;
            IsRunning = false;
            IsSneaking = false;
            return;
        }
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
        bool actualRunning = running && !sneaking;
        IsMoving = moving;
        IsRunning = actualRunning && moving;
        IsSneaking = sneaking;

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        else verticalVelocity += gravity * deltaTime;
        float speed = sneaking ? (stealth != null ? stealth.SneakSpeed : 2.2f) : actualRunning ? runSpeed : walkSpeed;
        controller.Move((planar * speed + Vector3.up * verticalVelocity) * deltaTime);

        if (moving)
        {
            Quaternion desired = Quaternion.LookRotation(planar.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, rotationSpeed * deltaTime);
        }
        UpdateAnimation(moving, actualRunning);
    }

    private void UpdateAnimation(bool moving, bool running)
    {
        if (hero.anim == null) return;
        // 状态机里 HumanoidWalk 带一条 walk==false 的退出过渡，AnyState 也由 walk 进入走路。
        // 直接模式若不维护这个布尔，CrossFade 进走路后会在 exitTime 被状态机打回 Idle，
        // 而下面的状态去重又不再重发 CrossFade，于是人物一直"站着滑行"（玩家看到的卡帧）。
        hero.anim.SetBool("walk", moving && !running && !IsSneaking && !hero.isAttack);
        if (hero.isAttack)
        {
            requestedState = null;
            return;
        }
        string desired = moving ? (IsSneaking ? "HumanoidSneak" : running ? "HumanoidRun" : "HumanoidWalk") : "HumanoidIdle";
        if (desired == requestedState) return;
        requestedState = desired;
        if (!hero.anim.HasState(0, Animator.StringToHash(desired)))
        {
            if (!reportedMissingState) Debug.LogError("Missing direct movement animator state: " + desired);
            reportedMissingState = true;
            enabled = false;
            return;
        }
        hero.anim.CrossFade(desired, 0.12f, 0);
    }
}
