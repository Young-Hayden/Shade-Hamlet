using UnityEngine;

public class HeroAnimator : MonoBehaviour
{
    // 定义攻击点位置，AttackTriggerBox出现的位置坐标。
    public Transform Attack1pos;
    // 定义攻击盒子的预制体变量
    public GameObject AttackTriggerBox;

    // 定义获取HeroController的变量
    public HeroController hero;

    // 定义攻击敌人的函数（由攻击动画的动画事件调用）
    void AttackEnemyEvent()
    {
        // 死亡后动画事件不再生成攻击盒。
        if (hero == null || hero.IsDead || AttackTriggerBox == null || Attack1pos == null) return;

        // 刺杀仍在同一个攻击动画事件结算；不符合条件时回退为普通攻击盒。
        var assassination = hero.GetComponent<AssassinationController>();
        if (assassination != null && assassination.TryConsumeTarget(out EnemyController assassinationTarget))
        {
            if (GameMusic.Instance != null) GameMusic.Instance.PlayMusic(0);
            assassinationTarget.Hurt(assassinationTarget.MaxHP, hero.transform);
            return;
        }

        // 获取HeroController中的ATK；
        float atk = hero.showATK();
        if (GameMusic.Instance != null) GameMusic.Instance.PlayMusic(0);
        var noise = hero.GetComponent<NoiseEmitter>();
        if (noise != null) noise.EmitNoise(4f, 0.75f, hero.transform.position);

        // 在攻击点位置创建一个攻击盒子
        GameObject box = Instantiate(AttackTriggerBox, Attack1pos.position, Attack1pos.rotation);
        // 定义攻击盒子的大小与攻击点位置的大小一致。
        box.transform.localScale = Attack1pos.localScale;

        // 将主角的攻击力传给AttackTriggerBox.cs中的OnTriggerEnter()
        AttackTriggerBox trigger = box.GetComponent<AttackTriggerBox>();
        if (trigger != null) trigger.setAttackInit(true, atk, hero.transform);
    }
}
