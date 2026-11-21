using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 远程卫兵行为：在射程内停下、转向并定时出箭；玩家贴身后沿 NavMesh 后撤到安全距离。
/// 与 EnemyController 共用 isAttack 状态，后撤期间由本组件独占移动目的地。
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class RangedAttack : MonoBehaviour
{
    /// <summary>箭矢预制体。</summary>
    public GameObject projectilePrefab;
    /// <summary>发射点（一般绑在弓/手上）。</summary>
    public Transform launchPoint;
    /// <summary>当前攻击目标（由感知系统同步）。</summary>
    public Transform attackPlayer;
    /// <summary>开始射击的距离。</summary>
    public float range = 8f;
    /// <summary>退出射击的距离（滞回）。</summary>
    public float exitRange = 10f;
    /// <summary>两次射击的间隔。</summary>
    public float attackInterval = 2f;
    /// <summary>追击时保持的停步距离。</summary>
    public float chaseStoppingDistance = 6.5f;
    /// <summary>箭矢伤害。</summary>
    public float damage = 5f;

    [Header("近身后撤")]
    /// <summary>玩家进入该距离时开始后撤。</summary>
    public float retreatDistance = 3f;
    /// <summary>后撤到该距离后恢复射击。</summary>
    public float retreatExitDistance = 4.5f;
    /// <summary>后撤落点的 NavMesh 采样半径。</summary>
    public float retreatSampleRadius = 4f;

    /// <summary>敌人控制器引用。</summary>
    private EnemyController controller;
    /// <summary>是否正在后撤。</summary>
    private bool retreating;

    /// <summary>是否正在后撤（EnemyController 据此让出移动控制权）。</summary>
    public bool IsRetreating { get { return retreating; } }

    void Awake() { controller = GetComponent<EnemyController>(); }

    void OnDisable()
    {
        // 组件关闭时恢复 NavMeshAgent 的自动转向。
        if (controller != null && controller.agent != null) controller.agent.updateRotation = true;
    }

    // 近身时退到可行走的位置；没有完整后撤路径则保持原行为，绝不穿墙。
    /// <summary>
    /// 尝试后撤：玩家进入 retreatDistance 时，在身后采样可行走点，
    /// 要求 NavMesh 路径完整且确实拉开距离，否则放弃后撤。
    /// </summary>
    public bool TryRetreat(Vector3 targetPosition)
    {
        if (controller == null || controller.isDeath) return false;
        Vector3 away = transform.position - targetPosition;
        away.y = 0f;
        float distance = away.magnitude;
        // 只有贴得过近才后撤；已经拉开到退出距离则停止后撤。
        if (!retreating && distance >= retreatDistance) return false;
        if (retreating && distance >= retreatExitDistance)
        {
            StopRetreat();
            return false;
        }
        if (distance < 0.001f) away = -transform.forward;
        else away /= distance;

        var agent = controller.agent;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            StopRetreat();
            return false;
        }

        // 在远离玩家的方向上找一个可行走点，并验证整条路径可达。
        float retreatLength = Mathf.Max(2f, retreatExitDistance - distance + 0.75f);
        Vector3 desired = transform.position + away * retreatLength;
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(desired, out hit, retreatSampleRadius, NavMesh.AllAreas))
        {
            StopRetreat();
            return false;
        }
        Vector3 flatDestination = hit.position;
        flatDestination.y = 0f;
        Vector3 flatTarget = targetPosition;
        flatTarget.y = 0f;
        // SamplePosition 可能只返回当前位置；这不构成后撤，必须退出并恢复射击。
        if (Vector3.Distance(flatDestination, flatTarget) <= distance + 0.25f)
        {
            StopRetreat();
            return false;
        }
        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path)
            || path.status != NavMeshPathStatus.PathComplete)
        {
            StopRetreat();
            return false;
        }

        retreating = true;
        // 后撤时收刀，避免边退边射。
        if (controller.isAttack)
        {
            controller.isAttack = false;
            CancelInvoke(nameof(AttackEnd));
            controller.animator.SetTrigger("attackEnd");
        }
        // 后撤期间自己控制朝向，面向玩家倒退。
        agent.updateRotation = false;
        agent.stoppingDistance = 0f;
        agent.isStopped = false;
        if (!agent.SetDestination(hit.position))
        {
            StopRetreat();
            return false;
        }
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(toTarget.normalized), 10f * Time.deltaTime);
        if (controller.animator != null) controller.animator.SetBool("walk", agent.velocity.magnitude > 0.05f);
        return true;
    }

    /// <summary>结束后撤并恢复追击停步距离与自动转向。</summary>
    private void StopRetreat()
    {
        retreating = false;
        if (controller == null || controller.agent == null) return;
        controller.agent.updateRotation = true;
        controller.agent.stoppingDistance = chaseStoppingDistance;
    }

    /// <summary>远程攻击决策：同步目标、尝试后撤；距离合适则停下并触发攻击动画。</summary>
    public void DoAttack()
    {
        if (controller.isDeath) return;
        // GuardSenses 是感知的唯一目标入口，丢失视线后清除过期目标。
        if (controller.senses != null) attackPlayer = controller.attackPlayer;
        else if (attackPlayer == null) attackPlayer = controller.attackPlayer;
        if (attackPlayer == null) return;
        if (TryRetreat(attackPlayer.position)) return;

        Vector3 direction = attackPlayer.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;
        if (distance <= (controller.isAttack ? exitRange : range))
        {
            // 进入射程：转身、站定、按间隔触发攻击动画（动画事件里 Fire）。
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction.normalized), 10f * Time.deltaTime);

            if (!controller.isAttack)
            {
                controller.isAttack = true;
                controller.animator.SetBool("walk", false);
                if (controller.agent.enabled && controller.agent.isOnNavMesh)
                {
                    controller.agent.ResetPath();
                    controller.agent.isStopped = true;
                }
                controller.animator.ResetTrigger("attackEnd");
                controller.animator.SetTrigger("attack");
                CancelInvoke(nameof(AttackEnd));
                Invoke(nameof(AttackEnd), attackInterval);
            }
        }
        else
        {
            // 超出射程：收刀并继续追击。
            if (controller.isAttack)
            {
                controller.isAttack = false;
                CancelInvoke(nameof(AttackEnd));
                controller.animator.SetTrigger("attackEnd");
            }
            if (controller.agent.enabled && controller.agent.isOnNavMesh) controller.agent.isStopped = false;
        }
    }

    /// <summary>攻击间隔结束，解除锁定并恢复移动。</summary>
    public void AttackEnd()
    {
        controller.isAttack = false;
        if (controller.agent.enabled && controller.agent.isOnNavMesh) controller.agent.isStopped = false;
    }

    /// <summary>由攻击动画事件调用：生成箭矢并初始化目标与伤害。</summary>
    public void Fire()
    {
        if (controller.isDeath || projectilePrefab == null || attackPlayer == null) return;
        Vector3 position = launchPoint != null
            ? launchPoint.position
            : transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
        Quaternion rotation = launchPoint != null ? launchPoint.rotation : transform.rotation;
        var arrow = Instantiate(projectilePrefab, position, rotation).GetComponent<ArrowProjectile>();
        if (arrow != null) arrow.Initialize(attackPlayer, transform, damage);
    }
}
