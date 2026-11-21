using UnityEngine;

/// <summary>
/// 偷听区域（例如兵营旁）：玩家在半径内持续停留 requiredSeconds 秒且未被追击时，
/// 自动登记线索并显示偷听到的对话；同一线索只触发一次。
/// </summary>
public class EavesdropZone : MonoBehaviour
{
    /// <summary>线索 ID。</summary>
    public string clueId = "barracks_eavesdrop";
    /// <summary>偷听半径。</summary>
    public float radius = 4f;
    /// <summary>需要停留的时间（秒）。</summary>
    public float requiredSeconds = 3f;
    /// <summary>偷听到的对话内容。</summary>
    public DialogueAsset content;
    /// <summary>主角引用。</summary>
    private HeroController player;
    /// <summary>已累计停留时间。</summary>
    private float timer;
    /// <summary>是否已经触发过。</summary>
    private bool registered;

    void Update()
    {
        if (registered || StoryDirector.Instance == null) return;
        if (GameplayInputGate.Blocked) return;
        if (StoryDirector.Instance.IsUiOpen) return;
        if (player == null) player = FindObjectOfType<HeroController>();
        if (player == null || player.IsDead) return;

        bool inside = Vector3.Distance(player.transform.position, transform.position) <= radius;
        // 被卫兵追击时无法安心偷听。
        bool chased = false;
        foreach (var guard in FindObjectsOfType<GuardSenses>())
            if (guard.State == GuardState.Chase) { chased = true; break; }

        if (inside && !chased)
        {
            // 满足条件累计时间，达到阈值后登记线索并播放对话。
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
