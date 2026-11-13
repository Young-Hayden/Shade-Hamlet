using UnityEngine;

public class EavesdropZone : MonoBehaviour
{
    public string clueId = "barracks_eavesdrop";
    public float radius = 4f;
    public float requiredSeconds = 3f;
    public DialogueAsset content;
    private HeroController player;
    private float timer;
    private bool registered;

    void Update()
    {
        if (registered || StoryDirector.Instance == null) return;
        if (StoryDirector.Instance.IsUiOpen) return;
        if (player == null) player = FindObjectOfType<HeroController>();
        if (player == null || player.IsDead) return;

        bool inside = Vector3.Distance(player.transform.position, transform.position) <= radius;
        bool chased = false;
        foreach (var guard in FindObjectsOfType<GuardSenses>())
            if (guard.State == GuardState.Chase) { chased = true; break; }

        if (inside && !chased)
        {
            timer += Time.deltaTime;
            if (timer >= requiredSeconds)
            {
                registered = true;
                StoryDirector.Instance.RegisterClue(clueId);
                if (content != null) StoryDirector.Instance.OpenDialogue(content);
            }
        }
        else timer = 0f;
    }
}
