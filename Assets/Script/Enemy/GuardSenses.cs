using UnityEngine;

/// <summary>卫兵感知状态机。</summary>
public enum GuardState
{
    /// <summary>巡逻：在巡逻点之间移动。</summary>
    Patrol,
    /// <summary>可疑：听到噪音或看到但未确认，站定观察朝向。</summary>
    Suspicious,
    /// <summary>追击：确认看到玩家，向玩家实时位置移动。</summary>
    Chase,
    /// <summary>搜索：丢失视线后前往最后目击点，超时回到巡逻。</summary>
    Search
}

/// <summary>
/// 卫兵视觉/听觉感知：
/// - 视觉：视锥 + 距离 + 射线遮挡（含烟雾遮挡），按距离与潜行系数累积警觉度；
/// - 听觉：订阅 <see cref="NoiseEmitter.OnNoise"/>，只产生可疑线索，不直接授予实时追击目标。
/// 输出 <see cref="Destination"/> 供 EnemyController 追击/搜索使用。
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class GuardSenses : MonoBehaviour
{
    /// <summary>玩家控制器；未指定时自动查找场景内的主角。</summary>
    public HeroController player;
    /// <summary>最大视距（米）。</summary>
    public float visionDistance = 9f;
    /// <summary>视锥总角度（度）。</summary>
    public float visionAngle = 110f;
    /// <summary>在理想距离下从 0 到完全警觉所需时间（秒）。</summary>
    public float timeToDetect = 0.8f;
    /// <summary>丢失视线后进入搜索的延迟（秒）。</summary>
    public float lostSightDelay = 2f;
    /// <summary>搜索持续时间（秒）。</summary>
    public float searchDuration = 4f;
    /// <summary>卫兵眼睛高度。</summary>
    public float eyeHeight = 1.5f;
    /// <summary>玩家胸口采样高度。</summary>
    public float playerChestHeight = 1f;

    /// <summary>当前感知状态。</summary>
    public GuardState State { get; private set; }
    /// <summary>警觉度 0～1；到达 1 时进入追击。</summary>
    public float Alertness { get; private set; }
    /// <summary>最后一次确认看到玩家的位置。</summary>
    public Vector3 LastSeenPosition { get; private set; }
    /// <summary>最后一次听到噪音的位置。</summary>
    public Vector3 LastHeardPosition { get; private set; }
    /// <summary>最后一次听到噪音的时间。</summary>
    public float LastNoiseTime { get; private set; }
    /// <summary>供 EnemyController 读取的移动目的地（隐藏对象，不参与渲染）。</summary>
    public Transform Destination { get; private set; }

    /// <summary>敌人控制器引用。</summary>
    private EnemyController controller;
    /// <summary>丢失视线计时。</summary>
    private float lostTimer;
    /// <summary>搜索计时。</summary>
    private float searchTimer;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        if (player == null) player = FindObjectOfType<HeroController>();
        // 目的地用隐藏对象承载，Scene 视图不会出现多余图标。
        var destination = new GameObject(gameObject.name + "_SenseDestination");
        destination.hideFlags = HideFlags.HideInHierarchy;
        Destination = destination.transform;
        ResetPerception();
    }

    void OnEnable()
    {
        NoiseEmitter.OnNoise += HandleNoise;
    }

    void OnDisable()
    {
        NoiseEmitter.OnNoise -= HandleNoise;
    }

    void OnDestroy()
    {
        if (Destination != null) Destroy(Destination.gameObject);
    }

    /// <summary>把感知与敌人战斗状态一起复位到巡逻。</summary>
    public void ResetPerception()
    {
        State = GuardState.Patrol;
        Alertness = 0f;
        lostTimer = 0f;
        searchTimer = 0f;
        LastSeenPosition = transform.position;
        LastHeardPosition = transform.position;
        LastNoiseTime = 0f;
        if (Destination != null) Destination.position = LastSeenPosition;
        if (controller != null)
        {
            controller.attackPlayer = null;
            controller.beAttack = false;
            if (controller.targetList != null && controller.targetList.Length > 0)
                controller.target = controller.targetList[0];
        }
    }

    /// <summary>
    /// 判断当前是否能看见玩家：
    /// 距离、视锥角度、烟雾遮挡、物理射线遮挡（玩家自身的碰撞体不算遮挡）。
    /// </summary>
    public bool EvaluateVisibility()
    {
        if (player == null || player.IsDead) return false;
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return true;
        if (direction.magnitude > visionDistance) return false;
        if (Vector3.Angle(transform.forward, direction.normalized) > visionAngle * 0.5f) return false;

        // 从眼睛到玩家胸口做遮挡检测；烟雾先做一次逻辑遮挡。
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 chest = player.transform.position + Vector3.up * playerChestHeight;
        Vector3 ray = chest - eye;
        if (SmokeZone.BlocksSight(eye, chest)) return false;
        float distance = ray.magnitude;
        if (distance < 0.001f) return true;
        if (!Physics.Raycast(eye, ray.normalized, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
            return true;
        return hit.transform.GetComponentInParent<HeroController>() == player;
    }

    /// <summary>噪音回调：范围内产生一次可疑线索并朝声源转向；追击/搜索中不降级。</summary>
    private void HandleNoise(NoiseEvent noise)
    {
        if (noise == null || controller == null || controller.isDeath || noise.Radius <= 0f) return;
        if ((transform.position - noise.Position).sqrMagnitude > noise.Radius * noise.Radius) return;

        LastHeardPosition = noise.Position;
        LastNoiseTime = noise.Time;
        if (Destination != null) Destination.position = noise.Position;

        // 声音只提供一次性位置线索，不把玩家的实时 Transform 写入追击目标。
        if (State == GuardState.Chase || State == GuardState.Search) return;
        State = GuardState.Suspicious;
        Alertness = Mathf.Max(Alertness, Mathf.Clamp01(noise.Strength) * 0.5f);
        controller.attackPlayer = null;
        controller.beAttack = false;
        FacePosition(noise.Position);
    }

    /// <summary>被主角攻击后立即进入追击，并记录攻击者位置。</summary>
    public void NotifyDamaged(Transform attacker)
    {
        if (attacker == null) return;
        var attackingHero = attacker.GetComponentInParent<HeroController>();
        if (attackingHero == null) return;
        if (player == null) player = attackingHero;
        if (player != attackingHero) return;
        LastSeenPosition = player.transform.position;
        Destination.position = LastSeenPosition;
        EnterChase();
    }

    /// <summary>进入追击：警觉度拉满，把玩家实时位置交给敌人战斗逻辑。</summary>
    private void EnterChase()
    {
        State = GuardState.Chase;
        Alertness = 1f;
        lostTimer = 0f;
        searchTimer = 0f;
        if (player != null)
        {
            LastSeenPosition = player.transform.position;
            Destination.position = LastSeenPosition;
            controller.attackPlayer = player.transform;
            controller.beAttack = true;
        }
    }

    /// <summary>进入搜索：放弃实时目标，前往最后目击点。</summary>
    private void EnterSearch()
    {
        State = GuardState.Search;
        searchTimer = 0f;
        controller.attackPlayer = null;
        controller.beAttack = false;
        Destination.position = LastSeenPosition;
    }

    void Update()
    {
        // 敌人已死或组件丢失时停止感知。
        if (controller == null || controller.isDeath)
        {
            enabled = false;
            return;
        }
        if (player == null) player = FindObjectOfType<HeroController>();
        if (player == null || player.IsDead)
        {
            if (State != GuardState.Patrol) ResetPerception();
            return;
        }

        bool visible = EvaluateVisibility();
        if (visible)
        {
            // 只有真正看见时才持续更新最后目击点。
            LastSeenPosition = player.transform.position;
            Destination.position = LastSeenPosition;
        }

        switch (State)
        {
            case GuardState.Patrol:
            case GuardState.Suspicious:
                if (visible)
                {
                    lostTimer = 0f;
                    // 越近警觉越快；玩家潜行/藏草会降低累积速度。
                    float distance = Vector3.Distance(transform.position, player.transform.position);
                    float distanceFactor = Mathf.Lerp(1.25f, 0.75f, Mathf.Clamp01(distance / Mathf.Max(0.01f, visionDistance)));
                    var stealth = player.GetComponent<PlayerStealthState>();
                    float detectionMultiplier = stealth != null ? stealth.DetectionMultiplier : 1f;
                    Alertness = Mathf.Min(1f, Alertness + Time.deltaTime * distanceFactor * detectionMultiplier / Mathf.Max(0.01f, timeToDetect));
                    if (Alertness >= 1f) EnterChase();
                    else
                    {
                        // 还没确认：转入可疑并面向玩家。
                        State = GuardState.Suspicious;
                        FacePlayer();
                    }
                }
                else
                {
                    // 看不见时警觉度缓慢衰减，衰减到 0 回到巡逻。
                    Alertness = Mathf.Max(0f, Alertness - Time.deltaTime / Mathf.Max(0.01f, timeToDetect * 2f));
                    if (State == GuardState.Suspicious && Alertness <= 0f) ResetPerception();
                }
                break;

            case GuardState.Chase:
                if (visible)
                {
                    lostTimer = 0f;
                    controller.attackPlayer = player.transform;
                }
                else
                {
                    // 追丢一段时间后转为搜索最后目击点。
                    lostTimer += Time.deltaTime;
                    if (lostTimer >= lostSightDelay) EnterSearch();
                }
                break;

            case GuardState.Search:
                if (visible) EnterChase();
                else
                {
                    // 搜索超时后放弃并回到巡逻。
                    searchTimer += Time.deltaTime;
                    if (searchTimer >= searchDuration) ResetPerception();
                }
                break;
        }
    }

    /// <summary>平滑转向指定世界坐标（只影响水平朝向）。</summary>
    private void FacePosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(direction.normalized), 6f * Time.deltaTime);
    }

    /// <summary>转向玩家当前位置。</summary>
    private void FacePlayer()
    {
        if (player != null) FacePosition(player.transform.position);
    }
}
