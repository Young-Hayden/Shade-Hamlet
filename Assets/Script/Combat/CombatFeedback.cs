using UnityEngine;

/// <summary>
/// 战斗反馈服务（单例）：显示伤害弹字，并生成命中/击杀粒子。
/// 服务缺失时调用方会降级处理，不影响伤害结算本身。
/// </summary>
public class CombatFeedback : MonoBehaviour
{
    /// <summary>全局唯一实例。</summary>
    public static CombatFeedback Instance { get; private set; }
    /// <summary>命中粒子预制体（角色数量多时复用同一资源）。</summary>
    public GameObject hitEffect;
    /// <summary>击杀粒子预制体。</summary>
    public GameObject killEffect;

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>在世界坐标处生成伤害数字；伤害 <=0 时不显示。</summary>
    /// <param name="playerWasHit">true 表示主角受伤（红字），false 表示敌人受伤（橙字）。</param>
    public void ShowDamage(Vector3 position, float damage, bool playerWasHit)
    {
        if (damage <= 0f) return;
        DamagePopup.Create(position, damage, playerWasHit);
    }

    /// <summary>在指定位置播放命中粒子。</summary>
    public void PlayHit(Vector3 position) { PlayEffect(hitEffect, position + Vector3.up, 2f); }
    /// <summary>在指定位置播放击杀粒子。</summary>
    public void PlayKill(Vector3 position) { PlayEffect(killEffect, position + Vector3.up, 2.5f); }

    /// <summary>实例化一次性特效，并在指定寿命后销毁。</summary>
    private void PlayEffect(GameObject prefab, Vector3 position, float lifetime)
    {
        if (prefab == null) return;
        Destroy(Instantiate(prefab, position, Quaternion.identity), lifetime);
    }
}
