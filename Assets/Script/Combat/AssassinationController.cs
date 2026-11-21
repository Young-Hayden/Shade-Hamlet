using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 刺杀控制器（挂在主角上）。
/// E 键尝试刺杀：要求目标在近身距离、主角位于目标背后、目标背对主角、目标处于 Patrol 状态，
/// 且剧情目标允许被击杀。真正伤害在攻击动画事件中由 TryConsumeTarget 二次校验后结算。
/// </summary>
[RequireComponent(typeof(HeroController))]
public class AssassinationController : MonoBehaviour
{
    /// <summary>可刺杀的最大距离（米）。</summary>
    public float range = 1.4f;
    /// <summary>目标朝向与「目标→主角」方向的点积阈值；小于该值才算背对主角。</summary>
    public float behindDot = -0.5f;
    /// <summary>主角朝向与「主角→目标」方向的点积阈值；大于该值才算正面接近。</summary>
    public float facingDot = 0.2f;

    /// <summary>主角控制器。</summary>
    private HeroController hero;
    /// <summary>本次 E 键锁定、等待动画事件结算的目标。</summary>
    private EnemyController pendingTarget;

    void Awake() { hero = GetComponent<HeroController>(); }

    void Update()
    {
        if (hero == null) return;
        // 剧情/暂停/死亡/过渡期间不响应刺杀输入。
        if (GameplayInputGate.Blocked) return;
        // 剧情 UI 打开时不响应刺杀输入。
        if (StoryDirector.Instance != null && StoryDirector.Instance.IsUiOpen) return;
        // 攻击动作结束仍未结算，说明攻击被打断，丢弃锁定的目标。
        if (!hero.isAttack) pendingTarget = null;
        if (Input.GetKeyDown(KeyCode.E)
            && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            TryAssassinate();
        }
    }

    /// <summary>查找当前可刺杀的最近目标；没有则返回 false。</summary>
    public bool CanAssassinate(out EnemyController target)
    {
        target = null;
        if (GameplayInputGate.Blocked) return false;
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

    /// <summary>发起刺杀：锁定目标、转向目标并播放攻击动画；命中由动画事件结算。</summary>
    public bool TryAssassinate()
    {
        if (!CanAssassinate(out EnemyController target)) return false;
        pendingTarget = target;
        hero.tarEnemy = target.gameObject;
        hero.enemyScript = target;

        // 立即面向目标，保证刺杀动画方向正确。
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
    /// <summary>动画事件消费刺杀目标：再次校验条件，符合则返回目标并清空锁定。</summary>
    public bool TryConsumeTarget(out EnemyController target)
    {
        target = pendingTarget;
        pendingTarget = null;
        return target != null && IsEligible(target, false);
    }

    /// <summary>
    /// 刺杀条件判定：存活、距离、目标背对主角、主角面向目标、目标仍在巡逻、剧情允许击杀。
    /// </summary>
    /// <param name="requireReady">true 表示发起时校验（主角不能正在攻击）；false 表示动画结算时校验。</param>
    private bool IsEligible(EnemyController enemy, bool requireReady)
    {
        if (enemy == null || enemy.isDeath) return false;
        if (requireReady && hero.isAttack) return false;

        // 目标是否背对主角：目标 forward 与「目标→主角」方向越相反越适合背刺。
        Vector3 toHero = transform.position - enemy.transform.position;
        toHero.y = 0f;
        float distance = toHero.magnitude;
        if (distance > range || distance < 0.001f) return false;
        if (Vector3.Dot(enemy.transform.forward, toHero.normalized) > behindDot) return false;

        // 主角是否正面朝向目标。
        Vector3 toEnemy = -toHero;
        if (Vector3.Dot(transform.forward, toEnemy.normalized) < facingDot) return false;

        // 已进入警戒/追击的目标不能被背刺。
        var senses = enemy.GetComponent<GuardSenses>();
        if (senses == null || senses.State != GuardState.Patrol) return false;
        // 剧情保护目标（例如未解锁的队长）不允许击杀。
        var storyTarget = enemy.GetComponent<StoryTarget>();
        return storyTarget == null || storyTarget.allowKill;
    }
}
