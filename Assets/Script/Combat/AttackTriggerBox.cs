using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一次性攻击盒：由攻击动画事件在攻击点生成，0.2 秒后自动销毁。
/// 按攻击者阵营只对敌对目标结算一次伤害；同一个目标在同一攻击盒内只吃一次伤害。
/// </summary>
public class AttackTriggerBox : MonoBehaviour
{
    // 定义一个攻击者变量
    /// <summary>发起攻击的角色根节点变换，用于结算伤害和避免自伤。</summary>
    public Transform attackPlayer;
    // 定义一个是否为主角攻击的变量
    /// <summary>true 表示主角攻击（伤害 Enemy），false 表示敌人攻击（伤害 Player）。</summary>
    public bool IsPlayerAttack = true;

    // 定义伤害值
    /// <summary>本次攻击的伤害值。</summary>
    public float damage;

    // 同一个攻击盒对同一个目标只结算一次伤害，避免多个碰撞器/多次触发把伤害翻倍。
    private readonly HashSet<int> hitTargets = new HashSet<int>();

    // 定义一个接收外部传值的默认函数
    /// <summary>初始化攻击盒：阵营、伤害与攻击者。</summary>
    public void setAttackInit(bool isPlayer, float dam, Transform attacker)
    {
        // 接收外部传入的相关参数：是否为主角和攻击力
        IsPlayerAttack = isPlayer;
        damage = dam;
        attackPlayer = attacker;
    }

    void Start()
    {
        // 预制体出现0.2秒后自动销毁
        Destroy(gameObject, 0.2f);
    }

    // 设置验证Trigger碰撞的函数
    /// <summary>碰到敌对目标时结算一次伤害；忽略自身与已命中目标。</summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        // 不打攻击者自己
        if (attackPlayer != null && other.transform == attackPlayer) return;

        // 主角攻击只打 Enemy，敌人攻击只打 Player。
        string wantedTag = IsPlayerAttack ? "Enemy" : "Player";
        if (!other.CompareTag(wantedTag)) return;

        // 每个目标只受一次伤害
        if (!hitTargets.Add(other.GetInstanceID())) return;

        if (IsPlayerAttack)
        {
            // 如果碰到的是敌人，则扣血
            EnemyController ec = other.GetComponent<EnemyController>();
            if (ec != null) ec.Hurt(damage, attackPlayer);
        }
        else
        {
            // 如果碰到的是主角，则扣血
            HeroController hc = other.GetComponent<HeroController>();
            if (hc != null) hc.Hurt(damage, attackPlayer);
        }
    }
}
