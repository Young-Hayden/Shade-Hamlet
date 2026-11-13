using UnityEngine;
using UnityEngine.EventSystems;

public class HeroCamera : MonoBehaviour
{
    public Transform HeroController;
    public Vector3 offset = new Vector3(0f, 5f, 4f);
    public float smoothTime = 0.12f;

    [Header("缩放与旋转")]
    public float minDistance = 4f;
    public float maxDistance = 12f;
    public float zoomSpeed = 1.5f;
    public float obstructionPadding = 0.25f;

    [Header("鼠标视角")]
    public bool lockCursor = true;
    public float mouseSensitivity = 2.5f;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    [Header("情境距离")]
    public bool useContextDistance = true;
    public float explorationDistance;
    public float stealthDistance;
    public float combatDistance;
    private string cameraContext;

    private Vector3 baseOffset;
    private Vector3 velocity;
    private float distance;
    private float yaw;
    private float pitch;
    private bool cursorLocked;

    void Awake()
    {
        if (offset == Vector3.zero) offset = new Vector3(0f, 5f, 4f);
        baseOffset = offset;
        distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
        if (explorationDistance <= 0f) explorationDistance = distance;
        if (stealthDistance <= 0f) stealthDistance = Mathf.Max(minDistance, distance * 0.9f);
        if (combatDistance <= 0f) combatDistance = Mathf.Min(maxDistance, distance * 1.05f);
    }

    void OnDisable()
    {
        UnlockCursor();
    }

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

    private bool PointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void ApplyZoom(float scroll)
    {
        distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
    }

    public void ApplyLookDelta(Vector2 delta)
    {
        yaw += delta.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, minPitch, maxPitch);
    }

    private bool IsActor(Transform value)
    {
        return value.GetComponentInParent<HeroController>() != null
            || value.GetComponentInParent<EnemyController>() != null;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    private void UnlockCursor()
    {
        if (!cursorLocked) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }

    public bool CursorLocked { get { return cursorLocked; } }
    public float Yaw { get { return yaw; } }
    public float Pitch { get { return pitch; } }

    void Update()
    {
        // 剧情/暂停/对话会把 timeScale 置 0，此时解锁光标让玩家能点 UI；恢复游戏后重新锁定。
        bool shouldLock = lockCursor && Application.isFocused && Time.timeScale > 0f && HeroController != null;
        if (shouldLock && !cursorLocked) LockCursor();
        else if (!shouldLock && cursorLocked) UnlockCursor();

        if (cursorLocked)
        {
            ApplyLookDelta(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
            ApplyZoom(Input.mouseScrollDelta.y);
        }
        else if (!PointerOverUI())
        {
            ApplyZoom(Input.mouseScrollDelta.y);
        }
    }

    void LateUpdate()
    {
        if (HeroController != null)
        {
            UpdateContextDistance();
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = HeroController.position + Vector3.up * 1.2f;
            Vector3 desired = focus + orbit * (baseOffset.normalized * distance);
            Vector3 ray = desired - focus;
            RaycastHit hit;
            if (ray.sqrMagnitude > 0.001f
                && Physics.Raycast(focus, ray.normalized, out hit, ray.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && !IsActor(hit.transform))
            {
                desired = hit.point + hit.normal * obstructionPadding;
            }

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
