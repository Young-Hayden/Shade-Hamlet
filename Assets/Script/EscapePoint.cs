using UnityEngine;

public class EscapePoint : MonoBehaviour
{
    public float range = 2f;
    public GameObject lightray;
    private HeroController player;

    void Awake() { player = FindObjectOfType<HeroController>(); }

    public bool CanInteract()
    {
        if (player == null) player = FindObjectOfType<HeroController>();
        return player != null && !player.IsDead
            && MissionController.Instance != null
            && MissionController.Instance.Stage == MissionStage.Escape
            && Vector3.Distance(player.transform.position, transform.position) <= range;
    }

    public bool Interact()
    {
        return CanInteract() && MissionController.Instance.TryExtract();
    }

    void Update()
    {
        if (lightray != null)
        {
            bool show = MissionController.Instance != null && MissionController.Instance.HasIntel
                && MissionController.Instance.Stage != MissionStage.Completed;
            if (lightray.activeSelf != show) lightray.SetActive(show);
        }
        if (Input.GetKeyDown(KeyCode.E)) Interact();
    }
}
