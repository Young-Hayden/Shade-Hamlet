using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 剧情目标（卫兵队长）的行为控制：
/// - 剧情走位期间关闭感知与战斗，只按脚本移动到集合点；
/// - 玩家靠近时自动触发对峙；
/// - 结局时按分支决定死亡（倒地被击杀）或离开。
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class StoryTarget : MonoBehaviour
{
    /// <summary>是否允许被玩家直接击杀（未解锁剧情时禁止）。</summary>
    public bool allowKill;
    /// <summary>自动触发对峙的距离。</summary>
    public float confrontRange = 2f;
    /// <summary>敌人控制器。</summary>
    private EnemyController controller;
    /// <summary>感知组件。</summary>
    private GuardSenses senses;
    /// <summary>寻路组件。</summary>
    private NavMeshAgent agent;
    /// <summary>主角引用。</summary>
    private HeroController hero;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        senses = GetComponent<GuardSenses>();
        agent = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// 切换脚本托管状态：true 时关闭感知/战斗并停下寻路，由剧情逻辑控制；
    /// false 时恢复普通敌人行为。
    /// </summary>
    public void SetScriptedPassive(bool value)
    {
        if (senses != null) senses.enabled = !value;
        if (controller != null) controller.enabled = !value;
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = value;
    }

    /// <summary>让队长走到剧情集合点（井边），期间保持脚本托管。</summary>
    public void MoveToStoryPoint(Transform point)
    {
        if (point == null) return;
        SetScriptedPassive(true);
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(point.position);
        }
    }

    /// <summary>对峙开始：停下并面向主角。</summary>
    public void BeginConfront()
    {
        SetScriptedPassive(true);
        if (hero == null) hero = FindObjectOfType<HeroController>();
        if (hero == null) return;
        Vector3 dir = hero.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    /// <summary>
    /// 结局表现：击杀分支直接按最大生命击杀队长；放走分支让队长朝远离主角的方向离开。
    /// </summary>
    public void ApplyStoryEnding(string endingId)
    {
        allowKill = endingId == "kill" || endingId == "kill_after_truth";
        if (allowKill)
        {
            if (hero == null) hero = FindObjectOfType<HeroController>();
            if (controller != null && !controller.isDeath)
                controller.Hurt(controller.MaxHP, hero != null ? hero.transform : null);
            return;
        }

        SetScriptedPassive(true);
        if (hero == null) hero = FindObjectOfType<HeroController>();
        if (hero != null && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            Vector3 away = transform.position - hero.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = transform.forward;
            agent.isStopped = false;
            agent.SetDestination(transform.position + away.normalized * 8f);
        }
    }

    void Update()
    {
        if (hero == null) hero = FindObjectOfType<HeroController>();
        // 线索齐备且玩家走近：自动进入对峙（UI 打开时不再重复触发）。
        var director = StoryDirector.Instance;
        if (director != null && director.Stage == StoryStage.ConfrontReady && !director.IsUiOpen
            && hero != null && !hero.IsDead
            && Vector3.Distance(hero.transform.position, transform.position) <= confrontRange)
        {
            director.BeginConfront();
            return;
        }

        // 普通敌人逻辑被脚本关掉时，仍要维护走路动画（例如剧情走位）。
        if (controller == null || controller.enabled || agent == null || !agent.enabled) return;
        if (controller.animator != null)
            controller.animator.SetBool("walk", agent.velocity.magnitude > 0.05f);
    }
}
