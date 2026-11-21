using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 世界空间伤害弹字：在受击位置生成，向上漂浮并在 0.85 秒内淡出后自毁。
/// 通过 RpgUI 运行时创建 Canvas/Text，不依赖预制体。
/// </summary>
public class DamagePopup : MonoBehaviour
{
    /// <summary>弹字总寿命（秒）。</summary>
    private const float Lifetime = 0.85f;
    /// <summary>透明度控制。</summary>
    private CanvasGroup group;
    /// <summary>已存活时间。</summary>
    private float age;

    /// <summary>在指定世界坐标生成一个伤害数字。</summary>
    /// <param name="playerWasHit">true 用红色（主角受伤），false 用橙色（敌人受伤）。</param>
    public static DamagePopup Create(Vector3 position, float damage, bool playerWasHit)
    {
        var canvas = RpgUI.CreateCanvas("DamagePopup", null, false);
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180f, 100f);
        rect.position = position;
        // 世界空间画布缩放得很小，字号在场景里才符合常理。
        rect.localScale = Vector3.one * 0.005f;
        var popup = canvas.gameObject.AddComponent<DamagePopup>();
        popup.group = canvas.gameObject.AddComponent<CanvasGroup>();
        RpgUI.CreateText("Value", rect, Mathf.RoundToInt(damage).ToString(), 90,
            playerWasHit ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.56f, 0.29f),
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, rect.sizeDelta);
        return popup;
    }

    void Update()
    {
        age += Time.deltaTime;
        // 匀速上浮，接近寿命末尾时淡出。
        transform.position += Vector3.up * (0.65f * Time.deltaTime);
        if (group != null) group.alpha = 1f - Mathf.Clamp01(age / Lifetime);
        // 始终正面朝向相机，避免侧面看见镜像文字。
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        if (age >= Lifetime) Destroy(gameObject);
    }
}
