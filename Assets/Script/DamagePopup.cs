using UnityEngine;
using UnityEngine.UI;

public class DamagePopup : MonoBehaviour
{
    private const float Lifetime = 0.85f;
    private CanvasGroup group;
    private float age;

    public static DamagePopup Create(Vector3 position, float damage, bool playerWasHit)
    {
        var canvas = RpgUI.CreateCanvas("DamagePopup", null, false);
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180f, 100f);
        rect.position = position;
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
        transform.position += Vector3.up * (0.65f * Time.deltaTime);
        if (group != null) group.alpha = 1f - Mathf.Clamp01(age / Lifetime);
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        if (age >= Lifetime) Destroy(gameObject);
    }
}
