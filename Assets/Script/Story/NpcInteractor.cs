using UnityEngine;

/// <summary>
/// NPC 对话交互：玩家进入范围并按 E 时，打开该 NPC 配置的对话。
/// 剧情 UI 打开时不响应。
/// </summary>
public class NpcInteractor : MonoBehaviour
{
    /// <summary>该 NPC 的对话资产。</summary>
    public DialogueAsset dialogue;
    /// <summary>可交互距离。</summary>
    public float range = 2f;
    /// <summary>主角引用。</summary>
    private HeroController player;

    /// <summary>当前是否满足交互条件（存活、距离、剧情 UI 未打开、未暂停/死亡/过渡）。</summary>
    public bool CanInteract()
    {
        if (GameplayInputGate.Blocked) return false;
        if (player == null) player = FindObjectOfType<HeroController>();
        if (player == null || player.IsDead || dialogue == null) return false;
        if (StoryDirector.Instance != null && StoryDirector.Instance.IsUiOpen) return false;
        return Vector3.Distance(player.transform.position, transform.position) <= range;
    }

    /// <summary>开始对话。</summary>
    public void Interact()
    {
        if (!CanInteract() || StoryDirector.Instance == null) return;
        StoryDirector.Instance.OpenDialogue(dialogue);
    }

    void Update()
    {
        if (GameplayInputGate.Blocked) return;
        if (Input.GetKeyDown(KeyCode.E)) Interact();
    }
}
