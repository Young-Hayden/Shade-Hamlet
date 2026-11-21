using UnityEngine;

/// <summary>
/// 旧「取卷轴后撤离」任务的卷轴拾取物。
/// 剧情模式下 MissionController 被停用，本组件不会触发；保留供旧任务复用。
/// </summary>
public class IntelPickup : MonoBehaviour
{
    /// <summary>可交互距离。</summary>
    public float range = 1.6f;
    /// <summary>主角引用。</summary>
    private HeroController player;

    void Awake() { player = FindObjectOfType<HeroController>(); }

    /// <summary>是否满足拾取条件：任务处于收集阶段且玩家在范围内。</summary>
    public bool CanInteract()
    {
        if (GameplayInputGate.Blocked) return false;
        if (player == null) player = FindObjectOfType<HeroController>();
        return player != null && !player.IsDead
            && MissionController.Instance != null
            && MissionController.Instance.Stage == MissionStage.CollectIntel
            && Vector3.Distance(player.transform.position, transform.position) <= range;
    }

    /// <summary>拾取卷轴并推进任务阶段。</summary>
    public void Interact()
    {
        if (!CanInteract()) return;
        MissionController.Instance.RegisterIntel();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (GameplayInputGate.Blocked) return;
        if (Input.GetKeyDown(KeyCode.E)) Interact();
    }
}
