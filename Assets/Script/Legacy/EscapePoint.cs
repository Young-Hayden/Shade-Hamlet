using UnityEngine;

/// <summary>
/// 旧任务的撤离点：取得卷轴后显示光柱，玩家在撤离阶段按 E 完成任务。
/// 剧情模式下 MissionController 停用，本组件不会推进。
/// </summary>
public class EscapePoint : MonoBehaviour
{
    /// <summary>可交互距离。</summary>
    public float range = 2f;
    /// <summary>撤离光柱对象。</summary>
    public GameObject lightray;
    /// <summary>主角引用。</summary>
    private HeroController player;

    void Awake() { player = FindObjectOfType<HeroController>(); }

    /// <summary>是否满足撤离条件：任务处于撤离阶段且玩家在范围内。</summary>
    public bool CanInteract()
    {
        if (GameplayInputGate.Blocked) return false;
        if (player == null) player = FindObjectOfType<HeroController>();
        return player != null && !player.IsDead
            && MissionController.Instance != null
            && MissionController.Instance.Stage == MissionStage.Escape
            && Vector3.Distance(player.transform.position, transform.position) <= range;
    }

    /// <summary>尝试撤离。</summary>
    public bool Interact()
    {
        return CanInteract() && MissionController.Instance.TryExtract();
    }

    void Update()
    {
        // 取得情报后、任务完成前显示光柱，作为撤离指引。
        if (lightray != null)
        {
            bool show = MissionController.Instance != null && MissionController.Instance.HasIntel
                && MissionController.Instance.Stage != MissionStage.Completed;
            if (lightray.activeSelf != show) lightray.SetActive(show);
        }
        if (!GameplayInputGate.Blocked && Input.GetKeyDown(KeyCode.E)) Interact();
    }
}
