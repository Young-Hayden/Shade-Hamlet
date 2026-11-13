using UnityEngine;

public enum GuardState
{
    Patrol,
    Suspicious,
    Chase,
    Search
}

[RequireComponent(typeof(EnemyController))]
public class GuardSenses : MonoBehaviour
{
    public HeroController player;
    public float visionDistance = 9f;
    public float visionAngle = 110f;
    public float timeToDetect = 0.8f;
    public float lostSightDelay = 2f;
    public float searchDuration = 4f;
    public float eyeHeight = 1.5f;
    public float playerChestHeight = 1f;

    public GuardState State { get; private set; }
    public float Alertness { get; private set; }
    public Vector3 LastSeenPosition { get; private set; }
    public Vector3 LastHeardPosition { get; private set; }
    public float LastNoiseTime { get; private set; }
    public Transform Destination { get; private set; }

    private EnemyController controller;
    private float lostTimer;
    private float searchTimer;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        if (player == null) player = FindObjectOfType<HeroController>();
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

    public bool EvaluateVisibility()
    {
        if (player == null || player.IsDead) return false;
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return true;
        if (direction.magnitude > visionDistance) return false;
        if (Vector3.Angle(transform.forward, direction.normalized) > visionAngle * 0.5f) return false;

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
                    float distance = Vector3.Distance(transform.position, player.transform.position);
                    float distanceFactor = Mathf.Lerp(1.25f, 0.75f, Mathf.Clamp01(distance / Mathf.Max(0.01f, visionDistance)));
                    var stealth = player.GetComponent<PlayerStealthState>();
                    float detectionMultiplier = stealth != null ? stealth.DetectionMultiplier : 1f;
                    Alertness = Mathf.Min(1f, Alertness + Time.deltaTime * distanceFactor * detectionMultiplier / Mathf.Max(0.01f, timeToDetect));
                    if (Alertness >= 1f) EnterChase();
                    else
                    {
                        State = GuardState.Suspicious;
                        FacePlayer();
                    }
                }
                else
                {
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
                    lostTimer += Time.deltaTime;
                    if (lostTimer >= lostSightDelay) EnterSearch();
                }
                break;

            case GuardState.Search:
                if (visible) EnterChase();
                else
                {
                    searchTimer += Time.deltaTime;
                    if (searchTimer >= searchDuration) ResetPerception();
                }
                break;
        }
    }

    private void FacePosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(direction.normalized), 6f * Time.deltaTime);
    }

    private void FacePlayer()
    {
        if (player != null) FacePosition(player.transform.position);
    }
}
