using UnityEngine;

public class CluePickup : MonoBehaviour
{
    public string clueId = "family_letter";
    public float range = 1.6f;
    public DialogueAsset content;
    private HeroController player;

    public bool CanInteract()
    {
        if (player == null) player = FindObjectOfType<HeroController>();
        if (player == null || player.IsDead || StoryDirector.Instance == null) return false;
        if (StoryDirector.Instance.IsUiOpen) return false;
        return Vector3.Distance(player.transform.position, transform.position) <= range;
    }

    public void Interact()
    {
        if (!CanInteract()) return;
        StoryDirector.Instance.RegisterClue(clueId);
        gameObject.SetActive(false);
        if (content != null) StoryDirector.Instance.OpenDialogue(content);
    }

    void Update() { if (Input.GetKeyDown(KeyCode.E)) Interact(); }
}
