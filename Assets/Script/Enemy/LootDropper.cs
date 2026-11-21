using UnityEngine;

/// <summary>
/// 敌人死亡掉落：在尸体位置生成一次药水拾取物，保证同一敌人只掉落一次。
/// </summary>
public class LootDropper : MonoBehaviour
{
    /// <summary>药水拾取物预制体。</summary>
    public GameObject potionPickupPrefab;
    /// <summary>是否已经掉落过。</summary>
    private bool dropped;

    /// <summary>执行掉落；重复调用会被忽略。</summary>
    public void Drop()
    {
        if (dropped) return;
        dropped = true;
        if (potionPickupPrefab != null) Instantiate(potionPickupPrefab, transform.position + Vector3.up * 0.25f, Quaternion.identity);
    }
}
