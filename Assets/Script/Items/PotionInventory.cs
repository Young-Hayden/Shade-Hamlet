using UnityEngine;

/// <summary>
/// 主角药水库存：记录数量，Q 键或 HUD 按钮使用。
/// 只有实际恢复生命时才扣除，满血/死亡/空库存不消耗。
/// </summary>
public class PotionInventory : MonoBehaviour
{
    /// <summary>当前药水数量。</summary>
    public int Count { get; private set; }
    /// <summary>单瓶治疗量。</summary>
    public float healAmount = 30f;
    /// <summary>主角控制器。</summary>
    private HeroController hero;

    void Awake() { hero = GetComponent<HeroController>(); }
    void Update()
    {
        if (GameplayInputGate.Blocked) return;
        if (Input.GetKeyDown(KeyCode.Q)) TryUse();
    }

    /// <summary>增加药水数量；非正数忽略。</summary>
    public void Add(int amount) { if (amount > 0) Count += amount; }

    /// <summary>尝试使用一瓶药水；成功恢复生命才扣减并返回 true。</summary>
    public bool TryUse()
    {
        // 剧情/暂停/死亡/过渡期间不允许使用（HUD 按钮与 Q 键共用此入口）。
        if (GameplayInputGate.Blocked) return false;
        if (hero == null || hero.IsDead || Count <= 0) return false;
        float healed = hero.Heal(healAmount);
        if (healed <= 0f) return false;
        Count--;
        return true;
    }
}
