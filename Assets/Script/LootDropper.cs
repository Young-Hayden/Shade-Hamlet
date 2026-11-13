using UnityEngine;

public class LootDropper : MonoBehaviour
{
    public GameObject potionPickupPrefab;
    private bool dropped;

    public void Drop()
    {
        if (dropped) return;
        dropped = true;
        if (potionPickupPrefab != null) Instantiate(potionPickupPrefab, transform.position + Vector3.up * 0.25f, Quaternion.identity);
    }
}
