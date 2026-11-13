using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class HeroController : MonoBehaviour
{
    // 定义一个NavMeshAgent对象，用它来控制人物在地图上移动。
    public NavMeshAgent nav;
    // 定义一个坐标变量，用来指定人物移动的终点。
    public Vector3 target;

    // 定义一个Animator的变量，用来获取自身的Animator组件
    public Animator anim;

    // 定义一个敌人的游戏物体的变量
    public GameObject tarEnemy;

    // 定义一个攻击状态
    public bool isAttack = false;

    // 定义调用敌人身上的脚本的变量EnemyController.cs
    public EnemyController enemyScript;

    // 定义一个画布中的脚本变量，用来获取画布中的血条
    public HPScript hpCanvas;

    [Header("战斗参数")]
    // 定义主角的攻击力
    [SerializeField] private float ATK = 20f;
    // 定义最大血量值
    [SerializeField] private float HP_Max = 100f;
    // 进入攻击的距离。实测攻击盒在约1.5米内才能命中，贴到这个距离再出刀才不会空挥。
    [SerializeField] private float attackRange = 1.4f;
    // 退出攻击的距离（滞回）。超过此距离重新追击，避免在攻击盒触及范围外持续空挥。
    [SerializeField] private float attackExitRange = 2.0f;
    // 两次攻击之间的间隔，与攻击动画的时长对应。
    [SerializeField] private float attackInterval = 1f;

    // 定义当前血量值
    private float HP_Now;
    // 定义死亡状态
    private bool isDeath = false;

    // 缓存主摄像机，避免每次点击都调用Camera.main做全场景查找。
    private Camera mainCamera;

    [Header("直接动作控制")]
    public bool directControl = false;
    public HeroDirectMovement directMovement;

    // 对外只读属性，便于调试与测试。
    public float CurrentHP { get { return HP_Now; } }
    public float MaxHP { get { return HP_Max; } }
    public bool IsDead { get { return isDeath; } }

    // 显式切换直接动作/旧点选模式，避免两种位移驱动同时工作。
    public void SetControlMode(bool value)
    {
        directControl = value;
        if (directMovement == null) directMovement = GetComponent<HeroDirectMovement>();
        CharacterController cc = GetComponent<CharacterController>();
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (value)
        {
            if (cc == null) cc = gameObject.AddComponent<CharacterController>();
            cc.height = 2.4f; cc.center = new Vector3(0f, 1.2f, 0f); cc.radius = 0.35f;
            cc.slopeLimit = 45f; cc.stepOffset = 0.3f;
            cc.enabled = true;
            if (capsule != null) capsule.enabled = false;
            if (nav != null) nav.enabled = false;
            if (directMovement != null) directMovement.enabled = true;
        }
        else
        {
            if (cc != null) cc.enabled = false;
            if (capsule != null) capsule.enabled = true;
            if (nav != null) nav.enabled = true;
            if (directMovement != null) directMovement.enabled = false;
        }
    }

    void Start()
    {
        // 将自身的NavMeshAgent组件值赋值给nav;
        if (nav == null) nav = GetComponent<NavMeshAgent>();
        // 获取自身的Animator组件
        if (anim == null) anim = GetComponent<Animator>();
        mainCamera = Camera.main;
        SetControlMode(directControl);

        // 位移完全交给NavMeshAgent：刚体设为Kinematic，只用于攻击盒的触发检测。
        // 否则刚体推挤会把两个角色顶到约1.9米开外，还会让尸体受重力穿地下坠。
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 设置主角的当前生命值为最大生命值。
        HP_Now = HP_Max;
        // 设置血条的血量值
        if (hpCanvas != null) hpCanvas.SetHP(HP_Now, HP_Max);
    }

    void Update()
    {
        // 死亡后不再响应移动与攻击，避免"尸体跑路"。
        if (isDeath) return;
        if (directControl)
        {
            if (directMovement != null) directMovement.Move();
            DoDirectAttack();
        }
        else
        {
            DoMove();
            DoAttack();
        }
    }

    // 鼠标是否点在了UI上，点UI时不应该驱动角色移动。
    private bool PointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // 结束当前攻击：清理状态、取消延迟回调并回到非攻击动画。
    private void StopAttack()
    {
        isAttack = false;
        CancelInvoke("AttackEnd");
        if (anim != null) anim.SetTrigger("attackEnd");
        if (nav != null && nav.enabled && nav.isOnNavMesh) nav.isStopped = false;
    }

    // 定义移动函数，将和移动相关的代码放这里
    public void DoMove()
    {
        // 通过鼠标点击来设置人物的移动位置（终点）。
        if (Input.GetMouseButtonDown(0) && !PointerOverUI())
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit) && hit.transform.CompareTag("earth"))
                {
                    // 点地移动：放弃当前攻击目标并结束攻击状态。
                    tarEnemy = null;
                    enemyScript = null;
                    StopAttack();

                    // 设置终点为鼠标所指物体的位置
                    target = hit.point;
                    nav.SetDestination(target);
                }
            }
        }

        // 根据寻路速度切换走路动画；不再每帧触发attackEnd，避免攻击动画被反复打断。
        if (anim != null) anim.SetBool("walk", !isAttack && nav.velocity.magnitude > 0.1f);
    }

    // 定义一个攻击函数，将主角攻击的内容放这里
    public void DoAttack()
    {
        // 点击鼠标右键选中敌人作为攻击目标
        if (Input.GetMouseButtonDown(1) && !PointerOverUI())
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit) && hit.transform.CompareTag("Enemy"))
                {
                    EnemyController ec = hit.transform.GetComponent<EnemyController>();
                    if (ec != null && !ec.isDeath)
                    {
                        tarEnemy = hit.transform.gameObject;
                        enemyScript = ec;
                        // 换目标时重置攻击状态，保证能立刻追上去。
                        StopAttack();
                    }
                }
            }
        }

        if (tarEnemy == null) return;
        if (enemyScript == null) enemyScript = tarEnemy.GetComponent<EnemyController>();

        // 目标已死亡或被销毁：收刀并解除锁定。
        if (enemyScript == null || enemyScript.isDeath || enemyScript.showHP() <= 0f)
        {
            tarEnemy = null;
            enemyScript = null;
            StopAttack();
            return;
        }

        // 使用水平距离判定，避免地形高低差让攻击距离失效。
        Vector3 dir = tarEnemy.transform.position - transform.position;
        dir.y = 0f;
        float dist = dir.magnitude;

        // 出刀后允许目标稍微走远一点，超过退出距离才重新追击。
        float limit = isAttack ? attackExitRange : attackRange;
        if (dist > limit)
        {
            // 关键修复：每一帧都按敌人当前位置更新追击终点，否则敌人巡逻时永远追不上。
            nav.isStopped = false;
            if (isAttack) StopAttack();
            Vector3 chasePos = tarEnemy.transform.position - dir.normalized * (attackRange - 0.5f);
            nav.SetDestination(chasePos);
            return;
        }

        // 进入攻击距离：停下、面向敌人、按间隔出刀。
        if (nav.enabled && nav.isOnNavMesh)
        {
            nav.ResetPath();
            nav.isStopped = true;
        }
        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir.normalized), 12f * Time.deltaTime);
        }

        if (!isAttack)
        {
            isAttack = true;
            anim.ResetTrigger("attackEnd");
            anim.SetTrigger("attack");
            // 攻击间隔结束后允许下一次出刀。
            Invoke("AttackEnd", attackInterval);
        }
    }

    // 直接模式右键挥剑：只选择近身前方目标，不驱动自动追击。
    private void DoDirectAttack()
    {
        if (Input.GetMouseButtonDown(1) && !PointerOverUI()) TryDirectAttack();
    }

    public void TryDirectAttack()
    {
        if (isDeath || isAttack) return;

        EnemyController best = null;
        float bestDistance = float.MaxValue;
        foreach (EnemyController enemy in FindObjectsOfType<EnemyController>())
        {
            if (enemy == null || enemy.isDeath) continue;
            Vector3 dir = enemy.transform.position - transform.position;
            dir.y = 0f;
            float distance = dir.magnitude;
            if (distance > attackRange + 0.3f || distance < 0.001f) continue;
            if (Vector3.Dot(transform.forward, dir.normalized) < 0f) continue;
            if (distance < bestDistance) { best = enemy; bestDistance = distance; }
        }

        Vector3 facing = Vector3.zero;
        if (best != null)
        {
            tarEnemy = best.gameObject;
            enemyScript = best;
            facing = best.transform.position - transform.position;
        }
        else
        {
            tarEnemy = null;
            enemyScript = null;
            if (mainCamera != null) facing = mainCamera.transform.forward;
        }
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(facing.normalized);

        isAttack = true;
        anim.ResetTrigger("attackEnd");
        anim.SetTrigger("attack");
        Invoke("AttackEnd", attackInterval);
    }

    // 定义一个停止攻击的函数
    public void AttackEnd()
    {
        isAttack = false;
    }

    // 治疗并返回实际恢复量；满血、死亡或非法输入不消耗物品。
    public float Heal(float amount)
    {
        if (isDeath || amount <= 0f) return 0f;
        float oldHP = HP_Now;
        HP_Now = Mathf.Clamp(HP_Now + amount, 0f, HP_Max);
        float healed = HP_Now - oldHP;
        if (healed > 0f && hpCanvas != null) hpCanvas.SetHP(HP_Now, HP_Max);
        return healed;
    }

    // 定义一个返回攻击力的函数
    public float showATK()
    {
        return ATK;
    }

    // 定义一个伤害、扣血的函数
    public void Hurt(float damage, Transform attacker)
    {
        // 已死亡就不再重复结算伤害、音效和销毁。
        if (isDeath) return;

        // 计算伤害并夹紧到[0, HP_Max]，血条不会出现负数。
        float oldHP = HP_Now;
        HP_Now = Mathf.Clamp(HP_Now - damage, 0f, HP_Max);
        float actualDamage = oldHP - HP_Now;
        if (hpCanvas != null) hpCanvas.SetHP(HP_Now, HP_Max);

        // 只有实际扣血才产生反馈；反馈服务缺失不影响伤害结算。
        if (actualDamage > 0f && CombatFeedback.Instance != null)
        {
            CombatFeedback.Instance.ShowDamage(transform.position + Vector3.up * 2.1f, actualDamage, true);
            CombatFeedback.Instance.PlayHit(transform.position);
        }

        // 修正原来的"Enemny"拼写错误，记录攻击者。
        if (attacker != null && attacker.CompareTag("Enemy"))
        {
            enemyScript = attacker.GetComponent<EnemyController>();
        }

        if (HP_Now <= 0f) Die();
    }

    // 死亡只执行一次。
    private void Die()
    {
        isDeath = true;
        isAttack = false;
        tarEnemy = null;
        CancelInvoke("AttackEnd");

        if (nav != null && nav.enabled)
        {
            if (nav.isOnNavMesh)
            {
                nav.ResetPath();
                nav.isStopped = true;
            }
            nav.enabled = false;
        }
        // 尸体不再参与碰撞，避免挡住活着的角色。
        foreach (Collider c in GetComponents<Collider>()) c.enabled = false;

        if (anim != null)
        {
            anim.SetBool("walk", false);
            anim.ResetTrigger("attack");
            anim.SetBool("death", true);
        }

        // 添加死亡音效
        if (GameMusic.Instance != null) GameMusic.Instance.PlayMusic(1);
        if (GameFlow.Instance != null) GameFlow.Instance.OnHeroDeath();

        // 2秒后销毁尸体
        Destroy(gameObject, 2f);
    }
}
