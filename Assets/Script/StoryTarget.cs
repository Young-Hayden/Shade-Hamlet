using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyController))]
public class StoryTarget : MonoBehaviour
{
    public bool allowKill;
    public float confrontRange = 2f;
    private EnemyController controller;
    private GuardSenses senses;
    private NavMeshAgent agent;
    private HeroController hero;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        senses = GetComponent<GuardSenses>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void SetScriptedPassive(bool value)
    {
        if (senses != null) senses.enabled = !value;
        if (controller != null) controller.enabled = !value;
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = value;
    }

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

    public void BeginConfront()
    {
        SetScriptedPassive(true);
        if (hero == null) hero = FindObjectOfType<HeroController>();
        if (hero == null) return;
        Vector3 dir = hero.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

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
        var director = StoryDirector.Instance;
        if (director != null && director.Stage == StoryStage.ConfrontReady && !director.IsUiOpen
            && hero != null && !hero.IsDead
            && Vector3.Distance(hero.transform.position, transform.position) <= confrontRange)
        {
            director.BeginConfront();
            return;
        }

        if (controller == null || controller.enabled || agent == null || !agent.enabled) return;
        if (controller.animator != null)
            controller.animator.SetBool("walk", agent.velocity.magnitude > 0.05f);
    }
}
