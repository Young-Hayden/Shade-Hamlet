using UnityEngine;

/// <summary>
/// 箭矢投射物：初始化后沿直线飞行，命中主角造成伤害，命中其他物体或超时后销毁。
/// 飞行采用逐帧射线检测，避免高速箭矢穿过薄碰撞体。
/// </summary>
public class ArrowProjectile : MonoBehaviour
{
    /// <summary>飞行速度（米/秒）。</summary>
    public float speed = 12f;
    /// <summary>最大存活时间（秒）。</summary>
    public float lifetime = 4f;

    /// <summary>发射者，用于忽略自身碰撞体。</summary>
    private Transform attacker;
    /// <summary>命中伤害。</summary>
    private float damage;
    /// <summary>飞行方向（世界空间单位向量）。</summary>
    private Vector3 direction;
    /// <summary>已存活时间。</summary>
    private float age;

    /// <summary>初始化箭矢：朝向目标胸口、记录伤害与发射者，并启动寿命回收。</summary>
    public void Initialize(Transform target, Transform source, float amount)
    {
        attacker = source;
        damage = amount;
        Vector3 aim = target.position + Vector3.up - transform.position;
        direction = aim.sqrMagnitude > 0.001f ? aim.normalized : transform.forward;
        if (direction.sqrMagnitude > 0.001f) transform.forward = direction;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime) return;
        float step = speed * Time.deltaTime;
        // 先用射线检查本帧位移路径上是否命中物体。
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, step, ~0, QueryTriggerInteraction.Ignore))
        {
            // 忽略发射者自身（弓/手）的碰撞体，继续飞行。
            if (attacker != null && hit.transform.IsChildOf(attacker))
            {
                transform.position += direction * step;
                return;
            }
            var hero = hit.transform.GetComponentInParent<HeroController>();
            if (hero != null && !hero.IsDead) hero.Hurt(damage, attacker);
            Destroy(gameObject);
            return;
        }
        transform.position += direction * step;
    }
}
