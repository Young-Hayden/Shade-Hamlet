using System.Collections.Generic;
using UnityEngine;

public class AttackTriggerBox : MonoBehaviour
{
    // 定义一个攻击者变量
    public Transform attackPlayer;
    // 定义一个是否为主角攻击的变量
    public bool IsPlayerAttack = true;

    // 定义伤害值
    public float damage;

    // 同一个攻击盒对同一个目标只结算一次伤害，避免多个碰撞器/多次触发把伤害翻倍。
    private readonly HashSet<int> hitTargets = new HashSet<int>();

    // 定义一个接收外部传值的默认函数
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
    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        // 不打攻击者自己
        if (attackPlayer != null && other.transform == attackPlayer) return;

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
