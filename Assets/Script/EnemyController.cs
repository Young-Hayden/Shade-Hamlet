using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    // 定义最大血量值
    [SerializeField] private float HP_Max = 100f;
    // 定义当前血量值
    private float HP_Now;

    // 定义一个animator的变量
    public Animator animator;

    // 定义敌人的攻击力
    [SerializeField] private float ATK = 5f;

    [Header("战斗参数")]
    // 进入攻击的距离，与攻击盒实际命中范围匹配。
    [SerializeField] private float attackRange = 1.8f;
    // 退出攻击的距离（滞回），避免目标稍微移动就反复收刀/出刀。
    [SerializeField] private float attackExitRange = 2.4f;
    // 两次攻击之间的间隔，与攻击动画时长对应。
    [SerializeField] private float attackInterval = 1f;

    // 定义一个死亡变量，用来判断是否死亡
    public bool isDeath = false;
    // 定义一个获取agent组件的变量
    public NavMeshAgent agent;
    // 定义目标位置的坐标变量
    public Transform target;
    // 定义一个目标列表
    public Transform[] targetList;

    // 判断是否被攻击
    public bool beAttack = false;
    // 获取攻击者的变量
    public Transform attackPlayer;

    // 定义一个攻击状态
    public bool isAttack = false;

    // 创建一个玩家脚本的游戏物体。
    public HeroController heroScript;

    // 定义血条的变量
    public HPScript hpCanvas;
    // 感知组件：负责巡逻、发现、追击和搜索，战斗仍由本类处理。
    public GuardSenses senses;
    // 可选远程攻击组件；为空时保持近战行为。
    public RangedAttack rangedAttack;

    public float CurrentHP { get { return HP_Now; } }
    public float MaxHP { get { return HP_Max; } }

    void Start()
    {
        // 设置敌人的默认血量
        HP_Now = HP_Max;
        if (hpCanvas != null) hpCanvas.SetHP(HP_Now, HP_Max);

        // 获取自己身上的动画控制器
        if (animator == null) animator = GetComponent<Animator>();
        // 获取自己上的NavMeshAgent组件
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (senses == null) senses = GetComponent<GuardSenses>();
        if (rangedAttack == null) rangedAttack = GetComponent<RangedAttack>();

        // 关键修复：位移完全交给NavMeshAgent，关闭Root Motion。
        // 原来Root Motion开启，攻击动画会把敌人自己推离目标，导致砍一刀就脱战。
        if (animator != null) animator.applyRootMotion = false;

        // 位移完全交给NavMeshAgent，刚体只用于攻击盒触发检测，避免互相推挤与尸体下坠。
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 开局不锁定主角：只有被打之后才追击反击（场景里拖好的引用不再当作仇恨来源）。
        attackPlayer = null;
        beAttack = false;
        isAttack = false;

        // 首先将标位置设置为目标列表中的第一项
        if (targetList != null && targetList.Length > 0) target = targetList[0];
    }

    void Update()
    {
        if (isDeath) return;
        DoMove();
        DoAttack();
    }

    // 水平距离，避免地形高低差影响判定。
    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // 定义一个伤害、扣血的函数
    public void Hurt(float damage, Transform attacker)
    {
        // 已死亡就不再重复结算。
        if (isDeath) return;

        // 计算伤害并夹紧，血条不会出现负数。
        float oldHP = HP_Now;
        HP_Now = Mathf.Clamp(HP_Now - damage, 0f, HP_Max);
        float actualDamage = oldHP - HP_Now;
        if (hpCanvas != null) hpCanvas.SetHP(HP_Now, HP_Max);

        // 只有实际扣血才产生反馈；反馈服务缺失不影响伤害结算。
        if (actualDamage > 0f && CombatFeedback.Instance != null)
        {
            CombatFeedback.Instance.ShowDamage(transform.position + Vector3.up * 2.1f, actualDamage, false);
            CombatFeedback.Instance.PlayHit(transform.position);
        }

        // 如果攻击者是主角hero，记录攻击者并进入反击状态。
        if (attacker != null && attacker.CompareTag("Player"))
        {
            heroScript = attacker.GetComponent<HeroController>();
            attackPlayer = attacker;
            beAttack = true;
            if (senses != null) senses.NotifyDamaged(attacker);
        }

        if (HP_Now <= 0f) Die();
    }

    // 碰撞检测：被主角的攻击盒打中时，记录真正的攻击者。
    private void OnTriggerEnter(Collider other)
    {
        if (isDeath || !other.CompareTag("AttackBox")) return;

        beAttack = true;
        AttackTriggerBox box = other.GetComponent<AttackTriggerBox>();
        if (box != null && box.IsPlayerAttack && box.attackPlayer != null)
        {
            attackPlayer = box.attackPlayer;
            heroScript = box.attackPlayer.GetComponent<HeroController>();
        }
    }

    public void DoMove()
    {
        if (isDeath) return;

        if (senses != null && senses.State == GuardState.Suspicious)
        {
            // 可疑但未确认时停止巡逻并观察，不读取玩家后续位置。
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            if (animator != null) animator.SetBool("walk", false);
            return;
        }

        if (senses != null && (senses.State == GuardState.Chase || senses.State == GuardState.Search))
        {
            // Chase 与 Search 都只使用可见位置或最后目击位置。
            target = senses.Destination;
            agent.stoppingDistance = senses.State == GuardState.Chase
                ? (rangedAttack != null ? rangedAttack.chaseStoppingDistance : Mathf.Max(0f, attackRange - 0.2f)) : 0f;
        }
        else if (beAttack && attackPlayer != null)
        {
            // 被攻击后追击攻击者。
            target = attackPlayer;
            agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.2f);
        }
        else
        {
            // 巡逻状态：到达巡逻点附近就随机换下一个点。
            agent.stoppingDistance = 0f;
            if (target == null && targetList != null && targetList.Length > 0)
                target = targetList[Random.Range(0, targetList.Length)];

            if (target != null && FlatDistance(transform.position, target.position) <= 1.5f
                && targetList != null && targetList.Length > 0)
            {
                target = targetList[Random.Range(0, targetList.Length)];
            }
        }

        // 攻击中不刷新目的地，保持站定出刀；其余时间持续向目标移动（追击时每帧更新，追得上移动目标）。
        if (target != null && !isAttack && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }

        // 设置走路动画
        if (animator != null) animator.SetBool("walk", !isAttack && agent.velocity.magnitude > 0.05f);
    }

    // 定义一个攻击函数，在这里执行攻击效果
    public void DoAttack()
    {
        if (isDeath || attackPlayer == null || HP_Now <= 0f) return;
        if (senses != null && senses.State != GuardState.Chase) return;
        if (rangedAttack != null && rangedAttack.enabled)
        {
            rangedAttack.DoAttack();
            return;
        }

        Vector3 dir = attackPlayer.position - transform.position;
        dir.y = 0f;
        float dist = dir.magnitude;

        if (dist <= (isAttack ? attackExitRange : attackRange))
        {
            // 面向主角
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir.normalized), 12f * Time.deltaTime);
            }

            if (!isAttack)
            {
                // 攻击开始，自己不再移动。
                isAttack = true;
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.isStopped = true;
                }
                animator.ResetTrigger("attackEnd");
                animator.SetTrigger("attack");
                Invoke("AttackEnd", attackInterval);
            }
            // 攻击期间保持站定，不再每帧isStopped=false / attackEnd（原来会导致边打边滑、动画抖动）。
        }
        else
        {
            // 目标离开攻击距离：收刀并恢复追击。
            if (isAttack)
            {
                isAttack = false;
                CancelInvoke("AttackEnd");
                animator.SetTrigger("attackEnd");
            }
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
        }
    }

    // 定义一个攻击结束的函数
    public void AttackEnd()
    {
        isAttack = false;
    }

    // 死亡只执行一次，尸体不再移动、不再碰撞。
    private void Die()
    {
        isDeath = true;
        isAttack = false;
        beAttack = false;
        attackPlayer = null;
        CancelInvoke("AttackEnd");

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
            agent.enabled = false;
        }
        foreach (Collider c in GetComponents<Collider>()) c.enabled = false;

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetBool("walk", false);
            animator.ResetTrigger("attack");
            animator.SetBool("death", true);
        }

        // 添加死亡音效
        if (GameMusic.Instance != null) GameMusic.Instance.PlayMusic(1);
        if (CombatFeedback.Instance != null) CombatFeedback.Instance.PlayKill(transform.position);
        LootDropper dropper = GetComponent<LootDropper>();
        if (dropper != null) dropper.Drop();
        if (GameFlow.Instance != null) GameFlow.Instance.RegisterEnemyKill(GetInstanceID());

        // 2秒后销毁尸体
        Destroy(gameObject, 2f);
    }

    // 定义一个返回攻击力的函数
    public float showATK()
    {
        return ATK;
    }

    // 定义一个返回敌人血量的函数
    public float showHP()
    {
        return HP_Now;
    }
}
