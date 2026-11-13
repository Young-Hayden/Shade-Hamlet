using UnityEngine;

public class CombatFeedback : MonoBehaviour
{
    public static CombatFeedback Instance { get; private set; }
    public GameObject hitEffect;
    public GameObject killEffect;

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void ShowDamage(Vector3 position, float damage, bool playerWasHit)
    {
        if (damage <= 0f) return;
        DamagePopup.Create(position, damage, playerWasHit);
    }

    public void PlayHit(Vector3 position) { PlayEffect(hitEffect, position + Vector3.up, 2f); }
    public void PlayKill(Vector3 position) { PlayEffect(killEffect, position + Vector3.up, 2.5f); }

    private void PlayEffect(GameObject prefab, Vector3 position, float lifetime)
    {
        if (prefab == null) return;
        Destroy(Instantiate(prefab, position, Quaternion.identity), lifetime);
    }
}
