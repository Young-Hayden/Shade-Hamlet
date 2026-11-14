using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyController))]
public class RangedAttack : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform launchPoint;
    public Transform attackPlayer;
    public float range = 8f;
    public float exitRange = 10f;
    public float attackInterval = 2f;
    public float chaseStoppingDistance = 6.5f;
    public float damage = 5f;

    [Header("近身后撤")]
    public float retreatDistance = 3f;
    public float retreatExitDistance = 4.5f;
    public float retreatSampleRadius = 4f;

    private EnemyController controller;
    private bool retreating;

    public bool IsRetreating { get { return retreating; } }

    void Awake() { controller = GetComponent<EnemyController>(); }

    void OnDisable()
    {
        if (controller != null && controller.agent != null) controller.agent.updateRotation = true;
    }

    // 近身时退到可行走的位置；没有完整后撤路径则保持原行为，绝不穿墙。
    public bool TryRetreat(Vector3 targetPosition)
    {
        if (controller == null || controller.isDeath) return false;
        Vector3 away = transform.position - targetPosition;
        away.y = 0f;
        float distance = away.magnitude;
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
        if (controller.isAttack)
        {
            controller.isAttack = false;
            CancelInvoke(nameof(AttackEnd));
            controller.animator.SetTrigger("attackEnd");
        }
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

    private void StopRetreat()
    {
        retreating = false;
        if (controller == null || controller.agent == null) return;
        controller.agent.updateRotation = true;
        controller.agent.stoppingDistance = chaseStoppingDistance;
    }

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
            if (controller.isAttack)
            {
                controller.isAttack = false;
                CancelInvoke(nameof(AttackEnd));
                controller.animator.SetTrigger("attackEnd");
            }
            if (controller.agent.enabled && controller.agent.isOnNavMesh) controller.agent.isStopped = false;
        }
    }

    public void AttackEnd()
    {
        controller.isAttack = false;
        if (controller.agent.enabled && controller.agent.isOnNavMesh) controller.agent.isStopped = false;
    }

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
