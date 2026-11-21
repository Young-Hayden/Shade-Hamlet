using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 第三人称跟随相机。
/// 功能：鼠标锁定时光标隐藏并驱动 yaw/pitch，滚轮缩放，遮挡检测，以及按探索/潜行/交战切换距离。
/// 剧情、暂停、对话（timeScale=0）时自动解锁光标，恢复游戏后重新锁定。
/// </summary>
public class HeroCamera : MonoBehaviour
{
    /// <summary>跟随目标（主角根节点）。</summary>
    public Transform HeroController;
    /// <summary>相机相对目标的基础偏移，同时决定默认轨道距离。</summary>
    public Vector3 offset = new Vector3(0f, 5f, 4f);
    /// <summary>位置平滑时间，0 表示不平滑。</summary>
    public float smoothTime = 0.12f;

    [Header("缩放与旋转")]
    /// <summary>最近缩放距离。</summary>
    public float minDistance = 4f;
    /// <summary>最远缩放距离。</summary>
    public float maxDistance = 12f;
    /// <summary>滚轮缩放速度。</summary>
    public float zoomSpeed = 1.5f;
    /// <summary>相机与遮挡物之间保留的间距。</summary>
    public float obstructionPadding = 0.25f;

    [Header("鼠标视角")]
    /// <summary>是否启用鼠标锁定与视角控制。</summary>
    public bool lockCursor = true;
    /// <summary>鼠标灵敏度。</summary>
    public float mouseSensitivity = 2.5f;
    /// <summary>最低俯仰角。</summary>
    public float minPitch = -30f;
    /// <summary>最高俯仰角。</summary>
    public float maxPitch = 70f;

    [Header("情境距离")]
    /// <summary>是否按战斗/潜行/探索切换相机距离。</summary>
    public bool useContextDistance = true;
    /// <summary>探索状态距离；<=0 时使用初始距离。</summary>
    public float explorationDistance;
    /// <summary>潜行状态距离；<=0 时使用初始距离的 90%。</summary>
    public float stealthDistance;
    /// <summary>交战状态距离；<=0 时使用初始距离的 105%。</summary>
    public float combatDistance;
    /// <summary>当前情境名（Explore/Stealth/Combat），用于避免重复切换。</summary>
    private string cameraContext;

    /// <summary>基础偏移（保持场景配置的原值）。</summary>
    private Vector3 baseOffset;
    /// <summary>SmoothDamp 的速度缓存。</summary>
    private Vector3 velocity;
    /// <summary>当前轨道距离。</summary>
    private float distance;
    /// <summary>水平视角。</summary>
    private float yaw;
    /// <summary>俯仰视角。</summary>
    private float pitch;
    /// <summary>光标当前是否被锁定。</summary>
    private bool cursorLocked;

    void Awake()
    {
        if (offset == Vector3.zero) offset = new Vector3(0f, 5f, 4f);
        baseOffset = offset;
        distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
        // 未配置的情境距离按初始距离推导。
        if (explorationDistance <= 0f) explorationDistance = distance;
        if (stealthDistance <= 0f) stealthDistance = Mathf.Max(minDistance, distance * 0.9f);
        if (combatDistance <= 0f) combatDistance = Mathf.Min(maxDistance, distance * 1.05f);
    }

    void OnDisable()
    {
        UnlockCursor();
    }

    /// <summary>检测是否处于战斗（有卫兵追击）或潜行，并切换到对应距离。</summary>
    private void UpdateContextDistance()
    {
        if (!useContextDistance || HeroController == null) return;
        var stealth = HeroController.GetComponent<PlayerStealthState>();
        bool combat = false;
        foreach (var guard in FindObjectsOfType<GuardSenses>())
        {
            if (guard != null && guard.State == GuardState.Chase) { combat = true; break; }
        }
        string nextContext = combat ? "Combat" : stealth != null && stealth.IsSneaking ? "Stealth" : "Explore";
        if (nextContext == cameraContext) return;
        cameraContext = nextContext;
        float target = combat ? combatDistance : nextContext == "Stealth" ? stealthDistance : explorationDistance;
        distance = Mathf.Clamp(target, minDistance, maxDistance);
    }

    /// <summary>鼠标是否悬停在 UI 上。</summary>
    private bool PointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>按滚轮输入缩放，并夹紧到允许范围。</summary>
    private void ApplyZoom(float scroll)
    {
        distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
    }

    /// <summary>应用鼠标位移（由 Update 或测试调用）：X 转 yaw，Y 转 pitch。</summary>
    public void ApplyLookDelta(Vector2 delta)
    {
        yaw += delta.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch + delta.y * mouseSensitivity, minPitch, maxPitch);
    }

    /// <summary>判断命中的碰撞体是否属于角色（主角/敌人），角色不参与遮挡收近。</summary>
    private bool IsActor(Transform value)
    {
        return value.GetComponentInParent<HeroController>() != null
            || value.GetComponentInParent<EnemyController>() != null;
    }

    /// <summary>锁定并隐藏光标。</summary>
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    /// <summary>解锁并显示光标。</summary>
    private void UnlockCursor()
    {
        if (!cursorLocked) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }

    /// <summary>光标是否处于锁定状态。</summary>
    public bool CursorLocked { get { return cursorLocked; } }
    /// <summary>当前水平视角。</summary>
    public float Yaw { get { return yaw; } }
    /// <summary>当前俯仰视角。</summary>
    public float Pitch { get { return pitch; } }

    void Update()
    {
        // 剧情/暂停/对话会把 timeScale 置 0，此时解锁光标让玩家能点 UI；恢复游戏后重新锁定。
        bool shouldLock = lockCursor && Application.isFocused && Time.timeScale > 0f && HeroController != null;
        if (shouldLock && !cursorLocked) LockCursor();
        else if (!shouldLock && cursorLocked) UnlockCursor();

        if (cursorLocked)
        {
            // 锁定状态下鼠标位移直接驱动视角，滚轮缩放。
            ApplyLookDelta(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
            ApplyZoom(Input.mouseScrollDelta.y);
        }
        else if (!PointerOverUI())
        {
            // 解锁状态仍允许在非 UI 区域滚轮缩放。
            ApplyZoom(Input.mouseScrollDelta.y);
        }
    }

    void LateUpdate()
    {
        if (HeroController != null)
        {
            UpdateContextDistance();
            // 以角色胸口上方为焦点，按 yaw/pitch 计算期望相机位置。
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = HeroController.position + Vector3.up * 1.2f;
            Vector3 desired = focus + orbit * (baseOffset.normalized * distance);

            // 遮挡检测：焦点到相机之间若被非角色碰撞体挡住，把相机拉到障碍物前。
            Vector3 ray = desired - focus;
            RaycastHit hit;
            if (ray.sqrMagnitude > 0.001f
                && Physics.Raycast(focus, ray.normalized, out hit, ray.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && !IsActor(hit.transform))
            {
                desired = hit.point + hit.normal * obstructionPadding;
            }

            // 平滑跟随并始终看向焦点。
            transform.position = smoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime)
                : desired;
            Vector3 lookDir = focus - transform.position;
            if (lookDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(lookDir.normalized, Vector3.up), 1f - Mathf.Exp(-15f * Time.deltaTime));
        }
    }
}
