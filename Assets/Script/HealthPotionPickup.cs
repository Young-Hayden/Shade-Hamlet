using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class HealthPotionPickup : MonoBehaviour
{
    private bool picked;

    void Update() { transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World); }

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
