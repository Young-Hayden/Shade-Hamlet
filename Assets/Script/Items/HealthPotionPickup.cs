using UnityEngine;

/// <summary>
/// 治疗药水拾取物：世界空间旋转展示，玩家碰到后向库存 +1 并消失。
/// 同一物件只拾取一次。
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class HealthPotionPickup : MonoBehaviour
{
    /// <summary>是否已经被拾取。</summary>
    private bool picked;

    void Update() { transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World); }

    /// <summary>玩家进入触发器时拾取；死亡或无库存组件时忽略。</summary>
    private void OnTriggerEnter(Collider other)
    {
        if (picked) return;
        var hero = other.GetComponentInParent<HeroController>();
        var inventory = hero != null ? hero.GetComponent<PotionInventory>() : null;
        if (inventory == null || hero.IsDead) return;
        picked = true;
        inventory.Add(1);
        Destroy(gameObject);
    }
}
