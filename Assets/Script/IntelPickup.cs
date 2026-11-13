using UnityEngine;

public class IntelPickup : MonoBehaviour
{
    public float range = 1.6f;
    private HeroController player;

    void Awake() { player = FindObjectOfType<HeroController>(); }

    public bool CanInteract()
    {
        if (player == null) player = FindObjectOfType<HeroController>();
        return player != null && !player.IsDead
            && MissionController.Instance != null
            && MissionController.Instance.Stage == MissionStage.CollectIntel
            && Vector3.Distance(player.transform.position, transform.position) <= range;
    }

    public void Interact()
    {
        if (!CanInteract()) return;
        MissionController.Instance.RegisterIntel();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) Interact();
    }
}
