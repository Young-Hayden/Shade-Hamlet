using UnityEngine;

/// <summary>
/// 统一玩法输入闸门：剧情 UI、暂停、死亡结算、场景过渡期间阻断玩法输入。
/// 各玩法脚本的 Update 在读取按键前检查 <see cref="Blocked"/>，避免多个系统各自判断导致状态不一致。
/// 注意：剧情字幕/对话自身的推进不受此闸门限制。
/// </summary>
public static class GameplayInputGate
{
    /// <summary>当前是否应阻断玩法输入。</summary>
    public static bool Blocked
    {
        get
        {
            // 场景过渡（黑场淡出/淡入）期间冻结玩法操作。
            if (SceneTransition.BlocksGameplay) return true;
            // 暂停菜单或剧情暂停都会把 timeScale 置 0。
            if (Time.timeScale <= 0f) return true;
            // 剧情字幕/对话/结局 UI 打开。
            var story = StoryDirector.Instance;
            if (story != null && story.IsUiOpen) return true;
            // 死亡结算面板显示期间。
            if (GameFlow.Instance != null && GameFlow.Instance.IsDeathPanelShown) return true;
            return false;
        }
    }
}
