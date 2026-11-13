using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(HeroController))]
public class AssassinationController : MonoBehaviour
{
    public float range = 1.4f;
    public float behindDot = -0.5f;
    public float facingDot = 0.2f;

    private HeroController hero;
    private EnemyController pendingTarget;

    void Awake() { hero = GetComponent<HeroController>(); }

    void Update()
    {
        if (hero == null) return;
        if (StoryDirector.Instance != null && StoryDirector.Instance.IsUiOpen) return;
        if (!hero.isAttack) pendingTarget = null;
        if (Input.GetKeyDown(KeyCode.E)
            && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            TryAssassinate();
        }
    }

    public bool CanAssassinate(out EnemyController target)
    {
        target = null;
        if (hero == null || hero.IsDead || hero.isAttack) return false;

        float nearest = float.MaxValue;
        foreach (var enemy in FindObjectsOfType<EnemyController>())
        {
            if (!IsEligible(enemy, true)) continue;
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < nearest)
            {
                nearest = distance;
                target = enemy;
            }
        }
        return target != null;
    }

    public bool TryAssassinate()
    {
        if (!CanAssassinate(out EnemyController target)) return false;
        pendingTarget = target;
        hero.tarEnemy = target.gameObject;
        hero.enemyScript = target;

        Vector3 facing = target.transform.position - transform.position;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(facing.normalized);

        hero.isAttack = true;
        hero.anim.ResetTrigger("attackEnd");
        hero.anim.SetTrigger("attack");
        hero.Invoke("AttackEnd", 1f);
        return true;
    }

    // 动画事件时再次校验，防止目标已离开或已警觉时仍然延迟必杀。
    public bool TryConsumeTarget(out EnemyController target)
    {
        target = pendingTarget;
        pendingTarget = null;
        return target != null && IsEligible(target, false);
    }

    private bool IsEligible(EnemyController enemy, bool requireReady)
    {
        if (enemy == null || enemy.isDeath) return false;
        if (requireReady && hero.isAttack) return false;

        Vector3 toHero = transform.position - enemy.transform.position;
        toHero.y = 0f;
        float distance = toHero.magnitude;
        if (distance > range || distance < 0.001f) return false;
        if (Vector3.Dot(enemy.transform.forward, toHero.normalized) > behindDot) return false;

        Vector3 toEnemy = -toHero;
        if (Vector3.Dot(transform.forward, toEnemy.normalized) < facingDot) return false;

        var senses = enemy.GetComponent<GuardSenses>();
        if (senses == null || senses.State != GuardState.Patrol) return false;
        var storyTarget = enemy.GetComponent<StoryTarget>();
        return storyTarget == null || storyTarget.allowKill;
    }
}
