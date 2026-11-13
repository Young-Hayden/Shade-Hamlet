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

    private EnemyController controller;

    void Awake() { controller = GetComponent<EnemyController>(); }

    public void DoAttack()
    {
        if (controller.isDeath) return;
        // GuardSenses 是感知的唯一目标入口，丢失视线后清除过期目标。
        if (controller.senses != null) attackPlayer = controller.attackPlayer;
        else if (attackPlayer == null) attackPlayer = controller.attackPlayer;
        if (attackPlayer == null) return;

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
