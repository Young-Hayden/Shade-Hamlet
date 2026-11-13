using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    // 定义攻击点位置，AttackTriggerBox出现的位置坐标。
    public Transform Attack1pos;
    // 定义攻击盒子的预制体变量
    public GameObject AttackTriggerBox;

    // 定义获取EnemyController的变量
    public EnemyController Enemy;

    // 定义攻击主角的函数（由攻击动画的动画事件调用）
    void AttackHerroEvent()
    {
        // 死亡后动画事件不再生成攻击盒。
        if (Enemy == null || Enemy.isDeath || AttackTriggerBox == null || Attack1pos == null) return;

        // 获取EnemyController中的ATK；
        // 远程卫兵由动画事件发射箭矢，近战卫兵继续使用攻击盒。
        var ranged = Enemy.GetComponent<RangedAttack>();
        if (ranged != null && ranged.enabled)
        {
            ranged.Fire();
            return;
        }

        float atk = Enemy.showATK();
        if (GameMusic.Instance != null) GameMusic.Instance.PlayMusic(0);

        // 在攻击点位置创建一个攻击盒子
        GameObject box = Instantiate(AttackTriggerBox, Attack1pos.position, Attack1pos.rotation);
        // 定义攻击盒子的大小与攻击点位置的大小一致。
        box.transform.localScale = Attack1pos.localScale;

        // 将敌人的攻击力传给AttackTriggerBox.cs中的OnTriggerEnter()
        AttackTriggerBox trigger = box.GetComponent<AttackTriggerBox>();
        if (trigger != null) trigger.setAttackInit(false, atk, Enemy.transform);
    }
}
