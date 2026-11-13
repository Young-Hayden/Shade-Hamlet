using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 4f;

    private Transform attacker;
    private float damage;
    private Vector3 direction;
    private float age;

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
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, step, ~0, QueryTriggerInteraction.Ignore))
        {
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
