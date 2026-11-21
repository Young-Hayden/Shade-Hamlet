using UnityEngine;

/// <summary>
/// 线索拾取物（例如家书）：玩家进入范围按 E，登记线索、隐藏自身并显示阅读对话。
/// </summary>
public class CluePickup : MonoBehaviour
{
    /// <summary>线索 ID，必须与章节 requiredClueIds 中的某一项一致。</summary>
    public string clueId = "family_letter";
    /// <summary>可拾取距离。</summary>
    public float range = 1.6f;
    /// <summary>阅读内容对话（可为空）。</summary>
    public DialogueAsset content;
    /// <summary>主角引用。</summary>
    private HeroController player;

    /// <summary>是否满足拾取条件。</summary>
    public bool CanInteract()
    {
        if (GameplayInputGate.Blocked) return false;
        if (player == null) player = FindObjectOfType<HeroController>();
        if (player == null || player.IsDead || StoryDirector.Instance == null) return false;
        if (StoryDirector.Instance.IsUiOpen) return false;
        return Vector3.Distance(player.transform.position, transform.position) <= range;
    }

    /// <summary>拾取：登记线索、移除物件、打开阅读对话。</summary>
    public void Interact()
    {
        if (!CanInteract()) return;
        StoryDirector.Instance.RegisterClue(clueId);
        gameObject.SetActive(false);
        if (content != null) StoryDirector.Instance.OpenDialogue(content);
    }

    void Update()
    {
        if (GameplayInputGate.Blocked) return;
        if (Input.GetKeyDown(KeyCode.E)) Interact();
    }
}
