using UnityEngine;

public class PotionInventory : MonoBehaviour
{
    public int Count { get; private set; }
    public float healAmount = 30f;
    private HeroController hero;

    void Awake() { hero = GetComponent<HeroController>(); }
    void Update() { if (Input.GetKeyDown(KeyCode.Q)) TryUse(); }
    public void Add(int amount) { if (amount > 0) Count += amount; }

    public bool TryUse()
    {
        if (hero == null || hero.IsDead || Count <= 0) return false;
        float healed = hero.Heal(healAmount);
        if (healed <= 0f) return false;
        Count--;
        return true;
    }
}
