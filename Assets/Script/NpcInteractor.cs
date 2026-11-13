using UnityEngine;

public class NpcInteractor : MonoBehaviour
{
    public DialogueAsset dialogue;
    public float range = 2f;
    private HeroController player;

    public bool CanInteract()
    {
        if (player == null) player = FindObjectOfType<HeroController>();
        if (player == null || player.IsDead || dialogue == null) return false;
        if (StoryDirector.Instance != null && StoryDirector.Instance.IsUiOpen) return false;
        return Vector3.Distance(player.transform.position, transform.position) <= range;
    }

    public void Interact()
    {
        if (!CanInteract() || StoryDirector.Instance == null) return;
        StoryDirector.Instance.OpenDialogue(dialogue);
    }

    void Update() { if (Input.GetKeyDown(KeyCode.E)) Interact(); }
}
