using UnityEngine;

[RequireComponent(typeof(HeroController))]
public class SmokeAbility : MonoBehaviour
{
    public GameObject smokePrefab;
    public float duration = 4f;
    public float cooldown = 12f;
    public float radius = 3f;
    private float readyAt;
    private HeroController hero;

    public float RemainingCooldown { get { return Mathf.Max(0f, readyAt - Time.time); } }

    void Awake() { hero = GetComponent<HeroController>(); }
    void Update() { if (Input.GetKeyDown(KeyCode.G)) TryActivate(); }

    public bool TryActivate()
    {
        if (hero == null) hero = GetComponent<HeroController>();
        if (hero.IsDead || smokePrefab == null || Time.time < readyAt) return false;
        readyAt = Time.time + cooldown;
        var smoke = Instantiate(smokePrefab, transform.position, Quaternion.identity);
        var zone = smoke.GetComponent<SmokeZone>();
        if (zone == null) zone = smoke.AddComponent<SmokeZone>();
        zone.radius = radius;
        Destroy(smoke, duration);
        return true;
    }
}
